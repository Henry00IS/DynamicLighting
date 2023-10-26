Shader "Dynamic Lighting/Metallic PBR"
{
    // special thanks to https://learnopengl.com/PBR/Lighting

    Properties
    {
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile DYNAMIC_LIGHTING_SHADOW_SOFT DYNAMIC_LIGHTING_SHADOW_HARD
            #pragma shader_feature DYNAMIC_LIGHTING_UNLIT
            #pragma shader_feature METALLIC_TEXTURE_UNASSIGNED

            #include "UnityCG.cginc"
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
                o.uv1 = v.uv1 * lightmap_resolution;
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

#if DYNAMIC_LIGHTING_UNLIT

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv0);
            }

#else

            fixed4 frag (v2f i, uint triangle_index:SV_PrimitiveID) : SV_Target
            {
                // material parameters
                float3 albedo = tex2D(_MainTex, i.uv0).rgb;
                
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
                // iterate over every dynamic light affecting this triangle:
                uint triangle_light_count = dynamic_triangles_light_count(triangle_index);
                for (uint k = 0; k < triangle_light_count; k++)
                {
                    // get the current light from memory.
                    DynamicLight light = dynamic_lights[dynamic_triangles_light_index(triangle_index, k)];
                    
                    // this generates the light with shadows and effects calculation declaring:
                    // 
                    // required: DynamicLight light; the current dynamic light source.
                    // float3 light_direction; normalized direction between the light source and the fragment.
                    // float light_distanceSqr; the square distance between the light source and the fragment.
                    // float NdotL; dot product with the normal and light direction (diffusion).
                    // float map; the computed shadow of this fragment with effects.
                    // float attenuation; the attenuation of the point light with maximum radius.
                    //
                    // it may also early out and continue the loop to the next light.
                    //
                    #define GENERATE_NORMAL N
                    #include "GenerateLightProcessor.cginc"

                    // calculate per-light radiance
                    float3 H = normalize(V + light_direction);
                    float3 radiance = light.color * light.intensity * attenuation;

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
                    Lo += (kD * albedo / UNITY_PI + specular) * radiance * NdotL * map;
                }

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
                half3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));

                // the final lighting calculation combining all of the parts.
                float3 ambient = kD * albedo * (unity_lightmap_color + dynamic_ambient_color);
                float3 color = (ambient + Lo) * lerp(1.0, ao, _OcclusionStrength) + specular;

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, color);
                return fixed4(color, 1.0);
            }

#endif

            ENDCG
        }
    }
    Fallback "Diffuse"
}
