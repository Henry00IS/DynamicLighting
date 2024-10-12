Shader "Dynamic Lighting/Transparent"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
    }
    
    CustomEditor "AlpacaIT.DynamicLighting.Editor.DefaultShaderGUI"
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            // enable regular alpha blending for this pass.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
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
            };

            struct v2f
            {
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD5;
                UNITY_FOG_COORDS(4)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 world : TEXCOORD2;
                float3 normal : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

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
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #if DYNAMIC_LIGHTING_LIT

            #define DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS inout float3 light_final
            #define DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS light_final

            DYNLIT_FRAGMENT_FUNCTION
            {
                float3 light_final = dynamic_ambient_color;
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    half3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    half3 unity_lightmap_color = half3(0.0, 0.0, 0.0);
                #endif

                // sample the main texture, multiply by the light and add vertex colors.
                fixed4 col = tex2D(_MainTex, i.uv0) * half4(_Color) * half4(light_final + unity_lightmap_color, 1) * i.color;
                
                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
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
                #define GENERATE_NORMAL i.normal
                #include "GenerateLightProcessor.cginc"
                
                // add this light to the final color of the fragment.
#if DYNAMIC_LIGHTING_BOUNCE
                light_final += (light.color * attenuation * NdotL * map) + (light.bounceColor * attenuation * bounce);
#else
                light_final += (light.color * attenuation * NdotL * map);
#endif
            }

            #else

                DYNLIT_FRAGMENT_UNLIT

            #endif
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/Diffuse"
}
