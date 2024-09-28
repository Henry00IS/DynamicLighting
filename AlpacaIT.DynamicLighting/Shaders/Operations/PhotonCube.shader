Shader "Hidden/Dynamic Lighting/PhotonCube"
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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // packs a float into a byte so that -1.0 is 0 and +1.0 is 255.
            uint normalized_float_to_byte(float value)
            {
                return (1.0 + value) * 0.5 * 255.0;
            }

            float byte_to_normalized_float(uint byte)
            {
                return -1.0 + byte * (1.0 / 255.0) * 2.0;
            }

            // packs a float into a byte so that 0.0 is 0 and +1.0 is 255.
            uint saturated_float_to_byte(float value)
            {
                return value * 255;
            }

            float byte_to_saturated_float(float value)
            {
                return value * (1.0 / 255.0);
            }

            float pack_normalized_float4_into_float(float4 value)
            {
                //value = normalize(value);
                uint x8 = normalized_float_to_byte(value.x);
                uint y8 = normalized_float_to_byte(value.y);
                uint z8 = normalized_float_to_byte(value.z);
                uint w8 = normalized_float_to_byte(value.w);
                uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
                return asfloat(combined); // force the bit pattern into a float.
            }

            float4 unpack_normalized_float4_from_float(float value)
            {
                uint bytes = asuint(value);
                float4 result;
                result.x = byte_to_normalized_float((bytes >> 24) & 0xFF);
                result.y = byte_to_normalized_float((bytes >> 16) & 0xFF);
                result.z = byte_to_normalized_float((bytes >> 8) & 0xFF);
                result.w = byte_to_normalized_float(bytes & 0xFF);
                return result;
            }

            float pack_saturated_float4_into_float(float4 value)
            {
                value = saturate(value);
                uint x8 = saturated_float_to_byte(value.x);
                uint y8 = saturated_float_to_byte(value.y);
                uint z8 = saturated_float_to_byte(value.z);
                uint w8 = saturated_float_to_byte(value.w);
                uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
                return asfloat(combined); // force the bit pattern into a float.
            }

            float4 unpack_saturated_float4_from_float(float value)
            {
                uint bytes = asuint(value);
                float4 result;
                result.x = byte_to_saturated_float((bytes >> 24) & 0xFF);
                result.y = byte_to_saturated_float((bytes >> 16) & 0xFF);
                result.z = byte_to_saturated_float((bytes >> 8) & 0xFF);
                result.w = byte_to_saturated_float(bytes & 0xFF);
                return result;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 result;

                // calculate the unnormalized direction between the light source and the fragment.
                float3 light_direction = _WorldSpaceCameraPos - i.world;

                // properly normalize the direction between the light source and the fragment.
                light_direction = normalize(light_direction);

                // as the distance from the light increases, so does the chance that the world positions
                // are behind the geometry when sampled from the cubemap due to the low resolution.
                // we try to wiggle them back out by moving them closer towards the light source as well
                // as offsetting them by the geometry normal.
                float light_distance = distance(_WorldSpaceCameraPos, i.world);
                float bias = max(light_distance * 0.001, 0.001);
                light_distance = distance(_WorldSpaceCameraPos, i.world + light_direction * bias + i.normal * bias);

                // store the distance in the red channel and a small normal offset for raycasting on the cpu.
                result.r = light_distance;
                // store the normal as 3 bytes in the green channel (1 byte unused).
                result.g = pack_normalized_float4_into_float(float4(i.normal, 0));
                // store the main texture multiplied with material color and vertex color as 3 bytes in the blue channel (1 byte unused).
                result.b = pack_saturated_float4_into_float(float4(tex2D(_MainTex, i.uv0).rgb * _Color.rgb * i.color, 0));

                // unused:
                result.a = 1.0;

                return result;
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

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv0 : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            // packs a float into a byte so that -1.0 is 0 and +1.0 is 255.
            uint normalized_float_to_byte(float value)
            {
                return (1.0 + value) * 0.5 * 255.0;
            }

            float byte_to_normalized_float(uint byte)
            {
                return -1.0 + byte * (1.0 / 255.0) * 2.0;
            }

            // packs a float into a byte so that 0.0 is 0 and +1.0 is 255.
            uint saturated_float_to_byte(float value)
            {
                return value * 255;
            }

            float byte_to_saturated_float(float value)
            {
                return value * (1.0 / 255.0);
            }

            float pack_normalized_float4_into_float(float4 value)
            {
                //value = normalize(value);
                uint x8 = normalized_float_to_byte(value.x);
                uint y8 = normalized_float_to_byte(value.y);
                uint z8 = normalized_float_to_byte(value.z);
                uint w8 = normalized_float_to_byte(value.w);
                uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
                return asfloat(combined); // force the bit pattern into a float.
            }

            float4 unpack_normalized_float4_from_float(float value)
            {
                uint bytes = asuint(value);
                float4 result;
                result.x = byte_to_normalized_float((bytes >> 24) & 0xFF);
                result.y = byte_to_normalized_float((bytes >> 16) & 0xFF);
                result.z = byte_to_normalized_float((bytes >> 8) & 0xFF);
                result.w = byte_to_normalized_float(bytes & 0xFF);
                return result;
            }

            float pack_saturated_float4_into_float(float4 value)
            {
                value = saturate(value);
                uint x8 = saturated_float_to_byte(value.x);
                uint y8 = saturated_float_to_byte(value.y);
                uint z8 = saturated_float_to_byte(value.z);
                uint w8 = saturated_float_to_byte(value.w);
                uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
                return asfloat(combined); // force the bit pattern into a float.
            }

            float4 unpack_saturated_float4_from_float(float value)
            {
                uint bytes = asuint(value);
                float4 result;
                result.x = byte_to_saturated_float((bytes >> 24) & 0xFF);
                result.y = byte_to_saturated_float((bytes >> 16) & 0xFF);
                result.z = byte_to_saturated_float((bytes >> 8) & 0xFF);
                result.w = byte_to_saturated_float(bytes & 0xFF);
                return result;
            }

            float4 frag (v2f i) : SV_Target
            {
                float4 result;

                fixed4 col = tex2D(_MainTex, i.uv0);

                // calculate the unnormalized direction between the light source and the fragment.
                float3 light_direction = _WorldSpaceCameraPos - i.world;

                // properly normalize the direction between the light source and the fragment.
                light_direction = normalize(light_direction);

                // as the distance from the light increases, so does the chance that the world positions
                // are behind the geometry when sampled from the cubemap due to the low resolution.
                // we try to wiggle them back out by moving them closer towards the light source as well
                // as offsetting them by the geometry normal.
                float light_distance = distance(_WorldSpaceCameraPos, i.world);
                float bias = max(light_distance * 0.001, 0.001);
                light_distance = distance(_WorldSpaceCameraPos, i.world + light_direction * bias + i.normal * bias);

                // store the distance in the red channel and a small normal offset for raycasting on the cpu.
                result.r = light_distance;
                // store the normal as 3 bytes in the green channel (1 byte unused).
                result.g = pack_normalized_float4_into_float(float4(i.normal, 0));
                // store the main texture multiplied with material color and vertex color as 3 bytes in the blue channel (1 byte unused).
                result.b = pack_saturated_float4_into_float(float4(col.rgb * _Color.rgb * i.color, 0));

                // unused:
                result.a = 1.0;

                // discard fragments for transparent textures so that light can shine through it.
                if (col.a > 0.5)
                {
                    return result;
                }
                else
                {
                    result.r = 0.0;
                    discard;
                }

                return result;
            }

            ENDCG
        }
    }
    Fallback "Diffuse"
}