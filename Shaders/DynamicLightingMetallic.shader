Shader "Dynamic Lighting/Metallic PBR"
{
    Properties
    {
        _MainTex("Albedo", 2D) = "white" {}
        _MetallicGlossMap("Metallic", 2D) = "black" {}
        _GlossMapScale("Smoothness", Range(0,1)) = 1
        _BumpMap("Normal map", 2D) = "bump" {}
        _BumpScale("Normal scale", Range(0,1)) = 1
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 0.75
    }
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
            #pragma shader_feature DYNAMIC_LIGHTING_UNLIT

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
                UNITY_FOG_COORDS(1)
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

            fixed4 frag (v2f i) : SV_Target
            {
                // material parameters
                float3 albedo = tex2D(_MainTex, i.uv0).rgb;
                float4 metallicmap = tex2D(_MetallicGlossMap, i.uv0);
                float metallic = metallicmap.r;
                float roughness = 1.0 - metallicmap.a * _GlossMapScale;
                float ao = tex2D(_OcclusionMap, i.uv0).r;

                half3 bumpmap = UnpackNormal(tex2D(_BumpMap, i.uv0));
                // transform normal from tangent to world space
                half3 worldNormal;
                worldNormal.x = dot(i.tspace0, bumpmap);
                worldNormal.y = dot(i.tspace1, bumpmap);
                worldNormal.z = dot(i.tspace2, bumpmap);

                float3 N = normalize(worldNormal);
                float3 V = normalize(_WorldSpaceCameraPos - i.world);

                float3 F0 = float3(0.04, 0.04, 0.04);
                F0 = lerp(F0, albedo, metallic);

                // reflectance equation
                float3 Lo = float3(0.0, 0.0, 0.0);
                // iterate over every dynamic light in the scene:
                for (uint k = 0; k < dynamic_lights_count; k++)
                {
                    // get the current light from memory.
                    DynamicLight light = dynamic_lights[k];
                    
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

                    // cook-torrance brdf
                    float NDF = DistributionGGX(N, H, roughness);
                    float G = GeometrySmith(N, V, light_direction, roughness);
                    float3 F = fresnelSchlick(max(dot(H, V), 0.0), F0);

                    float3 kS = F;
                    float3 kD = float3(1.0, 1.0, 1.0) - kS;
                    kD *= 1.0 - metallic;

                    float3 numerator = NDF * G * F;
                    float denominator = 4.0 * max(dot(N, V), 0.0) * NdotL + 0.0001;
                    float3 specular = numerator / denominator;
                    
                    // add to outgoing radiance Lo
                    Lo += (kD * albedo / UNITY_PI + specular) * radiance * NdotL * map;
                }

                float3 color = Lo * lerp(1.0, ao, _OcclusionStrength);

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
