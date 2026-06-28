Shader "Hidden/Dynamic Lighting/ShadowDepth"
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

        // every rendering pipeline - opaque:
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 world : TEXCOORD1;
            };

            float dynamic_lighting_shadow_depth_light_radius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 frag (v2f i) : SV_Target
            {
                float dist = distance(_WorldSpaceCameraPos, i.world) / dynamic_lighting_shadow_depth_light_radius;
                return float2(dist, dist * dist);
            }
            ENDCG
        }

        // dynamic lighting render pipeline - transparent and cutout:
        Pass
        {
            CGPROGRAM
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ShadowDepthTransparent.cginc"
            ENDCG
        }
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "Transparent" }
        LOD 100

        // built-in render pipeline - transparent and cutout:
        Pass
        {
            CGPROGRAM
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ShadowDepthTransparent.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType" = "TransparentCutout" }
        LOD 100

        // built-in render pipeline - transparent and cutout:
        Pass
        {
            CGPROGRAM
            #include_with_pragmas "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Shaders/Generators/ShadowDepthTransparent.cginc"
            ENDCG
        }
    }
    Fallback "Diffuse"
}