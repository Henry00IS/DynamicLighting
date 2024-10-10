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

            float2 frag (v2f i) : SV_Target
            {
                float2 result;

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
                // store the compressed normal in the green channel (8 bits unused).
                result.g = asfloat(minivector3(i.normal));

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
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                float2 result;
                
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
                // store the compressed normal in the green channel (8 bits unused).
                result.g = asfloat(minivector3(i.normal));

                // discard fragments for transparent textures so that light can shine through it.
                float textureAlpha = texture_alpha_sample_gaussian5(_MainTex, _MainTex_TexelSize, i.uv0);
                if (textureAlpha > 0.5)
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
            float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                float2 result;

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
                // store the compressed normal in the green channel (8 bits unused).
                result.g = asfloat(minivector3(i.normal));

                // discard fragments for transparent textures so that light can shine through it.
                float textureAlpha = texture_alpha_sample_gaussian5(_MainTex, _MainTex_TexelSize, i.uv0);
                if (textureAlpha > 0.5)
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