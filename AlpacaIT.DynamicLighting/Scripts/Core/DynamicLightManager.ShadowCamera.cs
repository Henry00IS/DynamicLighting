﻿using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements real-time shadow generation using a shadow camera that builds cubemaps.

    public partial class DynamicLightManager
    {
        /// <summary>All of the shadow camera orientations to take cubemap frames.</summary>
        private static readonly Quaternion[] shadowCameraOrientations = new Quaternion[]
        {
            Quaternion.LookRotation(Vector3.left, Vector3.up),
            Quaternion.LookRotation(Vector3.right, Vector3.up),
            Quaternion.LookRotation(Vector3.down, Vector3.back),
            Quaternion.LookRotation(Vector3.up, Vector3.forward),
            Quaternion.LookRotation(Vector3.back, Vector3.up),
            Quaternion.LookRotation(Vector3.forward, Vector3.up),
        };

        /// <summary>The <see cref="GameObject"/> of the shadow camera.</summary>
        private GameObject shadowCameraGameObject;

        /// <summary>The <see cref="Transform"/> of the shadow camera <see cref="GameObject"/>.</summary>
        private Transform shadowCameraTransform;

        /// <summary>The <see cref="Camera"/> used for capturing shadow frames in the scene.</summary>
        private Camera shadowCamera;

        /// <summary>The array of shadow camera cubemap textures.</summary>
        private RenderTexture shadowCameraCubemaps;

        /// <summary>The replacement shader used by the shadow camera to render depth.</summary>
        private Shader shadowCameraDepthShader;

        /// <summary>Used to fetch 16-bit floating point textures for the shadow camera rendering.</summary>
        private RenderTextureDescriptor shadowCameraRenderTextureDescriptor;

        /// <summary>Temporary render texture used by the shadow camera to render the scene.</summary>
        private RenderTexture shadowCameraRenderTexture;

        /// <summary>Reference to <see cref="DynamicLightingResources.guassianBlurMaterial"/>.</summary>
        private Material shadowCameraGuassianBlurMaterial;

        private bool shadowCameraReady;
        private const int shadowCameraResolution = 512; // anything above 1024 will fail.
        private const int shadowCameraCubemapBudget = 16; // 300 is around the maximum, from there DX11 D3D error 0x80070057.
        private int shadowCameraCubemapIndex;

        /// <summary>Initialization of the DynamicLightManager.ShadowCamera partial class.</summary>
        private void ShadowCameraInitialize()
        {
            var dynamicLightingResources = DynamicLightingResources.Instance;
            shadowCameraDepthShader = dynamicLightingResources.shadowCameraDepthShader;
            shadowCameraGuassianBlurMaterial = dynamicLightingResources.guassianBlurMaterial;

#if UNITY_2021_3_OR_NEWER && !UNITY_2021_3_0 && !UNITY_2021_3_1 && !UNITY_2021_3_2 && !UNITY_2021_3_3 && !UNITY_2021_3_4 && !UNITY_2021_3_5 && !UNITY_2021_3_6 && !UNITY_2021_3_7 && !UNITY_2021_3_8 && !UNITY_2021_3_9 && !UNITY_2021_3_10 && !UNITY_2021_3_11 && !UNITY_2021_3_12 && !UNITY_2021_3_13 && !UNITY_2021_3_14 && !UNITY_2021_3_15 && !UNITY_2021_3_16 && !UNITY_2021_3_17 && !UNITY_2021_3_18 && !UNITY_2021_3_19 && !UNITY_2021_3_20 && !UNITY_2021_3_21 && !UNITY_2021_3_22 && !UNITY_2021_3_23 && !UNITY_2021_3_24 && !UNITY_2021_3_25 && !UNITY_2021_3_26 && !UNITY_2021_3_27 && !UNITY_2022_1 && !UNITY_2022_2 && !UNITY_2022_3_0 && !UNITY_2022_3_1 && !UNITY_2022_3_2 && !UNITY_2022_3_3 && !UNITY_2022_3_4 && !UNITY_2022_3_5
            shadowCameraRenderTextureDescriptor = new RenderTextureDescriptor(shadowCameraResolution, shadowCameraResolution, RenderTextureFormat.RGFloat, 16, 0, RenderTextureReadWrite.Linear);
#else
            shadowCameraRenderTextureDescriptor = new RenderTextureDescriptor(shadowCameraResolution, shadowCameraResolution, RenderTextureFormat.RGFloat, 16, 0);
#endif
            shadowCameraRenderTextureDescriptor.autoGenerateMips = false;

            // create the shadow camera game object.
            shadowCameraGameObject = new GameObject("[Dynamic Lighting - Realtime Shadow Camera]");
            shadowCameraGameObject.SetActive(false);

            // this is very important, otherwise it keeps adding more instances when switching
            // between editor and play mode as it saves the scene.
            shadowCameraGameObject.hideFlags = HideFlags.HideAndDontSave;

            // parent it under our instance to keep things clean.
            shadowCameraTransform = shadowCameraGameObject.transform;
            shadowCameraTransform.parent = transform;

            // create the shadow camera.
            shadowCamera = shadowCameraGameObject.AddComponent<Camera>();
            shadowCamera.enabled = false;
            // the 90 degrees field of view is exactly one side of a cubemap.
            shadowCamera.fieldOfView = 90;
            // this acts as the inner radius around the camera, which when objects clip inside, the
            // shadows will visually glitch, so we keep it very small.
            shadowCamera.nearClipPlane = 0.01f;
            // we do not wish to waste time rendering the skybox.
            shadowCamera.clearFlags = CameraClearFlags.Depth;
            // only useful in the editor previews, but programmers can filter by this category.
            shadowCamera.cameraType = CameraType.Reflection;
            // we render depth using a special shader.
            shadowCamera.SetReplacementShader(shadowCameraDepthShader, "RenderType");

            // create shadow cubemap array.
            shadowCameraCubemaps = new RenderTexture(shadowCameraRenderTextureDescriptor);
            shadowCameraCubemaps.dimension = TextureDimension.CubeArray;
            shadowCameraCubemaps.useMipMap = false;
            shadowCameraCubemaps.autoGenerateMips = false;
            shadowCameraCubemaps.volumeDepth = 6 * shadowCameraCubemapBudget;
            shadowCameraCubemaps.Create();

            Shader.SetGlobalTexture("shadow_cubemaps", shadowCameraCubemaps);
        }

        /// <summary>Cleanup of the DynamicLightManager.ShadowCamera partial class.</summary>
        private void ShadowCameraCleanup()
        {
            // destroy the shadow camera game object.
            DestroyImmediate(shadowCameraGameObject);

            // release the unity resources we no longer need.
            shadowCameraCubemaps.Release();
            shadowCameraCubemaps = null;
        }

        /// <summary>Called before the lights are processed for rendering.</summary>
        private void ShadowCameraEnsureReady()
        {
            if (shadowCameraReady) return;
            shadowCameraReady = true;

            shadowCameraCubemapIndex = 0;

            // get a temporary render texture.
            shadowCameraRenderTexture = RenderTexture.GetTemporary(shadowCameraRenderTextureDescriptor);

            // have the shadow camera render to the render texture.
            shadowCamera.targetTexture = shadowCameraRenderTexture;
        }

        /// <summary>Called after the lights have been processed for rendering.</summary>
        private void ShadowCameraPostUpdate()
        {
            if (!shadowCameraReady) return;
            shadowCameraReady = false;

            // remove the render texture reference from the camera.
            shadowCamera.targetTexture = null;

            // release the temporary render textures.
            RenderTexture.ReleaseTemporary(shadowCameraRenderTexture);
            shadowCameraRenderTexture = null;
        }

        private unsafe void ShadowCameraProcessLight(ShaderDynamicLight* shaderLight, DynamicLight light)
        {
            // integrated graphics does not render realtime shadows.
            if (runtimeQuality == DynamicLightingRuntimeQuality.IntegratedGraphics) return;

            // the shader light must still be active.
            if (shaderLight->radiusSqr == -1.0f) return;

            // the light must have realtime shadows enabled.
            if (light.lightShadows != DynamicLightShadowMode.RealtimeShadows) return;

            // we ran out of cubemaps.
            if (shadowCameraCubemapIndex >= shadowCameraCubemapBudget)
                return;

            // if the light can not be seen by the camera we do not calculate/activate realtime shadows.
            if (!MathEx.CheckSphereIntersectsFrustum(cameraFrustumPlanes, shaderLight->position, light.lightRadius))
                return;

            ShadowCameraRenderLight(shaderLight, light);
        }

        private unsafe void ShadowCameraRenderLight(ShaderDynamicLight* shaderLight, DynamicLight light)
        {
            // delay initialization until necessary.
            ShadowCameraEnsureReady();

            // we move the camera to the light source.
            shadowCameraTransform.position = shaderLight->position;

            // we do not need shadows beyond the light radius.
            shadowCamera.farClipPlane = light.lightRadius * 3f;
            shadowCamera.cullingMask = realtimeShadowLayers;

            // set the current light radius for the depth shader.
            shadersPropertyShadowDepthLightRadius = light.lightRadius;

            // render the 6 sides of the cubemap:
            for (int face = 0; face < 6; face++)
            {
                shadowCameraTransform.rotation = shadowCameraOrientations[face];

                // use the depth replacement shader to render the scene.
                shadowCamera.Render();

                RenderTexture rt = RenderTexture.GetTemporary(shadowCameraRenderTextureDescriptor);
                Graphics.Blit(shadowCameraRenderTexture, rt, shadowCameraGuassianBlurMaterial, 0);
                Graphics.Blit(rt, shadowCameraCubemaps, shadowCameraGuassianBlurMaterial, 1, (shadowCameraCubemapIndex * 6) + face);
                RenderTexture.ReleaseTemporary(rt);
            }

            // activate the cubemap on this light source.
            shaderLight->channel |= (uint)1 << 15; // shadow available bit
            shaderLight->shadowCubemapIndex = (uint)shadowCameraCubemapIndex++;
        }
    }
}