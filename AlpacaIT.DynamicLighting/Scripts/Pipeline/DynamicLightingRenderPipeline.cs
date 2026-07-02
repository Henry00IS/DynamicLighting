#if UNITY_6000_7_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The rendering engine of the Dynamic Lighting Render Pipeline (DLRP).</summary>
    public class DynamicLightingRenderPipeline : RenderPipeline
    {
        /// <summary>
        /// Called before the <see cref="DynamicLightingRenderPipeline"/> renders opaque geometry.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingRenderPipelinePreOpaqueEventArgs> onPreOpaque;

        /// <summary>
        /// Called after the <see cref="DynamicLightingRenderPipeline"/> renders opaque geometry.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingRenderPipelinePostOpaqueEventArgs> onPostOpaque;

        /// <summary>
        /// Called before the <see cref="DynamicLightingRenderPipeline"/> renders transparent geometry.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingRenderPipelinePreTransparentEventArgs> onPreTransparent;

        /// <summary>
        /// Called after the <see cref="DynamicLightingRenderPipeline"/> renders transparent geometry.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingRenderPipelinePostTransparentEventArgs> onPostTransparent;

        /// <summary>
        /// Called when the <see cref="DynamicLightingRenderPipeline"/> renders post processing.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingRenderPipelinePostProcessEventArgs> onPostProcess;

        /// <summary>Cached event arguments to prevent making new instances.</summary>
        private DynamicLightingRenderPipelinePreOpaqueEventArgs onPreOpaqueEventArgs;

        /// <summary>Cached event arguments to prevent making new instances.</summary>
        private DynamicLightingRenderPipelinePostOpaqueEventArgs onPostOpaqueEventArgs;

        /// <summary>Cached event arguments to prevent making new instances.</summary>
        private DynamicLightingRenderPipelinePreTransparentEventArgs onPreTransparentEventArgs;

        /// <summary>Cached event arguments to prevent making new instances.</summary>
        private DynamicLightingRenderPipelinePostTransparentEventArgs onPostTransparentEventArgs;

        /// <summary>Cached event arguments to prevent making new instances.</summary>
        private DynamicLightingRenderPipelinePostProcessEventArgs onPostProcessEventArgs;

        /// <summary>Reference to the pipeline asset scriptable object.</summary>
        private readonly DynamicLightingRenderPipelineAsset renderPipelineAsset;

        /// <summary>
        /// Tells Unity which geometry to draw, based on its "LightMode" pass tag value. When a
        /// "LightMode" has not been set this defaults to "SRPDefaultUnlit" which is this shader tag identifier.
        /// </summary>
        private readonly ShaderTagId shaderTagIdSrpDefaultUnlit;

        /// <summary>The Dynamic Lighting related camera type.</summary>
        private enum CameraType
        {
            /// <summary>Default Unity camera that is not Dynamic Lighting related.</summary>
            Default,

            /// <summary>Special Dynamic Lighting shadow camera.</summary>
            Shadow,

            /// <summary>Special Dynamic Lighting photon camera.</summary>
            Photon,
        }

        /// <summary>The shadow replacement shader.</summary>
        private readonly Shader shadowShader;

        /// <summary>The photon replacement shader.</summary>
        private readonly Shader photonShader;

        /// <summary>
        /// Creates a new instance of the Dynamic Lighting Render Pipeline (DLRP).
        /// <para>
        /// Do not call this directly, instead create a <see
        /// cref="DynamicLightingRenderPipelineAsset"/> and assign it in your project settings.
        /// </para>
        /// </summary>
        /// <param name="renderPipelineAsset"></param>
        public DynamicLightingRenderPipeline(DynamicLightingRenderPipelineAsset renderPipelineAsset)
        {
            this.renderPipelineAsset = renderPipelineAsset;
            shaderTagIdSrpDefaultUnlit = new ShaderTagId("SRPDefaultUnlit");

            var dynamicLightingResources = DynamicLightingResources.Instance;
            shadowShader = dynamicLightingResources.shadowCameraDepthShader;
            photonShader = dynamicLightingResources.photonCubeShader;

            // prepare event arguments for common callbacks.
            onPreOpaqueEventArgs = new(this);
            onPostOpaqueEventArgs = new(this);
            onPreTransparentEventArgs = new(this);
            onPostTransparentEventArgs = new(this);
            onPostProcessEventArgs = new(this);
        }

        /// <summary>Called by Unity whenever rendering has to occur.</summary>
        /// <param name="context">The scriptable render context.</param>
        /// <param name="cameras">The collection of cameras that must be processed.</param>
        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            // iterate over all cameras that must be rendered:
            var camerasCount = cameras.Count;
            for (int i = 0; i < camerasCount; i++)
            {
                var camera = cameras[i];

                // ------------------------------------------------------------
                // built-in render pipeline actions (fixes material peviews).
                // ------------------------------------------------------------
                Camera.onPreRender?.Invoke(camera);

                // figure out the camera type (todo: build a better way without string comparisons).
                CameraType cameraType = camera.name switch
                {
                    "[Dynamic Lighting - Realtime Shadow Camera]" => CameraType.Shadow,
                    "[Dynamic Lighting - Photon Camera]" => CameraType.Photon,
                    _ => CameraType.Default,
                };

                // ------------------------------------------------------------
                // begin the rendering process
                // ------------------------------------------------------------
                using var cmd = new CommandBuffer();

                // ------------------------------------------------------------
                // clear the depth and color of the active render target
                // ------------------------------------------------------------
                ClearActiveRenderTarget(cmd);

                // ------------------------------------------------------------
                // built-in render pipeline actions (precull unused in project)
                // ------------------------------------------------------------
                Camera.onPreCull?.Invoke(camera);

                // ------------------------------------------------------------
                // cull the scene as per camera settings
                // ------------------------------------------------------------
                var culling = ProcessCameraCulling(context, camera);

                // ------------------------------------------------------------
                // update built-in global shader variables
                // ------------------------------------------------------------
                context.SetupCameraProperties(camera);

                // ------------------------------------------------------------
                // draw the opaque scene
                // ------------------------------------------------------------
                onPreOpaqueEventArgs.Setup(context, camera, culling, cmd);
                onPreOpaque?.Invoke(this, onPreOpaqueEventArgs);
                DrawOpaqueScene(context, camera, culling, cameraType, cmd);
                onPostOpaqueEventArgs.Setup(context, camera, culling, cmd);
                onPostOpaque?.Invoke(this, onPostOpaqueEventArgs);

                // ------------------------------------------------------------
                // draw the skybox
                // ------------------------------------------------------------
                DrawCameraSkybox(context, camera, cmd);

                // ------------------------------------------------------------
                // draw the transparent scene
                // ------------------------------------------------------------
                onPreTransparentEventArgs.Setup(context, camera, culling, cmd);
                onPreTransparent?.Invoke(this, onPreTransparentEventArgs);
                DrawTransparentScene(context, camera, culling, cameraType, cmd);
                onPostTransparentEventArgs.Setup(context, camera, culling, cmd);
                onPostTransparent?.Invoke(this, onPostTransparentEventArgs);
#if UNITY_EDITOR
                // ------------------------------------------------------------
                // draw editor gizmos before post processing
                // ------------------------------------------------------------
                DrawEditorGizmos(context, camera, cmd, GizmoSubset.PreImageEffects);
#endif
                // ------------------------------------------------------------
                // draw the post processing
                // ------------------------------------------------------------
                onPostProcessEventArgs.Setup(context, camera, culling, cmd);
                onPostProcess?.Invoke(this, onPostProcessEventArgs);
#if UNITY_EDITOR
                // ------------------------------------------------------------
                // draw the editor debug wireframe overlay
                // ------------------------------------------------------------
                DrawWireframeOverlay(context, camera, cmd);

                // ------------------------------------------------------------
                // draw editor gizmos after post processing
                // ------------------------------------------------------------
                DrawEditorGizmos(context, camera, cmd, GizmoSubset.PostImageEffects);
#endif
                // ------------------------------------------------------------
                // finish the rendering process
                // ------------------------------------------------------------
                context.ExecuteCommandBuffer(cmd);
                context.Submit();

                // ------------------------------------------------------------
                // built-in render pipeline actions (fixes material peviews).
                // ------------------------------------------------------------
                Camera.onPostRender?.Invoke(camera);
            }
        }

        private static void DrawWireframeOverlay(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            cmd.DrawRendererList(context.CreateWireOverlayRendererList(camera));
        }

        /// <summary>
        /// Gets the culling parameters from the given <paramref name="camera"/>. Then uses those
        /// parameters to perform a culling operation.
        /// </summary>
        /// <param name="context">The scriptable render context of the pipeline.</param>
        /// <param name="camera">The camera that is getting rendered.</param>
        /// <returns>The culling results for the camera.</returns>
        private static CullingResults ProcessCameraCulling(ScriptableRenderContext context, Camera camera)
        {
            // get the culling parameters from the current camera.
            camera.TryGetCullingParameters(out var cullingParameters);
            // use the culling parameters to perform a cull operation.
            var cullingResults = context.Cull(ref cullingParameters);
            // return the results of the culling operation.
            return cullingResults;
        }

        private static void DrawEditorGizmos(ScriptableRenderContext context, Camera camera, CommandBuffer cmd, GizmoSubset gizmoSubset)
        {
#if UNITY_EDITOR
            if (UnityEditor.Handles.ShouldRenderGizmos())
            {
                cmd.DrawRendererList(context.CreateGizmoRendererList(camera, gizmoSubset));
            }
#endif
        }

        private static void DrawCameraSkybox(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                cmd.DrawRendererList(context.CreateSkyboxRendererList(camera));
            }
        }

        private void DrawOpaqueScene(ScriptableRenderContext context, Camera camera, CullingResults culling, CameraType cameraType, CommandBuffer cmd)
        {
            // tell unity how to sort the geometry, based on the current camera.
            var sortingSettings = new SortingSettings(camera);

            // create a drawing settings struct that describes which geometry to draw and how to draw it.
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagIdSrpDefaultUnlit, sortingSettings);

            // tell Unity how to filter the culling results, to further specify which geometry to
            // draw use FilteringSettings.defaultValue to specify no filtering.
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            var rendererListDesc = new RendererListDesc(shaderTagIdSrpDefaultUnlit, culling, camera) { renderQueueRange = RenderQueueRange.opaque };

            SetReplacementShaderForSpecialCameras(cameraType, ref rendererListDesc);

            cmd.DrawRendererList(context.CreateRendererList(rendererListDesc));
        }

        private void DrawTransparentScene(ScriptableRenderContext context, Camera camera, CullingResults culling, CameraType cameraType, CommandBuffer cmd)
        {
            // tell unity how to sort the geometry, based on the current camera.
            var sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;

            // create a drawing settings struct that describes which geometry to draw and how to draw it.
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagIdSrpDefaultUnlit, sortingSettings);

            // tell Unity how to filter the culling results, to further specify which geometry to
            // draw use FilteringSettings.defaultValue to specify no filtering.
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            var rendererListDesc = new RendererListDesc(shaderTagIdSrpDefaultUnlit, culling, camera)
            {
                renderQueueRange = RenderQueueRange.transparent,
                sortingCriteria = SortingCriteria.CommonTransparent,
            };

            SetReplacementShaderForSpecialCameras(cameraType, ref rendererListDesc);

            cmd.DrawRendererList(context.CreateRendererList(rendererListDesc));
        }

        /// <summary>Clears the active render target (color and depth) to zero on all channels.</summary>
        /// <param name="cmd">The command buffer to be appended.</param>
        private static void ClearActiveRenderTarget(CommandBuffer cmd)
        {
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        }

        /// <summary>Sets up a replacement shader pass for special Dynamic Lighting cameras.</summary>
        /// <param name="cameraType">The Dynamic Lighting related camera type.</param>
        /// <param name="dynamicLightingDepthShader">The depth shader to be used.</param>
        /// <param name="dynamicLightingPhotonShader">The photon shader to be used.</param>
        /// <param name="rendererListDesc">The current renderer list description.</param>
        private void SetReplacementShaderForSpecialCameras(CameraType cameraType, ref RendererListDesc rendererListDesc)
        {
            switch (cameraType)
            {
                case CameraType.Shadow:
                    rendererListDesc.overrideShaderPassIndex = 1;
                    rendererListDesc.overrideShader = shadowShader;
                    break;
                case CameraType.Photon:
                    rendererListDesc.overrideShaderPassIndex = 1;
                    rendererListDesc.overrideShader = photonShader;
                    break;
            }
        }
    }
}
#endif
