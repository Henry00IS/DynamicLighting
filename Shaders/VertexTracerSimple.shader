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
            
            struct DynamicLight
            {
                float3 position;
                float3 color;
                float  intensity;
                float  radius;
                uint   channel;
            };
            
            StructuredBuffer<DynamicLight> dynamic_lights;
            uint dynamic_lights_count;

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
                // clamp uv to 0-1 and multiply by resolution cast to uint.
                uint2 lightmap_uv = saturate(i.uv1) * lightmap_resolution;

                // iterate over every light in the scene:
                float3 light_final = float3(0, 0, 0);
                for (uint k = 0; k < dynamic_lights_count; k++)
                {
                    // get the current light from memory.
                    DynamicLight light = dynamic_lights[k];

                    // calculate the distance between the light source and the fragment.
                    float light_distance = distance(i.world, light.position);

                    // we can use the distance and guaranteed maximum light radius to early out.
                    // confirmed with NVIDIA Quadro K1000M doubling the framerate.
                    if (light_distance > light.radius) continue;

                    // calculate the direction between the light source and the fragment.
                    float3 light_direction = normalize(light.position - i.world);

                    // x x x
                    // x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
                    // x x x
                    float map  = lightmap_pixel(lightmap_uv, light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2(-1, -1), light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2( 0, -1), light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2( 1, -1), light.channel);
                          
                          map += lightmap_pixel(lightmap_uv + uint2(-1, 0), light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2( 1, 0), light.channel);
                          
                          map += lightmap_pixel(lightmap_uv + uint2(-1, 1), light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2( 0, 1), light.channel);
                          map += lightmap_pixel(lightmap_uv + uint2( 1, 1), light.channel);
                    map /= 9.0;

                    // a simple dot product with the normal gives us diffusion.
                    float diffusion = max(dot(i.normal, light_direction), 0);

                    // important attenuation that actually creates the spot light with maximum radius.
                    float attenuation = saturate(1.0 - light_distance * light_distance / (light.radius * light.radius)) * light.intensity;

                    // add this light to the final color of the fragment.
                    light_final += light.color * attenuation * diffusion * map;
                }
                
                // sample the main texture, multiply by the light and add vertex colors.
                fixed4 col = tex2D(_MainTex, i.uv0) * half4(light_final, 1) * i.color;

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
