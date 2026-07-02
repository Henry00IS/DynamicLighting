#if UNITY_6000_7_OR_NEWER

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightingRenderPipeline.onPostProcess"/> event.
    /// </summary>
    public class DynamicLightingRenderPipelinePostProcessEventArgs : DynamicLightingRenderPipelineBaseEventArgs
    {
        /// <summary>Creates a new instance on the given pipeline.</summary>
        /// <param name="pipeline">The Dynamic Lighting Rendering Pipeline (DLRP) instance.</param>
        internal DynamicLightingRenderPipelinePostProcessEventArgs(DynamicLightingRenderPipeline pipeline)
            : base(pipeline) { }
    }
}
#endif
