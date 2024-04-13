Shader "Dynamic Lighting/Metallic (Progressive Lightmapper)"
{
    // special thanks to https://learnopengl.com/PBR/Lighting

    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}
        [NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "black" {}
        _Metallic("Metallic (Fallback)", Range(0,1)) = 0
        _GlossMapScale("Smoothness", Range(0,1)) = 1
        [NoScaleOffset][Normal] _BumpMap("Normal map", 2D) = "bump" {}
        _BumpScale("Normal scale", Float) = 1
        [NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion strength", Range(0,1)) = 0.75
    }
    
    CustomEditor "AlpacaIT.DynamicLighting.Editor.MetallicShaderGUI"
    SubShader
    {
        Tags { "RenderType" = "Opaque" "DisableBatching" = "True" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #include "DynamicLightingMetallicPass.cginc"
            ENDCG
        }
    }
    Fallback "Diffuse"
}
