using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements photon cube generation using a camera that builds cubemaps.

    internal partial class DynamicLightingTracer
    {
        /// <summary>All of the photon camera orientations to take cubemap frames.</summary>
        private static readonly Quaternion[] photonCameraOrientations = new Quaternion[]
        {
            Quaternion.LookRotation(Vector3.right, Vector3.down),
            Quaternion.LookRotation(Vector3.left, Vector3.down),
            Quaternion.LookRotation(Vector3.up, Vector3.forward),
            Quaternion.LookRotation(Vector3.down, Vector3.back),
            Quaternion.LookRotation(Vector3.forward, Vector3.down),
            Quaternion.LookRotation(Vector3.back, Vector3.down)
        };

        /// <summary>The <see cref="GameObject"/> of the photon camera.</summary>
        private GameObject photonCameraGameObject;

        /// <summary>The <see cref="Transform"/> of the photon camera <see cref="GameObject"/>.</summary>
        private Transform photonCameraTransform;

        /// <summary>The <see cref="Camera"/> used for capturing photon frames in the scene.</summary>
        private Camera photonCamera;

        /// <summary>The array of photon camera cubemap textures.</summary>
        private RenderTexture photonCameraCubemaps;

        /// <summary>The replacement shader used by the photon camera to render depth.</summary>
        private Shader photonCameraDepthShader;

        /// <summary>Used to fetch 32-bit ARGB floating point textures for the photon camera rendering.</summary>
        private RenderTextureDescriptor photonCameraRenderTextureDescriptor;

        /// <summary>Temporary render texture used by the photon camera to render the scene.</summary>
        private RenderTexture photonCameraRenderTexture;

        private const int photonCameraResolution = 1024;
        private int photonCameraCubemapIndex;

        /// <summary>Initialization of the DynamicLightingTracer.PhotonCamera partial class.</summary>
        private void PhotonCameraInitialize()
        {
            photonCameraDepthShader = DynamicLightingResources.Instance.photonCubeShader;

#if UNITY_2021_3_OR_NEWER && !UNITY_2021_3_0 && !UNITY_2021_3_1 && !UNITY_2021_3_2 && !UNITY_2021_3_3 && !UNITY_2021_3_4 && !UNITY_2021_3_5 && !UNITY_2021_3_6 && !UNITY_2021_3_7 && !UNITY_2021_3_8 && !UNITY_2021_3_9 && !UNITY_2021_3_10 && !UNITY_2021_3_11 && !UNITY_2021_3_12 && !UNITY_2021_3_13 && !UNITY_2021_3_14 && !UNITY_2021_3_15 && !UNITY_2021_3_16 && !UNITY_2021_3_17 && !UNITY_2021_3_18 && !UNITY_2021_3_19 && !UNITY_2021_3_20 && !UNITY_2021_3_21 && !UNITY_2021_3_22 && !UNITY_2021_3_23 && !UNITY_2021_3_24 && !UNITY_2021_3_25 && !UNITY_2021_3_26 && !UNITY_2021_3_27
            photonCameraRenderTextureDescriptor = new RenderTextureDescriptor(photonCameraResolution, photonCameraResolution, RenderTextureFormat.ARGBFloat, 16, 0, RenderTextureReadWrite.Linear);
#else
            photonCameraRenderTextureDescriptor = new RenderTextureDescriptor(photonCameraResolution, photonCameraResolution, RenderTextureFormat.ARGBFloat, 16, 0);
#endif
            photonCameraRenderTextureDescriptor.autoGenerateMips = false;

            // create the photon camera game object.
            photonCameraGameObject = new GameObject("[Dynamic Lighting - Photon Camera]");
            photonCameraGameObject.SetActive(false);

            // this is very important, otherwise it keeps adding more instances when switching
            // between editor and play mode as it saves the scene.
            photonCameraGameObject.hideFlags = HideFlags.HideAndDontSave;

            // parent it under our instance to keep things clean.
            photonCameraTransform = photonCameraGameObject.transform;
            photonCameraTransform.parent = DynamicLightManager.Instance.transform;

            // create the photon camera.
            photonCamera = photonCameraGameObject.AddComponent<Camera>();
            photonCamera.enabled = false;
            // the 90 degrees field of view is exactly one side of a cubemap.
            photonCamera.fieldOfView = 90;
            // this acts as the inner radius around the camera, which when objects clip inside, the
            // photons will visually glitch, so we keep it very small.
            photonCamera.nearClipPlane = 0.01f;
            // we must render the skybox to prevent glitched distance data.
            photonCamera.clearFlags = CameraClearFlags.Skybox;
            // only useful in the editor previews, but programmers can filter by this category.
            photonCamera.cameraType = CameraType.Reflection;
            // we render depth using a special shader.
            photonCamera.SetReplacementShader(photonCameraDepthShader, "RenderType");
#if UNITY_EDITOR
            // we only render the temporary raytracing scene.
            photonCamera.scene = temporaryScene.scene;
#endif
            // create photon cubemap array.
            photonCameraCubemaps = new RenderTexture(photonCameraRenderTextureDescriptor);
            photonCameraCubemaps.dimension = TextureDimension.Cube;
            photonCameraCubemaps.useMipMap = false;
            photonCameraCubemaps.autoGenerateMips = false;
            photonCameraCubemaps.volumeDepth = 6;
            photonCameraCubemaps.filterMode = FilterMode.Point;
            photonCameraCubemaps.Create();

            Shader.SetGlobalTexture("photon_cubemaps", photonCameraCubemaps);
        }

        /// <summary>Cleanup of the DynamicLightingTracer.PhotonCamera partial class.</summary>
        private void PhotonCameraCleanup()
        {
            // destroy the photon camera game object.
            Object.DestroyImmediate(photonCameraGameObject);

            // release the unity resources we no longer need.
            photonCameraCubemaps.Release();
            photonCameraCubemaps = null;
        }

        /// <summary>Renders the photon camera for a light source.</summary>
        /// <param name="lightPosition">The world position of the light.</param>
        /// <param name="lightRadius">The radius of the light.</param>
        /// <param name="distanceOnly">Whether to store the distance only to reduce RAM usage.</param>
        private PhotonCube PhotonCameraRender(Vector3 lightPosition, float lightRadius, bool distanceOnly)
        {
            // get a temporary render texture.
            photonCameraRenderTexture = RenderTexture.GetTemporary(photonCameraRenderTextureDescriptor);
            photonCameraRenderTexture.filterMode = FilterMode.Point;

            // have the photon camera render to the render texture.
            photonCamera.targetTexture = photonCameraRenderTexture;

            // ----------------------------------------------------------------

            // we move the camera to the light source.
            photonCameraTransform.position = lightPosition;

            // we do not need photons beyond the light radius.
            photonCamera.farClipPlane = lightRadius * 2f;
            photonCamera.cullingMask = ~0;//bounceLightingLayers;

            // render the 6 sides of the cubemap:
            for (int face = 0; face < 6; face++)
            {
                photonCameraTransform.rotation = photonCameraOrientations[face];

                // use the direct illumination replacement shader to render the scene.
                photonCamera.Render();

                Graphics.CopyTexture(photonCameraRenderTexture, 0, 0, photonCameraCubemaps, face, 0);
            }

            // ----------------------------------------------------------------

            // remove the render texture reference from the camera.
            photonCamera.targetTexture = null;

            // release the temporary render textures.
            RenderTexture.ReleaseTemporary(photonCameraRenderTexture);

            // create the photon cube.
            return new PhotonCube(photonCameraCubemaps, distanceOnly);
        }
    }
}