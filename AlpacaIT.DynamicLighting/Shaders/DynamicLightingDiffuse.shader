Shader "Dynamic Lighting/Diffuse"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5
        [HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [NoScaleOffset] _EmissionMap("Emission (RGB)", 2D) = "white" {}

        [HideInInspector] _Mode ("Rendering Mode", Float) = 0.0 // standard shader (0: opaque, 1: cutout, 2: fade, 3: transparent).
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
        [HideInInspector] _Cull("Culling Mode", Float) = 2.0
    }

    CustomEditor "AlpacaIT.DynamicLighting.Editor.DefaultShaderGUI"
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
            #pragma multi_compile __ DYNAMIC_LIGHTING_QUALITY_LOW DYNAMIC_LIGHTING_QUALITY_HIGH DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS
            #pragma multi_compile __ DYNAMIC_LIGHTING_LIT
            #pragma multi_compile __ DYNAMIC_LIGHTING_BVH
            #pragma multi_compile __ DYNAMIC_LIGHTING_BOUNCE
            #pragma multi_compile __ DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES
            #pragma multi_compile multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _ DYNAMIC_LIGHTING_CULL_FRONT DYNAMIC_LIGHTING_CULL_OFF

            #include "UnityCG.cginc"
            #include "DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            #ifdef _EMISSION
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
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            #if DYNAMIC_LIGHTING_LIT

            #define DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS inout float3 light_final
            #define DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS light_final

            DYNLIT_FRAGMENT_FUNCTION
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float3 light_final = dynamic_ambient_color;
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    float3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    float3 unity_lightmap_color = float3(0.0, 0.0, 0.0);
                #endif

                // sample the main texture, multiply by the light and add vertex colors.
                float4 col = tex2D(_MainTex, i.uv0) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * float4(light_final + unity_lightmap_color, 1) * i.color;
                
                // clip the fragments when cutout mode is active (leaves holes in color and depth buffers).
                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                // sample the emission map, add after lighting calculations.
                #ifdef _EMISSION
                    col.rgb += tex2D(_EmissionMap, i.uv0).rgb * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor).rgb;
                #endif

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
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
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

        Pass
        {
            Name "FORWARD_DELTA"
			Tags { "LightMode" = "ForwardAdd" }
			Blend One One
			ZWrite Off

            CGPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON
            #pragma multi_compile_fwdadd_fullshadows
            #include "GenerateForwardAdd.cginc"
            ENDCG
        }

		Pass
        {
			Name "ShadowCaster"
			Tags { "LightMode" = "ShadowCaster" }

			CGPROGRAM
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON
			#pragma multi_compile_shadowcaster
            #include "GenerateShadowCaster.cginc"
			ENDCG
		}
    }
    Fallback "Diffuse"
}