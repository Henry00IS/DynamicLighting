Shader "Dynamic Lighting/Diffuse (Progressive Lightmapper)"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
    }

    CustomEditor "AlpacaIT.DynamicLighting.Editor.DefaultShaderGUI"
    SubShader
    {
        Tags { "RenderType" = "Opaque" "DisableBatching" = "True" }
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
