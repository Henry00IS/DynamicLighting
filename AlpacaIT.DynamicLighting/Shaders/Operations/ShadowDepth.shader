Shader "Hidden/Dynamic Lighting/ShadowDepth"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
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
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD1;
            };

            float dynamic_lighting_shadow_depth_light_radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                float dist = distance(_WorldSpaceCameraPos, i.world) / dynamic_lighting_shadow_depth_light_radius;
                return float2(dist, dist * dist);
            }

            ENDCG
        }
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "Transparent" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 world : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float dynamic_lighting_shadow_depth_light_radius;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv0);
                if (col.a > 0.5)
                {
                    float dist = distance(_WorldSpaceCameraPos, i.world) / dynamic_lighting_shadow_depth_light_radius;
                    return float2(dist, dist * dist);
                }
                else
                {
                    discard;
                    return float2(0.0, 0.0); // hlsl compiler wants us to return something- never gets executed.
                }
            }

            ENDCG
        }
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "TransparentCutout" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 world : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float dynamic_lighting_shadow_depth_light_radius;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv0);
                if (col.a > 0.5)
                {
                    float dist = distance(_WorldSpaceCameraPos, i.world) / dynamic_lighting_shadow_depth_light_radius;
                    return float2(dist, dist * dist);
                }
                else
                {
                    discard;
                    return float2(0.0, 0.0); // hlsl compiler wants us to return something- never gets executed.
                }
            }

            ENDCG
        }
    }
    Fallback "Diffuse"
}