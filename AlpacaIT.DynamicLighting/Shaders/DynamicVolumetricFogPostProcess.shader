Shader "Hidden/DynamicVolumetricFogPostProcess"
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
			#pragma target 5.0
			
			#include "UnityCG.cginc"
			#include "DynamicLighting.cginc"

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

			float4 frag (v2f i) : SV_Target
			{
				float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv.xy);
				#if defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)
				depth = (depth * 2.0) - 1.0;
				#endif
				float3 worldspace = ComputeWorldSpacePosition(i.uv.xy, depth, clipToWorld);
				float4 color = tex2D(_MainTex, i.uv);
				
                // iterate over every dynamic light in the scene:
                float4 fog_final = float4(0.0, 0.0, 0.0, 0.0);
                for (uint k = 0; k < dynamic_lights_count; k++)
				{
					// get the current light from memory.
					DynamicLight light = dynamic_lights[k];
					
					float3 camera = _WorldSpaceCameraPos;
					float4 fog_color = float4(light.color, 1.0);
					float3 fog_center = light.position;
					float fog_radius = sqrt(light.radiusSqr);
					float fog_intensity_multiplier = 1.0;
		
					// closest point to the fog center on line between camera and fragment.
					float3 fog_closest_point = nearest_point_on_finite_line(camera, worldspace, fog_center);
					
					// does the camera to world line intersect the fog sphere?
					if (point_in_sphere(fog_closest_point, fog_center, fog_radius))
					{
						// distance from the closest point on the camera and fragment line to the fog center.
						float fog_closest_point_distance_to_interior_sphere = fog_radius - distance(fog_closest_point, fog_center);
			
						// t is the volumetric linear color interpolant from 1.0 (center) to 0.0 (edge) of the sphere.
						float t = fog_closest_point_distance_to_interior_sphere / fog_radius;
						//float t = pow(fog_closest_point_distance_to_interior_sphere / fog_radius, 2.0);
						
						//t = saturate(t * 4.0);
						
						// the distance from the camera to the world is used to make nearby geometry inside the fog visible.
						float camera_distance_from_world = distance(camera, worldspace) * 0.5;
						
						// we only subtract from t so that naturally fading fog takes precedence.
						t = min(t, camera_distance_from_world);
			
						// let the user tweak the fog intensity with a multiplier.
						t *= fog_intensity_multiplier;
						
						// blend between the current color and the fog color.
						color = lerp(color, fog_color, t);
					}
				}
				
				return color;
			}
			ENDCG
		}
	}
}