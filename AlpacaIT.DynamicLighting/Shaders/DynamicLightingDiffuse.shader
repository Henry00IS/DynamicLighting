Shader "Dynamic Lighting/Diffuse"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
    }

    CustomEditor "AlpacaIT.DynamicLighting.Editor.DefaultShaderGUI"
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #include "DynamicLightingDiffusePass.cginc"
            ENDCG
        }
    }
    Fallback "Diffuse"
}
