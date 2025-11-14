Shader "Hidden/DynamicLightingPostProcessing"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
            #include "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/DynamicLighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4x4 clipToWorld;
			sampler2D_float _CameraDepthTexture;
			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float4 ComputeClipSpacePosition(float2 positionNDC, float deviceDepth)
			{
				float4 positionCS = float4(positionNDC * 2.0 - 1.0, deviceDepth, 1.0);
				/*#if UNITY_UV_STARTS_AT_TOP
				positionCS.y = -positionCS.y;
				#endif*/
				return positionCS;
			}

			float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
			{
				float4 positionCS = ComputeClipSpacePosition(positionNDC, deviceDepth);
				float4 hpositionWS = mul(invViewProjMatrix, positionCS);
				return hpositionWS.xyz / hpositionWS.w;
			}

			// macros to name the recycled variables.
			#define light_volumetricRadius radiusSqr
			#define light_volumetricThickness gpFloat1
			#define light_volumetricScale float3(light.gpFloat2, light.gpFloat3, light.shimmerScale)
			#define light_volumetricSpotAngle light.gpFloat2
			#define volumetric_type_sphere 1
			#define volumetric_type_box 2
			#define volumetric_type_cone_y 3
			#define volumetric_type_cone_z 4

			float4 frag (v2f i) : SV_Target
			{
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
				#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
				depth = (depth * 2.0) - 1.0;
				#endif
				float3 worldspace = ComputeWorldSpacePosition(i.uv.xy, depth, clipToWorld);
				float4 color = tex2D(_MainTex, i.uv);
				
				// iterate over every volumetric light in the scene (pre-filtered on the CPU):
				float4 fog_final = float4(0.0, 0.0, 0.0, 0.0);
				float fog_final_t = 0.0;
				for (uint k = 0; k < dynamic_lights_count; k++)
				{
					// get the current volumetric light from memory.
					DynamicLight light = dynamic_lights[k];
					
					float4 fog_color = float4(light.color, 1.0);
					float3 fog_center = light.position;
					float fog_radius = light.light_volumetricRadius;
					uint volumetric_type = light.channel;

					float t = 0.0;

					switch (volumetric_type)
					{
						case volumetric_type_sphere:
						{
							// closest point to the fog center on line between camera and fragment.
							float3 fog_closest_point = nearest_point_on_finite_line(_WorldSpaceCameraPos, worldspace, fog_center);
					
							// does the camera to world line intersect the fog sphere?
							if (point_in_sphere(fog_closest_point, fog_center, fog_radius))
							{
								// distance from the closest point on the camera and fragment line to the fog center.
								float fog_closest_point_distance_to_interior_sphere = fog_radius - distance(fog_closest_point, fog_center);
			
								// t is the volumetric non-linear color interpolant from 1.0 (center) to 0.0 (edge) of the sphere.
								t = fog_closest_point_distance_to_interior_sphere / fog_radius;
							}
						}
						break;

						case volumetric_type_box:
						{
							// define box bounds (min and max corners).
							float3 boxMin = light.position - light.light_volumetricRadius * light_volumetricScale;
							float3 boxMax = light.position + light.light_volumetricRadius * light_volumetricScale;

							// compute the ray direction (from camera to the current fragment).
							float3 rayDir = normalize(worldspace - _WorldSpaceCameraPos);

							// get the distance to the current fragment in world space.
							// tMax is limited by the maximum depth (geometry).
							float tMin, tMax;
							float maxDepth = length(worldspace - _WorldSpaceCameraPos);
							ray_box_intersection(_WorldSpaceCameraPos, rayDir, boxMin, boxMax, tMin, tMax, maxDepth);

							// compute the length of the ray segment inside the box.
							float ray_length_in_box = tMax - tMin;
									
							// calculate the fog intensity based on the distance traveled inside the box.
							t = ray_length_in_box / length(boxMax - boxMin); // normalize by box size.
						}
						break;

						case volumetric_type_cone_y:
						case volumetric_type_cone_z:
						{
							// compute the ray direction (from camera to the current fragment).
							float3 rayDir = normalize(worldspace - _WorldSpaceCameraPos);

							// get the distance to the current fragment in world space.
							float tMin, tMax;
							float maxDepth = length(worldspace - _WorldSpaceCameraPos);

							// Perform ray-cone intersection test
							if (ray_cone_intersection(_WorldSpaceCameraPos, rayDir, light.position, light.forward, light_volumetricSpotAngle, light.light_volumetricRadius, tMin, tMax, maxDepth))
							{
								// compute the length of the ray segment inside the cone.
								float ray_length_in_cone = tMax - tMin;

								// calculate the fog intensity based on the distance traveled inside the cone.
								t = ray_length_in_cone / light.light_volumetricRadius;
							}
						}
						break;
					}

					// apply smoothstep for a gradual fog transition near the edges.
					t = smoothstep(0.0, 1.0, t);
								
					// apply the thickness to the fog that appears as a solid color.
					t = saturate(t * light.light_volumetricThickness);
						
					// the distance from the camera to the world is used to make nearby geometry inside the fog visible.
					float camera_distance_from_world = distance(_WorldSpaceCameraPos, worldspace) * light.volumetricVisibility;
						
					// we only subtract from t so that naturally fading fog takes precedence.
					t = min(t, camera_distance_from_world);
			
					// let the user tweak the fog intensity with a multiplier.
					t *= light.volumetricIntensity;
			
					// remember the most opaque fog that we have encountered.
					fog_final_t = max(fog_final_t, t);
			
					// blend between the current color and the fog color.
					fog_final = color_screen(fog_final, fog_color * t);
				}
				
				// special blend that allows for fully opaque fog.
				return lerp(color_screen(fog_final, color), fog_final, saturate(fog_final_t));
			}
			ENDCG
		}
	}
}