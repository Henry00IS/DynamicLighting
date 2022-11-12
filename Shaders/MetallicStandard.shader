Shader "Game/MetallicStandard"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _MetallicGlossMap("Metallic", 2D) = "white" {}
        _BumpMap("Normal map", 2D) = "bump" {}
        _WorldCube("World Cubemap", CUBE) = "" {}
        //_Glossiness ("Smoothness", Range(0,1)) = 0.5
        //_Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Lambert fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicGlossMap;
        sampler2D _BumpMap;
        samplerCUBE _WorldCube;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_MetallicGlossMap;
            float2 uv_BumpMap;
            float3 worldRefl;
            INTERNAL_DATA
        };

        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            fixed4 metallic = tex2D(_MetallicGlossMap, IN.uv_MetallicGlossMap) * _Color;
            fixed4 normal = tex2D(_BumpMap, IN.uv_BumpMap);
            
            o.Normal = UnpackNormal(normal);
            fixed4 world = texCUBE(_WorldCube, WorldReflectionVector(IN, o.Normal)) * _Color;
            o.Albedo = c.rgb + (world.rgb * metallic.rgb);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
