#if UNITY_6000_7_OR_NEWER

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightingRenderPipeline.onPreTransparent"/> event.
    /// </summary>
    public class DynamicLightingRenderPipelinePreTransparentEventArgs : DynamicLightingRenderPipelineBaseEventArgs
    {
        /// <summary>Creates a new instance on the given pipeline.</summary>
        /// <param name="pipeline">The Dynamic Lighting Rendering Pipeline (DLRP) instance.</param>
        internal DynamicLightingRenderPipelinePreTransparentEventArgs(DynamicLightingRenderPipeline pipeline)
            : base(pipeline) { }
    }
}
#endif
