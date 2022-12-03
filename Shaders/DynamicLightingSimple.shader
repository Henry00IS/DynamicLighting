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
                o.uv1 = v.uv1;
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

                    // calculate the distance between the light source and the fragment.
                    float light_distance = distance(i.world, light.position);

                    // we can use the distance and guaranteed maximum light radius to early out.
                    // confirmed with NVIDIA Quadro K1000M doubling the framerate.
                    if (light_distance > light.radius) continue;

                    // if this renderer has a lightmap we use shadow bits otherwise it's a dynamic object.
                    // if this light is realtime we will skip this step.
                    float map = 1.0;
                    if (lightmap_resolution > 0 && light_is_dynamic(light))
                    {
                        uint shadow_channel = light_get_shadow_channel(light);

                        // retrieve the shadow bit at this position with bilinear filtering.
                        map = lightmap_sample_bilinear(i.uv1, shadow_channel);

                        // whenever the fragment is fully in shadow we can early out.
                        // confirmed with NVIDIA Quadro K1000M improving the framerate.
                        if (map == 0.0) continue;
                    }

                    // calculate the direction between the light source and the fragment.
                    float3 light_direction = normalize(light.position - i.world);

                    // spot lights determine whether we are in the light cone or outside.
                    if (light_is_spotlight(light))
                    {
                        // anything outside of the spot light can and must be skipped.
                        float2 spotlight = light_calculate_spotlight(light, light_direction);
                        if (spotlight.x <= light.outerCutoff)
                            continue;
                        map *= spotlight.y;
                    }
                    else if (light_is_discoball(light))
                    {
                        // anything outside of the spot lights can and must be skipped.
                        float2 spotlight = light_calculate_discoball(light, light_direction);
                        if (spotlight.x <= light.outerCutoff)
                            continue;
                        map *= spotlight.y;
                    }
                    if (light_is_watershimmer(light))
                    {
                        map *= light_calculate_watershimmer(light, i.world);
                    }

                    // a simple dot product with the normal gives us diffusion.
                    float diffusion = max(dot(i.normal, light_direction), 0);

                    // important attenuation that actually creates the point light with maximum radius.
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

#endif

            ENDCG
        }
    }
    Fallback "Diffuse"
}
