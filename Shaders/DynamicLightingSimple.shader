Shader "Dynamic Lighting/Simple"
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
            #pragma multi_compile_fog
            #pragma shader_feature DYNAMIC_LIGHTING_UNLIT

            #include "UnityCG.cginc"
            #include "DynamicLighting.cginc"

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
                // as we need pixel coordinates doing the multiplication here saves time.
                // confirmed with NVIDIA Quadro K1000M improving the framerate.
                o.uv1 = v.uv1 * lightmap_resolution;
                o.color = v.color;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

#if DYNAMIC_LIGHTING_UNLIT

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv0);
            }

#else

            fixed4 frag (v2f i) : SV_Target
            {
                // iterate over every dynamic light in the scene:
                float3 light_final = float3(0, 0, 0);
                for (uint k = 0; k < dynamic_lights_count; k++)
                {
                    // get the current light from memory.
                    DynamicLight light = dynamic_lights[k];

                    // this generates the light with shadows and effects calculation declaring:
                    // 
                    // required: DynamicLight light; the current dynamic light source.
                    // float3 light_direction; normalized direction between the light source and the fragment.
                    // float light_distanceSqr; the square distance between the light source and the fragment.
                    // float NdotL; dot product with the normal and light direction (diffusion).
                    // float map; the computed shadow of this fragment with effects.
                    // float attenuation; the attenuation of the point light with maximum radius.
                    //
                    // it may also early out and continue the loop to the next light.
                    //
                    #define GENERATE_NORMAL i.normal
                    #include "GenerateLightProcessor.cginc"

                    // add this light to the final color of the fragment.
                    light_final += light.color * attenuation * NdotL * map;
                }

                // sample the main texture, multiply by the light and add vertex colors.
                fixed4 col = tex2D(_MainTex, i.uv0) * half4(dynamic_ambient_color + light_final, 1) * i.color;

                // apply fog.
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

#endif

            ENDCG
        }
    }
    Fallback "Diffuse"
}
