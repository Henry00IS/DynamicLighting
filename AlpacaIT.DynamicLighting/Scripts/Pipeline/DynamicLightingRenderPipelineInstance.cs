using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AlpacaIT.DynamicLighting
{
    // 1. forward opaque rendering:
    // - unlit colors in R, G, B.
    // - normal directions in R, G, B.
    // - world positions in R, G, B.
    //
    // 2. compute shader binning:
    // - 1/16th rwstructuredbuffer processed as 16x16 bins.
    // - 256 AABB tests on all light sources in the scene in parallel.
    // - uses the world positions we gathered during forward rendering.
    // - add up to 32 light sources (very unlikely to ever fill up).
    //
    // 3. lighting pass:
    // - render per pixel lighting with dxr using the respective bins.

    public class DynamicLightingRenderPipelineInstance : RenderPipeline
    {
        private DynamicLightingRenderPipelineAsset renderPipelineAsset;

        public DynamicLightingRenderPipelineInstance(DynamicLightingRenderPipelineAsset renderPipelineAsset)
        {
            this.renderPipelineAsset = renderPipelineAsset;
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            // iterate over all cameras:
            var camerasCount = cameras.Count;
            for (int i = 0; i < camerasCount; i++)
            {
                var camera = cameras[i];

                //if (camera.name != "Main Camera" && camera.name != "SceneCamera")
                //	continue;
                //
                var cmd = new CommandBuffer();

                // clear the frame buffer.
                ClearActiveRenderTarget(cmd);

                // draw the opaque scene.
                context = DrawOpaqueScene(context, camera, cmd);

                // draw the skybox.
                context = DrawCameraSkybox(context, camera, cmd);

                // draw the transparent scene.
                context = DrawTransparentScene(context, camera, cmd);

                // draw the gizmos.
                context = DrawEditorGizmos(context, camera, cmd);

                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
                context.Submit();
            }
        }

        private static ScriptableRenderContext DrawEditorGizmos(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            var gizmoRendererList = context.CreateGizmoRendererList(camera, GizmoSubset.PostImageEffects);
            cmd.DrawRendererList(gizmoRendererList);
            return context;
        }

        private static ScriptableRenderContext DrawCameraSkybox(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
            {
                var skyboxRendererList = context.CreateSkyboxRendererList(camera);
                cmd.DrawRendererList(skyboxRendererList);
            }

            return context;
        }

        private static ScriptableRenderContext DrawOpaqueScene(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            // get the culling parameters from the current camera:
            camera.TryGetCullingParameters(out var cullingParameters);

            // use the culling parameters to perform a cull operation, and store the results.
            var cullingResults = context.Cull(ref cullingParameters);

            // update the value of built-in shader variables, based on the current camera.
            context.SetupCameraProperties(camera);

            // tell unity which geometry to draw, based on its LightMode pass tag value.
            ShaderTagId shaderTagId = new ShaderTagId("SRPDefaultUnlit");

            // tell unity how to sort the geometry, based on the current camera.
            var sortingSettings = new SortingSettings(camera);

            // create a drawing settings struct that describes which geometry to draw and how to draw it.
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);

            // tell Unity how to filter the culling results, to further specify which geometry to draw
            // use FilteringSettings.defaultValue to specify no filtering
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            var rendererListDesc = new RendererListDesc(shaderTagId, cullingResults, camera) { renderQueueRange = RenderQueueRange.opaque };

            if (camera.name == "[Dynamic Lighting - Realtime Shadow Camera]")
            {
                rendererListDesc.overrideMaterialPassIndex = 0;
                rendererListDesc.overrideMaterial = DynamicLightingResources.Instance.shadowCameraDepthMaterial;
            }

            if (camera.name == "[Dynamic Lighting - Photon Camera]")
            {
                rendererListDesc.overrideMaterialPassIndex = 0;
                rendererListDesc.overrideMaterial = DynamicLightingResources.Instance.photonCameraPhotonCubeMaterial;
            }

            var rendererList = context.CreateRendererList(rendererListDesc);
            cmd.DrawRendererList(rendererList);
            return context;
        }

        private static ScriptableRenderContext DrawTransparentScene(ScriptableRenderContext context, Camera camera, CommandBuffer cmd)
        {
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);
            context.SetupCameraProperties(camera);

            ShaderTagId shaderTagId = new ShaderTagId("SRPDefaultUnlit");

            var sortingSettings = new SortingSettings(camera);
            sortingSettings.criteria = SortingCriteria.CommonTransparent;

            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            var rendererListDesc = new RendererListDesc(shaderTagId, cullingResults, camera) { renderQueueRange = RenderQueueRange.transparent, sortingCriteria = SortingCriteria.CommonTransparent };

            if (camera.name == "[Dynamic Lighting - Realtime Shadow Camera]")
            {
                rendererListDesc.overrideMaterialPassIndex = 0;
                rendererListDesc.overrideMaterial = DynamicLightingResources.Instance.shadowCameraDepthMaterial;
            }

            if (camera.name == "[Dynamic Lighting - Photon Camera]")
            {
                rendererListDesc.overrideMaterialPassIndex = 0;
                rendererListDesc.overrideMaterial = DynamicLightingResources.Instance.photonCameraPhotonCubeMaterial;
            }

            var rendererList = context.CreateRendererList(rendererListDesc);
            cmd.DrawRendererList(rendererList);
            return context;
        }

        private static void ClearActiveRenderTarget(CommandBuffer cmd)
        {
            cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
        }
    }
}
