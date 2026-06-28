#if UNITY_6000_7_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // The CreateAssetMenu attribute lets you create instances of this class in the Unity Editor.
    [CreateAssetMenu(menuName = "Rendering/DynamicLightingRenderPipelineAsset")]
    public class DynamicLightingRenderPipelineAsset : RenderPipelineAsset
    {
        public override Type pipelineType => typeof(DynamicLightingRenderPipelineInstance);
        public override string renderPipelineShaderTag => "DynamicLightingRenderPipeline";
        public override Material defaultMaterial => DynamicLightingResources.Instance.pipelineDefaultMaterial;

        public override Shader defaultShader => DynamicLightingResources.Instance.pipelineDefaultShader;

        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
        protected override RenderPipeline CreatePipeline()
        {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering.
            return new DynamicLightingRenderPipelineInstance(this);
        }
    }
}
#endif
