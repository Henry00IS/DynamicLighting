#if UNITY_6000_7_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// The base class for rendering related <see cref="EventArgs"/> of the Dynamic Lighting
    /// Rendering Pipeline (DLRP).
    /// </summary>
    public abstract class DynamicLightingRenderPipelineBaseEventArgs : EventArgs
    {
        /// <summary>Gets the active Dynamic Lighting Rendering Pipeline (DLRP) instance.</summary>
        public DynamicLightingRenderPipeline pipeline { get; protected set; }

        /// <summary>Gets the state and drawing commands that a custom rendering pipeline uses.</summary>
        public ScriptableRenderContext context { get; protected set; }

        /// <summary>Gets the active camera that is getting rendered.</summary>
        public Camera camera { get; protected set; }

        /// <summary>Gets the culling results of the active camera.</summary>
        public CullingResults culling { get; protected set; }

        /// <summary>Gets the current list of graphics commands to execute.</summary>
        public CommandBuffer cmd { get; protected set; }

        /// <summary>Creates a new instance on the given pipeline.</summary>
        /// <param name="pipeline">The Dynamic Lighting Rendering Pipeline (DLRP) instance.</param>
        internal DynamicLightingRenderPipelineBaseEventArgs(DynamicLightingRenderPipeline pipeline)
        {
            this.pipeline = pipeline;
        }

        /// <summary>Updates event parameters so that this instance can be recycled.</summary>
        internal virtual void Setup(ScriptableRenderContext context, Camera camera, CullingResults culling, CommandBuffer cmd)
        {
            this.context = context;
            this.camera = camera;
            this.culling = culling;
            this.cmd = cmd;
        }
    }
}
#endif
