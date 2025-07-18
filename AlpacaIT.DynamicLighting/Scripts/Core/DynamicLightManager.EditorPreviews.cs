﻿#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements unity editor specific code, to repair material previews, so that they are
    // unaffected by light sources and settings present in the current scene. this also solves the
    // project browser real-time thumbnail generation, but does not solve black persistent
    // thumbnails upon saving the project.

    public partial class DynamicLightManager
    {
        /// <summary>Whether editor previews have just been rendered (dirty state).</summary>
        private bool editorPreviewsRendered = false;

        /// <summary>
        /// The <see cref="ComputeBuffer"/> used to replace <see cref="dynamicLightsBuffer"/> during previews.
        /// </summary>
        private ComputeBuffer editorPreviewsDynamicLightsBuffer;

        /// <summary>Stores the original value of the shader keyword state "DYNAMIC_LIGHTING_LIT".</summary>
        private bool editorPreviewsShaderLitEnabled;

        /// <summary>Stores the original value of the shader keyword state "DYNAMIC_LIGHTING_BVH".</summary>
        private bool editorPreviewsShaderBvhEnabled;

        /// <summary>Stores the original value of the shader property "dynamic_lights_count".</summary>
        private int editorPreviewsShaderDynamicLightsCount;

        /// <summary>Stores the original value of the shader property "realtime_lights_count".</summary>
        private int editorPreviewsShaderRealtimeLightsCount;

        /// <summary>Executes before a camera starts rendering.</summary>
        /// <param name="camera">The camera that is about to render.</param>
#if UNITY_PIPELINE_URP

        private void EditorOnPreRenderCallback(ScriptableRenderContext scriptableRenderContext, Camera camera)
#else
        private void EditorOnPreRenderCallback(Camera camera)
#endif
        {
            // we require that we are fully initialized.
            if (!isInitialized) return;

            // we want to affect the preview camera used for thumbnails and material inspectors.
            if (camera.cameraType != CameraType.Preview || camera.name != "Preview Scene Camera") return;

            // as we now change shader variables we have to clean it up later.
            editorPreviewsRendered = true;

            // create two directional lights for the preview, that match the directional lights of
            // unity editor's default material preview scene.
            var previewShaderDynamicLights = new ShaderDynamicLight[2];
            EditorPreviewsWriteDirectionalLight(ref previewShaderDynamicLights[0], Quaternion.Euler(new Vector3(50f, 50f, 0f)), new Vector3(0.769f, 0.769f, 0.769f), 2.0f);
            EditorPreviewsWriteDirectionalLight(ref previewShaderDynamicLights[1], Quaternion.Euler(new Vector3(340f, 218f, 177f)), new Vector3(0.280f, 0.280f, 0.315f), 1.0f);

            // upload the preview light sources to the graphics card.
            editorPreviewsDynamicLightsBuffer = new ComputeBuffer(previewShaderDynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);
            editorPreviewsDynamicLightsBuffer.SetData(previewShaderDynamicLights);
            ShadersSetGlobalDynamicLights(editorPreviewsDynamicLightsBuffer);

            // upload shader values to the graphics card.
            editorPreviewsShaderDynamicLightsCount = shadersLastDynamicLightsCount;
            editorPreviewsShaderRealtimeLightsCount = shadersLastRealtimeLightsCount;
            ShadersSetGlobalDynamicLightsCount(0);
            ShadersSetGlobalRealtimeLightsCount(2);
            ShadersSetGlobalDynamicAmbientColor(new Color(0.01568628f, 0.01568628f, 0.01568628f));

            // apply shader keywords.
            editorPreviewsShaderLitEnabled = shadersLastKeywordLitEnabled;
            if (!editorPreviewsShaderLitEnabled)
                ShadersSetKeywordLitEnabled(true);

            // apply shader keywords.
            editorPreviewsShaderBvhEnabled = shadersKeywordBvhEnabled;
            if (editorPreviewsShaderBvhEnabled)
                shadersKeywordBvhEnabled = false;
        }

        /// <summary>Executes after a camera stops rendering.</summary>
        /// <param name="camera">The camera that has finished rendering.</param>
#if UNITY_PIPELINE_URP

        private void EditorOnPostRenderCallback(ScriptableRenderContext scriptableRenderContext, Camera camera)
#else
        private void EditorOnPostRenderCallback(Camera camera)
#endif
        {
            // we require that we are fully initialized.
            if (!isInitialized) return;

            // we want to affect the preview camera used for thumbnails and material inspectors.
            if (camera.cameraType != CameraType.Preview || camera.name != "Preview Scene Camera") return;

            // only restore the shader state if we rendered a preview.
            if (!editorPreviewsRendered) return;
            editorPreviewsRendered = false;

            // delete the preview light sources from the graphics card.
            ShadersSetGlobalDynamicLights(dynamicLightsBuffer);
            if (editorPreviewsDynamicLightsBuffer.IsValid())
            {
                editorPreviewsDynamicLightsBuffer.Dispose();
                editorPreviewsDynamicLightsBuffer = null;
            }

            // restore shader values on the graphics card.
            ShadersSetGlobalDynamicLightsCount(editorPreviewsShaderDynamicLightsCount);
            ShadersSetGlobalRealtimeLightsCount(editorPreviewsShaderRealtimeLightsCount);
            ShadersSetGlobalDynamicAmbientColor(ambientColor);

            // restore shader keywords.
            if (!editorPreviewsShaderLitEnabled)
                ShadersSetKeywordLitEnabled(false);

            // restore shader keywords.
            if (editorPreviewsShaderBvhEnabled)
                shadersKeywordBvhEnabled = true;
        }

        /// <summary>
        /// Writes a dynamic light source at 2.5km distance into the given <paramref name="shaderDynamicLight"/>.
        /// </summary>
        /// <param name="shaderDynamicLight">The <see cref="ShaderDynamicLight"/> to be modified.</param>
        /// <param name="rotation">The rotation of the directional light (matches Unity Editor).</param>
        /// <param name="color">The color in Red, Green, Blue of the directional light.</param>
        /// <param name="intensity">The intensity of the directional light.</param>
        private void EditorPreviewsWriteDirectionalLight(ref ShaderDynamicLight shaderDynamicLight, Quaternion rotation, Vector3 color, float intensity)
        {
            shaderDynamicLight.color = color;
            shaderDynamicLight.intensity = intensity;
            shaderDynamicLight.radiusSqr = 10000.0f * 10000.0f;
            shaderDynamicLight.channel = 32;
            shaderDynamicLight.position = rotation * new Vector3(0f, 0f, -2500f); // 2.5km
        }
    }
}

#endif