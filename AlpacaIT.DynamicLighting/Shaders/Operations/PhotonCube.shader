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
            #include "../Common.cginc"

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
            #include "../Common.cginc"

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

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "TransparentCutout" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            #include "../Common.cginc"

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