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
            #pragma multi_compile multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma multi_compile __ DYNAMIC_LIGHTING_SCENE_VIEW_MODE_LIGHTING
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _ DYNAMIC_LIGHTING_CULL_FRONT DYNAMIC_LIGHTING_CULL_OFF

            #include "UnityCG.cginc"
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/DynamicLighting.cginc"

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

                float3 light_final = dynamic_ambient_color;
                
                DYNLIT_FRAGMENT_INTERNAL
                
                // sample the unity baked lightmap (i.e. progressive lightmapper).
                #if LIGHTMAP_ON
                    float3 unity_lightmap_color = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, i.uv2));
                #else
                    float3 unity_lightmap_color = float3(0.0, 0.0, 0.0);
                #endif

                #ifdef DYNAMIC_LIGHTING_SCENE_VIEW_MODE_LIGHTING
                    // sample the main texture alpha, multiply by the light and add vertex color alpha.
                    float4 col = float4(1, 1, 1, tex2D(_MainTex, i.uv0).a) * float4(1, 1, 1, UNITY_ACCESS_INSTANCED_PROP(Props, _Color).a) * float4(light_final + unity_lightmap_color, 1) * float4(1, 1, 1, i.color.a);
                #else
                    // sample the main texture, multiply by the light and add vertex colors.
                    float4 col = tex2D(_MainTex, i.uv0) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * float4(light_final + unity_lightmap_color, 1) * i.color;
                #endif
                
                // clip the fragments when cutout mode is active (leaves holes in color and depth buffers).
                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                // sample the emission map, add after lighting calculations.
                #if defined(_EMISSION) && !defined(DYNAMIC_LIGHTING_SCENE_VIEW_MODE_LIGHTING)
                    col.rgb += tex2D(_EmissionMap, i.uv0).rgb * UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor).rgb;
                #endif

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
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
                #define GENERATE_NORMAL i.normal
                #include "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/LightProcessor.cginc"
                
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
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ForwardAdd.cginc"
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