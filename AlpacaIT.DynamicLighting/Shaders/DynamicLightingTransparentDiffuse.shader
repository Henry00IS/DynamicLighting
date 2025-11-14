Shader "Dynamic Lighting/Transparent"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _EmissionMap("Emission (RGB)", 2D) = "white" {}
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
                                                                                        /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile multi_compile_fwdbase
            #pragma shader_feature _EMISSION

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            struct v2f /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
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
            float4 _Color;

            #if _EMISSION
                sampler2D _EmissionMap;
                float4 _EmissionColor;
            #endif

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                // as we need pixel coordinates the xy component also contains the lightmap resolution.
                // confirmed with NVIDIA Quadro K1000M improving the framerate.
                o.uv1 = (v.uv1 - dynamic_lighting_unity_LightmapST.zw) * dynamic_lighting_unity_LightmapST.xy;
                o.uv2 = v.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
                /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #if DYNAMIC_LIGHTING_LIT

            #define DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS inout float3 light_final
            #define DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS light_final

            DYNLIT_FRAGMENT_FUNCTION /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
            {
                float3 light_final = dynamic_ambient_color;
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    float3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    float3 unity_lightmap_color = float3(0.0, 0.0, 0.0);
                #endif

                /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */

                // sample the main texture, multiply by the light and add vertex colors.
                float4 col = tex2D(_MainTex, i.uv0) * _Color * float4(light_final + unity_lightmap_color, 1) * i.color;
                
                // sample the emission map, add after lighting calculations.
                #if _EMISSION
                    col.rgb += tex2D(_EmissionMap, i.uv0).rgb * _EmissionColor.rgb;
                #endif

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            
            DYNLIT_FRAGMENT_LIGHT /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
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
                #include "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/LightProcessor.cginc"
                
                // add this light to the final color of the fragment.
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
                light_final += (light.color * attenuation * NdotL * map) + (light.bounceColor * attenuation * bounce);
#else /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */
                light_final += (light.color * attenuation * NdotL * map);
#endif
            }

            #else

                DYNLIT_FRAGMENT_UNLIT

            #endif
            ENDCG
        }

        /* DO NOT USE THIS SHADER IT IS OLD AND WILL BE REMOVED SOON */

        Pass
        {
            Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off

            CGPROGRAM
            #define DYNAMIC_LIGHTING_LEGACY_TRANSPARENT_SHADER
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ForwardAdd.cginc"
            ENDCG
        }
        
		Pass
        {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
            #define DYNAMIC_LIGHTING_LEGACY_TRANSPARENT_SHADER
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ShadowCaster.cginc"
			ENDCG
		}
    }
    Fallback "Legacy Shaders/Transparent/Diffuse"
}
