Shader "Dynamic Lighting/Metallic"
{
    // special thanks to https://learnopengl.com/PBR/Lighting

    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "black" {}
        _Metallic("Metallic (Fallback)", Range(0,1)) = 0
        _GlossMapScale("Smoothness", Range(0,1)) = 1
        [NoScaleOffset][Normal] _BumpMap("Normal map", 2D) = "bump" {}
        _BumpScale("Normal scale", Float) = 1
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 0.75
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _EmissionMap("Emission (RGB)", 2D) = "white" {}

        [HideInInspector] _Mode ("Rendering Mode", Float) = 0.0 // standard shader (0: opaque, 1: cutout, 2: fade, 3: transparent).
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _Cull("Culling Mode", Float) = 2.0
    }
    
    CustomEditor "AlpacaIT.DynamicLighting.Editor.MetallicShaderGUI"
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma shader_feature_local METALLIC_TEXTURE_UNASSIGNED
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _ DYNAMIC_LIGHTING_CULL_FRONT DYNAMIC_LIGHTING_CULL_OFF

            #include "UnityCG.cginc"
            #include "UnityStandardUtils.cginc"
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD8;
                UNITY_FOG_COORDS(7)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 world : TEXCOORD2;
                float3 normal : TEXCOORD3;
                float3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
                float3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
                float3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _MetallicGlossMap;
            float _Metallic;
            float _GlossMapScale;
            sampler2D _BumpMap;
            sampler2D _OcclusionMap;
            float _BumpScale;
            float _OcclusionStrength;

            #if _EMISSION
                sampler2D _EmissionMap;
            #endif

            #ifdef _ALPHATEST_ON
                float _Cutoff;
            #endif

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
            #ifdef _EMISSION
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
            #endif
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                UNITY_SETUP_INSTANCE_ID(v);

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                // as we need pixel coordinates the xy component also contains the lightmap resolution.
                // confirmed with NVIDIA Quadro K1000M improving the framerate.
                o.uv1 = (v.uv1 - dynamic_lighting_unity_LightmapST.zw) * dynamic_lighting_unity_LightmapST.xy;
                o.uv2 = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

                float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                float3 wBitangent = cross(o.normal, wTangent) * tangentSign;
                // output the tangent space matrix
                o.tspace0 = float3(wTangent.x, wBitangent.x, o.normal.x);
                o.tspace1 = float3(wTangent.y, wBitangent.y, o.normal.y);
                o.tspace2 = float3(wTangent.z, wBitangent.z, o.normal.z);

                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #if DYNAMIC_LIGHTING_LIT

            #define DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS inout float4 albedo, inout float metallic, inout float roughness, inout float3 N, inout float3 V, inout float3 F0, inout float3 Lo
            #define DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS albedo, metallic, roughness, N, V, F0, Lo

            DYNLIT_FRAGMENT_FUNCTION
            {
                // This function computes the final fragment color to be output to the screen,
                // declaring and using the following variables (see DynamicLighting.cginc for the full list of variables):
                //
                // --------------------------------- | ------------------------------ | ---------------------------------------------------------------
                // Variable                          | Type/Semantic                  | Description
                // --------------------------------- | ------------------------------ | ---------------------------------------------------------------
                // i                                 | v2f                            | Fragment shader interpolants (from struct v2f in this file).
                // triangle_index                    | uint : SV_PrimitiveID          | Triangle identifier in the mesh.
                // is_front_face                     | bool : SV_IsFrontFace          | Specifies whether a triangle is front facing.
                // --------------------------------- | ------------------------------ | ---------------------------------------------------------------
                // lightmap_resolution               | uint                           | Lightmap size of the mesh (if > 0 then it was raytraced).
                // dynamic_ambient_color             | float3                         | The ambient color as set in the Dynamic Light Manager.
                // --------------------------------- | ------------------------------ | ---------------------------------------------------------------
                // DYNLIT_FRAGMENT_INTERNAL          | Macro                          | Performs the lighting computations (see DYNLIT_FRAGMENT_LIGHT).
                // --------------------------------- | ------------------------------ | ---------------------------------------------------------------
                //
                UNITY_SETUP_INSTANCE_ID(i);

                // material parameters
                float4 albedo = tex2D(_MainTex, i.uv0) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * i.color;
                
            #if METALLIC_TEXTURE_UNASSIGNED
                float metallic = _Metallic;
                float roughness = 1.0 - _GlossMapScale;
            #else
                float4 metallicmap = tex2D(_MetallicGlossMap, i.uv0);
                float metallic = metallicmap.r;
                float roughness = 1.0 - metallicmap.a * _GlossMapScale;
            #endif
                float ao = tex2D(_OcclusionMap, i.uv0).r;

                float3 bumpmap = UnpackNormalWithScale(tex2D(_BumpMap, i.uv0), _BumpScale);
                // transform normal from tangent to world space
                float3 worldNormal;
                worldNormal.x = dot(i.tspace0, bumpmap);
                worldNormal.y = dot(i.tspace1, bumpmap);
                worldNormal.z = dot(i.tspace2, bumpmap);

                float3 N = normalize(worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.world);

                // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0
                // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow).
                float3 F0 = lerp(0.04, albedo.rgb, metallic);

                // reflectance equation
                float3 Lo = float3(0.0, 0.0, 0.0);
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // ambient lighting (we now use IBL as the ambient term).
                float3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
                
                float3 kS = F;
                float3 kD = 1.0 - kS;
                kD *= 1.0 - metallic;
                
                // reflecting a ray from the camera against the object surface.
                float3 reflection = reflect(-V, worldNormal);

                // adjust the reflection direction for box projection.
                #ifdef UNITY_SPECCUBE_BOX_PROJECTION
                    reflection = BoxProjectedCubemapDirection(
                        reflection, i.world,
                        unity_SpecCube0_ProbePosition,
                        unity_SpecCube0_BoxMin,
                        unity_SpecCube0_BoxMax
                    );
                #endif

                // sample the default cubemap provided by unity.
                float4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflection, roughness * 4.0);
                float3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
                float3 specular = skyColor * F;
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    float3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    float3 unity_lightmap_color = float3(0.0, 0.0, 0.0);
                #endif
                
                // the final lighting calculation combining all of the parts.
                float3 ambient = kD * albedo * (unity_lightmap_color + dynamic_ambient_color);
                float3 color = (ambient + Lo) * lerp(1.0, ao, _OcclusionStrength) + specular;
                
                // clip the fragments when cutout mode is active (leaves holes in color and depth buffers).
                #ifdef _ALPHATEST_ON
                    clip(albedo.a - _Cutoff);
                #endif

                // sample the emission map, add after lighting calculations.
                #if _EMISSION
                    color.rgb += tex2D(_EmissionMap, i.uv0).rgb * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor).rgb;
                #endif

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, color);

            #ifdef _ALPHAPREMULTIPLY_ON
                return float4(color, albedo.a);
            #else
                return float4(color, 1.0);
            #endif
            }

            DYNLIT_FRAGMENT_LIGHT
            {
                // This function computes lighting with shadows and effects for a dynamic light source,
                // declaring and using the following variables:
                //
                // --------------------------------- | ------------------------------ | --------------------------------------------------------------- | -----------------------------------------------------------------
                // Variable                          | Type/Semantic                  | Description                                                     | Conditional Availability
                // --------------------------------- | ------------------------------ | --------------------------------------------------------------- | -----------------------------------------------------------------
                // i                                 | v2f                            | Fragment shader interpolants (from struct v2f in this file).    | Always
                // triangle_index                    | uint : SV_PrimitiveID          | Triangle identifier in the mesh.                                | Always
                // is_front_face                     | bool : SV_IsFrontFace          | Specifies whether a triangle is front facing.                   | Always
                // light                             | DynamicLight                   | Current dynamic light source (see DynamicLighting.cginc).       | Always
                // dynamic_triangle                  | DynamicTriangle                | Current triangle data (see DynamicLighting.cginc).              | Always
                // --------------------------------- | ------------------------------ | --------------------------------------------------------------- | -----------------------------------------------------------------
                // light_direction                   | float3                         | Normalized direction from fragment to light source.             | Always
                // light_distanceSqr                 | float                          | Squared distance from fragment to light source.                 | Always
                // light_position_minus_world        | float3                         | Light position minus world position.                            | Always
                // NdotL                             | float                          | Dot product of normal and light direction (for diffusion).      | Always
                //                                   |                                |   - Culling flips the normal based on is_front_face.            |
                //                                   |                                |   - Dynamic geometry uses half-Lambert shading.                 |
                // attenuation                       | float                          | Attenuation for point light with max radius.                    | Always
                // map                               | float                          | Computed shadow value for this fragment with effects.           | Always
                // --------------------------------- | ------------------------------ | --------------------------------------------------------------- | -----------------------------------------------------------------
                // is_bounce_available               | bool                           | Whether bounce texture data is available on this triangle.      | DYNAMIC_LIGHTING_BOUNCE && !DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS
                // bounce                            | float                          | Computed grayscale bounce texture color for this fragment.      | DYNAMIC_LIGHTING_BOUNCE && !DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS
                // --------------------------------- | ------------------------------ | --------------------------------------------------------------- | -----------------------------------------------------------------
                //
                // The function may early-out and continue to the next light in the loop due to heavy optimizations.
                //
                #define GENERATE_NORMAL N
                #include "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/LightProcessor.cginc"
                
                // calculate per-light radiance
                float3 H = normalize(V + light_direction);
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
                float3 radiance = (light.color * light.intensity * attenuation) + (light.bounceColor * light.intensity * attenuation * bounce);
#else
                float3 radiance = (light.color * light.intensity * attenuation);
#endif                
                // normal distribution function: approximates the amount the surface's
                // microfacets are aligned to the halfway vector, influenced by the roughness of
                // the surface; this is the primary function approximating the microfacets.
                float NDF = DistributionGGX(N, H, roughness);
                
                // geometry function: describes the self-shadowing property of the microfacets.
                // when a surface is relatively rough, the surface's microfacets can overshadow
                // other microfacets reducing the light the surface reflects.
                float G = GeometrySmith(N, V, light_direction, roughness);
                
                // fresnel equation: the fresnel equation describes the ratio of surface
                // reflection at different surface angles.
                float3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);
                
                // reflected and refracted light must be mutually exclusive. whatever light
                // energy gets reflected will no longer be absorbed by the material itself.
                // 
                // kS: reflection/specular fraction.
                // kD: refraction/diffuse fraction.
                float3 kS = F;
                
                // for energy conservation, the diffuse and specular light can't be above 1.0
                // (unless the surface emits light); to preserve this relationship the diffuse
                // component (kD) should equal 1.0 - kS.
                float3 kD = 1.0 - kS;
                
                // multiply kD by the inverse metalness such that only non-metals have diffuse
                // lighting, or a linear blend if partly metal (pure metals have no diffuse light).
                kD *= 1.0 - metallic; 
                
                // cook-torrance brdf
                // we add 0.0001 to the denominator to prevent a potential divide by zero.
                float3 numerator = NDF * G * F;
                float denominator = 4.0 * max(dot(N, V), 0.0) * NdotL + 0.0001;
                float3 specular = numerator / denominator;
                
                // add to outgoing radiance Lo
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
                Lo += (kD * albedo.rgb / UNITY_PI + specular) * radiance * NdotL * map;
                Lo += (kD * albedo.rgb / UNITY_PI + specular) * radiance * bounce;
#else
                Lo += (kD * albedo.rgb / UNITY_PI + specular) * radiance * NdotL * map;
#endif
            }
            
            #else

                DYNLIT_FRAGMENT_UNLIT

            #endif
            ENDCG
        }

        Pass
        {
            Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off

            CGPROGRAM
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ForwardAddMetallic.cginc"
            ENDCG
        }

		Pass
        {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ShadowCaster.cginc"
			ENDCG
		}
    }
    Fallback "Diffuse"
}
