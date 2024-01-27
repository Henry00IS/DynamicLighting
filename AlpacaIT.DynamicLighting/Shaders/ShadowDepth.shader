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
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            #include "DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                float3 world_direction = _WorldSpaceCameraPos - i.world;
                float world_distanceSqr = dot(world_direction, world_direction);
                return world_distanceSqr;
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
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            #include "DynamicLighting.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv0 : TEXCOORD0;
                float3 world : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                return o;
            }

            float frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv0);
                if (col.a > 0.5)
                {
                    float3 world_direction = _WorldSpaceCameraPos - i.world;
                    float world_distanceSqr = dot(world_direction, world_direction);
                    return world_distanceSqr;
                }
                else
                {
                    discard;
                    return 0.0; // hlsl compiler wants us to return something- never gets executed.
                }
            }

            ENDCG
        }
    }
    Fallback "Diffuse"
}