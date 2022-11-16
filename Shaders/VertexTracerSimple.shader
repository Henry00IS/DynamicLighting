Shader "Unlit/VertexTracerSimple"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
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

            StructuredBuffer<uint> lightmap;
            uint lightmap_resolution;

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.uv1 = v.uv1;
                o.color = v.color;
                o.normal = v.normal;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float lightmap_pixel(uint2 uv, uint channel)
            {
                return (lightmap[uv.y * lightmap_resolution + uv.x] & (1 << channel)) > 0;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // calculate the lightmap pixel coordinates in advance.
                // clamp uv to 0-1 and multiply by resolution.
                uint2 lightmap_uv = saturate(i.uv1) * lightmap_resolution;


                //tex2D(_LightmapTex, i.uv1);
                //fixed4 map2 = tex2D(_LightmapTex, float2(i.uv1.x - (1.0 / 512.0), i.uv1.y));
                //fixed4 map3 = tex2D(_LightmapTex, float2(i.uv1.x + (1.0 / 512.0), i.uv1.y));
                //fixed4 map4 = tex2D(_LightmapTex, float2(i.uv1.x, i.uv1.y - (1.0 / 512.0)));
                //fixed4 map5 = tex2D(_LightmapTex, float2(i.uv1.x, i.uv1.y + (1.0 / 512.0)));
                //
                //map = (map + map2 + map3 + map4 + map5) / 5.0;

                float3 light_final = float3(0, 0, 0);
                for (uint k = 0; k < lights_count; k++)
                {
                    Light light = lights[k];

                    float map =  lightmap_pixel(lightmap_uv, light.channel);
                    float map2 = lightmap_pixel(lightmap_uv - uint2(1, 0), light.channel);
                    float map3 = lightmap_pixel(lightmap_uv + uint2(1, 0), light.channel);
                    float map4 = lightmap_pixel(lightmap_uv - uint2(0, 1), light.channel);
                    float map5 = lightmap_pixel(lightmap_uv + uint2(0, 1), light.channel);
                    map = (map + map2 + map3 + map4 + map5) / 5.0;

                    float3 light_direction = normalize(light.position - i.world);
                    float light_distance = distance(i.world, light.position);

                    float diffusion = max(dot(i.normal, light_direction), 0);
                    float attenuation = saturate(1.0 - light_distance * light_distance / (light.radius * light.radius)) * light.intensity;

                    light_final += light.color * attenuation * diffusion * map;
                }
                
                // sample the main texture.
                fixed4 col = tex2D(_MainTex, i.uv0) * i.color;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * half4(light_final, 1);
            }
            ENDCG
        }
    }
}
