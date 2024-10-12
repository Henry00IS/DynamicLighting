Shader "Dynamic Lighting/Metallic"
{
    // special thanks to https://learnopengl.com/PBR/Lighting

    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        [NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "black" {}
        _Metallic("Metallic (Fallback)", Range(0,1)) = 0
        _GlossMapScale("Smoothness", Range(0,1)) = 1
        [NoScaleOffset][Normal] _BumpMap("Normal map", 2D) = "bump" {}
        _BumpScale("Normal scale", Float) = 1
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 0.75
    }
    
    CustomEditor "AlpacaIT.DynamicLighting.Editor.MetallicShaderGUI"
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile __ DYNAMIC_LIGHTING_SHADOW_SOFT
            #pragma multi_compile __ DYNAMIC_LIGHTING_LIT
            #pragma multi_compile __ DYNAMIC_LIGHTING_BVH
            #pragma multi_compile __ DYNAMIC_LIGHTING_BOUNCE
            #pragma multi_compile __ DYNAMIC_LIGHTING_BOUNCE_6BPP
            #pragma multi_compile multi_compile_fwdbase
            #pragma shader_feature METALLIC_TEXTURE_UNASSIGNED

            #include "UnityCG.cginc"

            // the following compiler flags are available:
            // 
            // when targeting modern hardware, you can improve the shadow quality by uncommenting one of these lines:
            // #define DYNAMIC_LIGHTING_SHADOW_SAMPLER shadow_sample_gaussian3
            // #define DYNAMIC_LIGHTING_SHADOW_SAMPLER shadow_sample_gaussian5
            //
            #include "DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
                float4 tangent : TANGENT;
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
                half3 tspace0 : TEXCOORD4; // tangent.x, bitangent.x, normal.x
                half3 tspace1 : TEXCOORD5; // tangent.y, bitangent.y, normal.y
                half3 tspace2 : TEXCOORD6; // tangent.z, bitangent.z, normal.z
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            sampler2D _MetallicGlossMap;
            float _Metallic;
            float _GlossMapScale;
            sampler2D _BumpMap;
            sampler2D _OcclusionMap;
            float _BumpScale;
            float _OcclusionStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                // as we need pixel coordinates doing the multiplication here saves time.
                // confirmed with NVIDIA Quadro K1000M improving the framerate.
                o.uv1 = (v.uv1 - dynamic_lighting_unity_LightmapST.zw) * dynamic_lighting_unity_LightmapST.xy * lightmap_resolution;
                o.uv2 = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

                half3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
                // compute bitangent from cross product of normal and tangent
                half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                half3 wBitangent = cross(o.normal, wTangent) * tangentSign;
                // output the tangent space matrix
                o.tspace0 = half3(wTangent.x, wBitangent.x, o.normal.x);
                o.tspace1 = half3(wTangent.y, wBitangent.y, o.normal.y);
                o.tspace2 = half3(wTangent.z, wBitangent.z, o.normal.z);

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #if DYNAMIC_LIGHTING_LIT

            #define DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS inout float3 albedo, inout float metallic, inout float roughness, inout float3 N, inout float3 V, inout float3 F0, inout float3 Lo
            #define DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS albedo, metallic, roughness, N, V, F0, Lo

            DYNLIT_FRAGMENT_FUNCTION
            {
                // material parameters
                float3 albedo = tex2D(_MainTex, i.uv0).rgb * _Color.rgb * i.color;
                
            #if METALLIC_TEXTURE_UNASSIGNED
                float metallic = _Metallic;
                float roughness = 1.0 - _GlossMapScale;
            #else
                float4 metallicmap = tex2D(_MetallicGlossMap, i.uv0);
                float metallic = metallicmap.r;
                float roughness = 1.0 - metallicmap.a * _GlossMapScale;
            #endif
                float ao = tex2D(_OcclusionMap, i.uv0).r;

                half3 bumpmap = UnpackNormalWithScale(tex2D(_BumpMap, i.uv0), _BumpScale);
                // transform normal from tangent to world space
                half3 worldNormal;
                worldNormal.x = dot(i.tspace0, bumpmap);
                worldNormal.y = dot(i.tspace1, bumpmap);
                worldNormal.z = dot(i.tspace2, bumpmap);

                float3 N = normalize(worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.world);

                // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0
                // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow).
                float3 F0 = lerp(0.04, albedo, metallic);

                // reflectance equation
                float3 Lo = float3(0.0, 0.0, 0.0);
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // ambient lighting (we now use IBL as the ambient term).
                float3 F = fresnelSchlickRoughness(max(dot(N, V), 0.0), F0, roughness);
                
                float3 kS = F;
                float3 kD = 1.0 - kS;
                kD *= 1.0 - metallic;
                
                // reflecting a ray from the camera against the object surface.
                half3 reflection = reflect(-V, worldNormal);
                // sample the default cubemap provided by unity.
                half4 skyData = UNITY_SAMPLE_TEXCUBE_LOD(unity_SpecCube0, reflection, roughness * 4.0);
                half3 skyColor = DecodeHDR(skyData, unity_SpecCube0_HDR);
                float3 specular = skyColor * F;
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    half3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    half3 unity_lightmap_color = half3(0.0, 0.0, 0.0);
                #endif
                
                // the final lighting calculation combining all of the parts.
                float3 ambient = kD * albedo * (unity_lightmap_color + dynamic_ambient_color);
                float3 color = (ambient + Lo) * lerp(1.0, ao, _OcclusionStrength) + specular;
                
                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, color);
                return fixed4(color, 1.0);
            }

            DYNLIT_FRAGMENT_LIGHT
            {
                // this generates the light with shadows and effects calculation declaring:
                // 
                // DynamicLight light; the current dynamic light source.
                // DynamicTriangle dynamic_triangle; the current triangle data.
                // float3 light_direction; normalized direction between the light source and the fragment.
                // float light_distanceSqr; the square distance between the light source and the fragment.
                // float3 light_position_minus_world; the light position minus the world position.
                // float NdotL; dot product with the normal and light direction (diffusion).
                // float attenuation; the attenuation of the point light with maximum radius.
                // float map; the computed shadow of this fragment with effects.
                // bool is_bounce_available; whether bounce texture data is available on this triangle.
                // float bounce; the computed grayscale bounce texture color of this fragment.
                //
                // it may also early out and continue the loop to the next light.
                //
                #define GENERATE_NORMAL N
                #include "GenerateLightProcessor.cginc"
                
                // calculate per-light radiance
                float3 H = normalize(V + light_direction);
#if DYNAMIC_LIGHTING_BOUNCE
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
#if DYNAMIC_LIGHTING_BOUNCE
                Lo += (kD * albedo / UNITY_PI + specular) * radiance * NdotL * map;
                Lo += (kD * albedo / UNITY_PI + specular) * radiance * bounce;
#else
                Lo += (kD * albedo / UNITY_PI + specular) * radiance * NdotL * map;
#endif
            }
            
            #else

                DYNLIT_FRAGMENT_UNLIT

            #endif
            ENDCG
        }
    }
    Fallback "Diffuse"
}
