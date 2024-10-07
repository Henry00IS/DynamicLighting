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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
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
                // store the normal in the green, blue and alpha channels.
                result.g = i.normal.x;
                result.b = i.normal.y;
                result.a = i.normal.z;

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
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
                // store the normal in the green, blue and alpha channels.
                result.g = i.normal.x;
                result.b = i.normal.y;
                result.a = i.normal.z;

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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv0 : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
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
                // store the normal in the green, blue and alpha channels.
                result.g = i.normal.x;
                result.b = i.normal.y;
                result.a = i.normal.z;

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