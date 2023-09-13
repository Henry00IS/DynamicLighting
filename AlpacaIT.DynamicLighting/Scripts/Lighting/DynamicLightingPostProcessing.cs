using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    [ImageEffectAllowedInSceneView]
    [RequireComponent(typeof(Camera))]
    public class DynamicLightingPostProcessing : MonoBehaviour
    {
        private Material _material;
        private Camera _camera;

        private void Start()
        {
            _camera = GetComponent<Camera>();
            _camera.depthTextureMode = DepthTextureMode.Depth;

            _material = DynamicLightingResources.Instance.dynamicLightingPostProcessingMaterial;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            var viewMatrix = _camera.worldToCameraMatrix;
            var projectionMatrix = _camera.projectionMatrix;
            projectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, false);
            var clipToPos = (projectionMatrix * viewMatrix).inverse;
            _material.SetMatrix("clipToWorld", clipToPos);

            Graphics.Blit(source, destination, _material);
        }
    }
}