using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Represents a default state for a draw mode unrelated to Dynamic Lighting.</summary>
    public class SceneViewDrawModeNone : SceneViewDrawMode
    {
        public override string Name => "None";

        public override void OnEnable(SceneView sceneView)
        {
        }

        public override void OnDisable(SceneView sceneView)
        {
        }

        public override void OnCameraPreRender(SceneView sceneView, Camera camera)
        {
        }

        public override void OnCameraPostRender(SceneView sceneView, Camera camera)
        {
        }
    }
}