Shader "Unlit/VertexTracerSimple"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _LightmapTex("Texture", 2D) = "white" {}
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
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            
            struct Light
            {
                float3 position;
                float3 color;
                float  intensity;
                float  radius;
                uint   channel;
            };
            
            StructuredBuffer<Light> lights;
            uint lights_count;
            
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
                for (uint k = 0; k < lights_count; k++)
                {
                    Light light = lights[k];

                    float3 light_direction = normalize(light.position - i.world);
                    float light_distance = distance(i.world, light.position);

                    float diffusion = max(dot(i.normal, light_direction), 0);
                    float attenuation = saturate(1.0 - light_distance * light_distance / (light.radius * light.radius)) * light.intensity;

                    if (light.channel == 0)
                        light_final += light.color * attenuation * diffusion * map.r;
                    else if (light.channel == 1)
                        light_final += light.color * attenuation * diffusion * map.g;
                    else
                        light_final += light.color * attenuation * diffusion * map.b;
                }
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * half4(light_final, 1);
            }
            ENDCG
        }
    }
}
