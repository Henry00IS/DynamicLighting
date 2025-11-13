using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Scene view draw mode that only outputs the lighting.</summary>
    public class SceneViewDrawModeLighting : SceneViewDrawMode
    {
        public override string Name => "Lighting Only";

        private GlobalKeyword shaderKeywordDynamicLightingSceneViewModeLighting;

        public override void OnEnable(SceneView sceneView)
        {
            shaderKeywordDynamicLightingSceneViewModeLighting = GlobalKeyword.Create("DYNAMIC_LIGHTING_SCENE_VIEW_MODE_LIGHTING");
            sceneView.SetSceneViewShaderReplace(DynamicLightingEditorResources.Instance.dynamicLightingDiffuseShader, null);
        }

        public override void OnDisable(SceneView sceneView)
        {
            sceneView.SetSceneViewShaderReplace(null, null);
        }

        public override void OnCameraPreRender(SceneView sceneView, Camera camera)
        {
            Shader.EnableKeyword(shaderKeywordDynamicLightingSceneViewModeLighting);
        }

        public override void OnCameraPostRender(SceneView sceneView, Camera camera)
        {
            Shader.DisableKeyword(shaderKeywordDynamicLightingSceneViewModeLighting);
        }
    }
}