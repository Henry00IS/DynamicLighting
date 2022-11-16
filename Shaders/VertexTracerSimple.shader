// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/VertexTracerSimple"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LightmapTex("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float3 world : TEXCOORD2;
                float3 normal : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            sampler2D _LightmapTex;
            float4 _LightmapTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.uv1 = TRANSFORM_TEX(v.uv1, _LightmapTex);
                o.color = v.color;
                o.normal = v.normal;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv0) * i.color;
                fixed4 map = tex2D(_LightmapTex, i.uv1);


                float3 light_final = float3(0, 0, 0);
                for (int k = 0; k < 2; k++)
                {
                    float3 light_position = float3(-3, 3, -12.125);
                    float3 light_color = float3(1, 0.726, 0);
                    float light_intensity = 1 + _SinTime.w;
                    float light_radius = 6;

                    if (k == 1)
                    {
                        light_radius = 3;
                        light_position = float3(-3.375, 1.375, -8.75);
                        light_color = float3(0.1485849, 0.2623755, 0.5943396);
                        light_intensity = 2 + sin(_Time.w * 10);
                    }

                    float3 light_direction = normalize(light_position - i.world);
                    float light_distance = distance(i.world, light_position);

                    float diffusion = max(dot(i.normal, light_direction), 0);
                    float attenuation = saturate(1.0 - light_distance * light_distance / (light_radius * light_radius)) * light_intensity;

                    if (k == 0)
                        light_final += light_color * attenuation * diffusion * map.r;
                    else
                        light_final += light_color * attenuation * diffusion * map.g;
                }
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * half4(light_final, 1);
            }
            ENDCG
        }
    }
}
