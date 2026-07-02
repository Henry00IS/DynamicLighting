#if UNITY_6000_7_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// The Dynamic Lighting Render Pipeline (DLRP) Asset. This contains the pipeline rendering settings.
    /// </summary>
    [CreateAssetMenu(menuName = "Dynamic Lighting/Render Pipeline Asset")]
    public class DynamicLightingRenderPipelineAsset : RenderPipelineAsset
    {
        public override Type pipelineType => typeof(DynamicLightingRenderPipeline);
        public override string renderPipelineShaderTag => "DynamicLightingRenderPipeline";
        public override Material defaultMaterial => DynamicLightingResources.Instance.pipelineDefaultMaterial;
        public override Shader defaultShader => DynamicLightingResources.Instance.pipelineDefaultShader;

        // Unity calls this method before rendering the first frame.
        // If a setting on the Render Pipeline Asset changes, Unity destroys the current Render Pipeline Instance and calls this method again before rendering the next frame.
        protected override RenderPipeline CreatePipeline()
        {
            // Instantiate the Render Pipeline that this custom SRP uses for rendering.
            return new DynamicLightingRenderPipeline(this);
        }
    }
}
#endif
