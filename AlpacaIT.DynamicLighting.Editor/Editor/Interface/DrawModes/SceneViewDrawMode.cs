using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    public abstract class SceneViewDrawMode
    {
        /// <summary>The default state for a draw mode unrelated to Dynamic Lighting.</summary>
        public static SceneViewDrawModeNone SceneViewNoneDrawMode = new SceneViewDrawModeNone();

        /// <summary>The collection of custom draw modes.</summary>
        public static SceneViewDrawMode[] sceneViewDrawModes = {
            new SceneViewDrawModeLighting(),
        };

        /// <summary>The category name of all custom draw modes "Dynamic Lighting".</summary>
        public const string Category = "Dynamic Lighting";

        /// <summary>Gets the name of the custom draw mode.</summary>
        public abstract string Name { get; }

        /// <summary>Called when this draw mode is enabled in a scene view.</summary>
        /// <param name="sceneView">The active scene view.</param>
        public abstract void OnEnable(SceneView sceneView);

        /// <summary>Called when this draw mode is disabled in a scene view.</summary>
        /// <param name="sceneView">The active scene view.</param>
        public abstract void OnDisable(SceneView sceneView);

        /// <summary>Called before the scene view camera renders.</summary>
        /// <param name="sceneView">The active scene view.</param>
        /// <param name="camera">The active camera instance.</param>
        public abstract void OnCameraPreRender(SceneView sceneView, Camera camera);

        /// <summary>Called after the scene view camera renders.</summary>
        /// <param name="sceneView">The active scene view.</param>
        /// <param name="camera">The active camera instance.</param>
        public abstract void OnCameraPostRender(SceneView sceneView, Camera camera);

        /// <summary>Gets whether this draw mode is selected in the given <paramref name="sceneView"/>.</summary>
        /// <param name="sceneView">The scene view to be checked (null returns false).</param>
        /// <returns>True when this draw mode is selected else false.</returns>
        public bool IsSelected(SceneView sceneView)
        {
            if (sceneView == null) return false;
            var cameraMode = sceneView.cameraMode;
            return cameraMode.section == Category && cameraMode.name == Name;
        }

        /// <summary>Gets the active <see cref="SceneViewDrawMode"/> on the given scene view.</summary>
        /// <param name="sceneView">The scene view to be checked.</param>
        /// <returns>
        /// The scene view draw mode or <see cref="SceneViewNoneDrawMode"/> if none of our modes are active.
        /// </returns>
        public static SceneViewDrawMode GetSceneViewDrawMode(SceneView sceneView)
        {
            for (var i = 0; i < sceneViewDrawModes.Length; i++)
            {
                var sceneViewDrawMode = sceneViewDrawModes[i];
                if (sceneViewDrawMode.IsSelected(sceneView))
                    return sceneViewDrawMode;
            }
            return SceneViewNoneDrawMode;
        }
    }
}