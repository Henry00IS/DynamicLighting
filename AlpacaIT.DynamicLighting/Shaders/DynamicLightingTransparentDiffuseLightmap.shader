Shader "Dynamic Lighting/Transparent (Progressive Lightmapper)"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB)", 2D) = "white" {}
    }
    
    CustomEditor "AlpacaIT.DynamicLighting.Editor.DefaultShaderGUI"
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching" = "True" }
        LOD 100

        Pass
        {
            // enable regular alpha blending for this pass.
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            CGPROGRAM
            #include "DynamicLightingTransparentDiffusePass.cginc"
            ENDCG
        }
    }
    Fallback "Legacy Shaders/Transparent/Diffuse"
}
