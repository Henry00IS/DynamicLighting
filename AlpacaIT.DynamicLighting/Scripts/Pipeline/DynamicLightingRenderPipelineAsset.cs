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

        [Header("Directional Shadows")]
        [Tooltip("Maximum distance from the camera at which directional cascade shadows are rendered.")]
        [Min(0.001f)]
        public float directionalShadowDistance = 150f;

        [Tooltip("Size of the square directional shadow atlas (power of two recommended).")]
        public int directionalShadowAtlasSize = 2048;

        [Tooltip("Number of shadow cascades (1-4). More cascades improve near-field resolution.")]
        [Range(1, 4)]
        public int directionalShadowCascades = 4;

        [Tooltip("Fraction of the max shadow distance used to fade shadows out near the far limit.")]
        [Range(0.001f, 1f)]
        public float directionalShadowDistanceFade = 0.1f;

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
