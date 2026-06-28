Shader "Dynamic Lighting/Unlit"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

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
            #pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv0 : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

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
                o.color = v.color;
                UNITY_TRANSFER_INSTANCE_ID(v,o);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // diffuse texture multiplied by material color and vertex colors.
                float4 col = tex2D(_MainTex, i.uv0) * UNITY_ACCESS_INSTANCED_PROP(Props, _Color) * i.color;

                // clip the fragments when cutout mode is active (leaves holes in color and depth buffers).
                #ifdef _ALPHATEST_ON
                    clip(col.a - _Cutoff);
                #endif

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }

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