#if UNITY_6000_7_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The rendering engine of the Dynamic Lighting Render Pipeline (DLRP).</summary>
    public partial class DynamicLightingRenderPipeline : RenderPipeline
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

        /// <summary>The command buffer instance that is used for all drawing.</summary>
        private readonly CommandBuffer cmd;

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

        /// <summary>Unique identifier for shader property "_CameraOpaqueTexture".</summary>
        private readonly int shaderCameraOpaqueTextureId;

        /// <summary>Unique identifier for shader property "_CameraDepthTexture".</summary>
        private readonly int shaderCameraDepthTextureId;

#if UNITY_POSTPROCESSING
        /// <summary>Support for the Post Processing Stack v2 (com.unity.postprocessing).</summary>
        private readonly UnityEngine.Rendering.PostProcessing.PostProcessRenderContext postProcessRenderContext;
#endif

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

            cmd = new CommandBuffer();

            var dynamicLightingResources = DynamicLightingResources.Instance;
            shadowShader = dynamicLightingResources.shadowCameraDepthShader;
            photonShader = dynamicLightingResources.photonCubeShader;

            // prepare event arguments for common callbacks.
            onPreOpaqueEventArgs = new(this);
            onPostOpaqueEventArgs = new(this);
            onPreTransparentEventArgs = new(this);
            onPostTransparentEventArgs = new(this);
            onPostProcessEventArgs = new(this);

            shaderCameraOpaqueTextureId = Shader.PropertyToID("_CameraOpaqueTexture");
            shaderCameraDepthTextureId = Shader.PropertyToID("_CameraDepthTexture");

            // directional cascade shadow atlas globals.
            shaderTagIdShadowCaster = new ShaderTagId("ShadowCaster");
            dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
            dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
            cascadeCountId = Shader.PropertyToID("_CascadeCount");
            cascadeSplitsId = Shader.PropertyToID("_CascadeSplits");
            shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");

#if UNITY_POSTPROCESSING
            // prepare the post processing stack support.
            postProcessRenderContext = new();
#endif
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
                var cameraPixelWidth = camera.pixelWidth;
                var cameraPixelHeight = camera.pixelHeight;

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
                cmd.Clear();

                // ------------------------------------------------------------
                // create custom render targets for color and depth
                // ------------------------------------------------------------
                var cameraOpaqueTextureFormat = camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
                cmd.GetTemporaryRT(shaderCameraOpaqueTextureId, cameraPixelWidth, cameraPixelHeight, 0, FilterMode.Bilinear, cameraOpaqueTextureFormat);
                cmd.GetTemporaryRT(shaderCameraDepthTextureId, cameraPixelWidth, cameraPixelHeight, 24, FilterMode.Point, RenderTextureFormat.Depth);
                cmd.SetRenderTarget((RenderTargetIdentifier)shaderCameraOpaqueTextureId, (RenderTargetIdentifier)shaderCameraDepthTextureId);

                // ------------------------------------------------------------
                // clear the depth and color of the active render target
                // ------------------------------------------------------------
                ClearActiveRenderTarget(camera, cmd);

                // ------------------------------------------------------------
                // built-in render pipeline actions (precull unused in project)
                // ------------------------------------------------------------
                Camera.onPreCull?.Invoke(camera);

                // ------------------------------------------------------------
                // cull the scene as per camera settings
                // ------------------------------------------------------------
                var culling = ProcessCameraCulling(context, camera);

                // ------------------------------------------------------------
                // directional cascaded shadows (before camera properties)
                // ------------------------------------------------------------
                if (cameraType == CameraType.Default)
                    RenderDirectionalCascades(context, camera, culling, cmd);

                // ------------------------------------------------------------
                // restore the camera color/depth targets after shadow rendering
                // ------------------------------------------------------------
                cmd.SetRenderTarget((RenderTargetIdentifier)shaderCameraOpaqueTextureId, (RenderTargetIdentifier)shaderCameraDepthTextureId);

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

#if UNITY_POSTPROCESSING
                if (camera.TryGetComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>(out var postProcessLayer) && postProcessLayer.enabled)
                {
                    var b = Shader.PropertyToID("_HenrysPipelineFX");
                    cmd.GetTemporaryRT(b, cameraPixelWidth, cameraPixelHeight, 0, FilterMode.Bilinear, cameraOpaqueTextureFormat);

                    postProcessRenderContext.Reset();
                    postProcessRenderContext.camera = camera;
                    postProcessRenderContext.command = cmd;
                    postProcessRenderContext.source = shaderCameraOpaqueTextureId;
                    postProcessRenderContext.sourceFormat = cameraOpaqueTextureFormat;
                    postProcessRenderContext.destination = b;
                    postProcessRenderContext.flip = false;

                    postProcessLayer.Render(postProcessRenderContext);

                    cmd.Blit(b, BuiltinRenderTextureType.CameraTarget, DynamicLightingResources.Instance.blitColorDepthMaterial);
                    cmd.ReleaseTemporaryRT(b);
                }
                else
#endif
                cmd.Blit(shaderCameraOpaqueTextureId, BuiltinRenderTextureType.CameraTarget, DynamicLightingResources.Instance.blitColorDepthMaterial);

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
                // release temporary resources
                // ------------------------------------------------------------
                if (cameraType == CameraType.Default)
                    ReleaseDirectionalShadowAtlas(cmd);
                cmd.ReleaseTemporaryRT(shaderCameraOpaqueTextureId);
                cmd.ReleaseTemporaryRT(shaderCameraDepthTextureId);

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

        /// <summary>Clears the active render target (color or depth) to zero on all channels, depending on the camera settings.</summary>
        /// <param name="cmd">The command buffer to be appended.</param>
        private static unsafe void ClearActiveRenderTarget(Camera camera, CommandBuffer cmd)
        {
            switch (camera.clearFlags)
            {
                case CameraClearFlags.Skybox:
                case CameraClearFlags.SolidColor:

                    Color result = camera.backgroundColor;
                    if (DynamicLightManager.colorSpace == ColorSpace.Linear)
                        UMath.GammaToLinearSpace((Vector3*)&result);
                    cmd.ClearRenderTarget(true, true, result);
                    break;
                case CameraClearFlags.Depth:
                    cmd.ClearRenderTarget(true, false, Color.black);
                    break;
                case CameraClearFlags.Nothing:
                    break;
            }
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
