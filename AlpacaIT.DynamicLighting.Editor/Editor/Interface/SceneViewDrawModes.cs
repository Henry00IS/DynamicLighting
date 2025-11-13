using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Implements additional draw modes in the scene view.</summary>
    [InitializeOnLoad]
    public static class SceneViewDrawModes
    {
        /// <summary>The scene view that was last active.</summary>
        private static SceneView lastActiveSceneView;

        /// <summary>Stores the last active draw mode during menu selection in the scene view.</summary>
        private static SceneViewDrawMode lastActiveDrawMode = SceneViewDrawMode.SceneViewNoneDrawMode;

        /// <summary>Called by the <see cref="InitializeOnLoadMethodAttribute"/> on C# (re)compilation.</summary>
        static SceneViewDrawModes()
        {
            // wait for unity editor to inform us about the active scene view.
            SceneView.lastActiveSceneViewChanged += OnSceneViewChanged;

            // register all of our custom draw modes:
            for (int i = 0; i < SceneViewDrawMode.sceneViewDrawModes.Length; i++)
                SceneView.AddCameraMode(SceneViewDrawMode.sceneViewDrawModes[i].Name, SceneViewDrawMode.Category);

            // listen for camera rendering events.
            Camera.onPreRender += OnCameraPreRender;
            Camera.onPostRender += OnCameraPostRender;
        }

        /// <summary>Called whenever Unity Editor switches to a different scene view.</summary>
        /// <param name="previous">The scene view that was previously active (or null).</param>
        /// <param name="current">The scene view that is now active (or null).</param>
        private static void OnSceneViewChanged(SceneView previous, SceneView current)
        {
            // remember the current scene view (may be set to null).
            lastActiveSceneView = current;

            // clean up any old event handlers.
            if (previous)
            {
                previous.onCameraModeChanged -= OnSceneViewDrawModeChanged;
                previous.onValidateCameraMode -= OnSceneViewValidateCameraMode;
            }

            // subscribe new event handlers.
            if (current)
            {
                current.onCameraModeChanged += OnSceneViewDrawModeChanged;
                current.onValidateCameraMode += OnSceneViewValidateCameraMode;
            }
        }

        /// <summary>
        /// Called while the draw mode dropdown menu is open to check whether draw modes can be selected.
        /// <para>
        /// This function is called before the user makes a change thus it's the perfect opportunity
        /// to remember the last active draw mode for cleanup.
        /// </para>
        /// </summary>
        /// <returns>True when the draw mode is enabled else false.</returns>
        private static bool OnSceneViewValidateCameraMode(SceneView.CameraMode _)
        {
            // fetch the draw mode active during menu selection.
            lastActiveDrawMode = SceneViewDrawMode.GetSceneViewDrawMode(lastActiveSceneView);
            return true;
        }

        /// <summary>
        /// Called whenever the draw mode of a <see cref="SceneView"/> changes and on C# (re)compilation.
        /// </summary>
        /// <param name="cameraMode">The active draw mode of the scene view.</param>
        private static void OnSceneViewDrawModeChanged(SceneView.CameraMode cameraMode)
        {
            // fetch the draw mode active after menu selection.
            var activeDrawMode = SceneViewDrawMode.GetSceneViewDrawMode(lastActiveSceneView);

            // clean up the draw mode if needed.
            if (activeDrawMode != lastActiveDrawMode)
                lastActiveDrawMode.OnDisable(lastActiveSceneView);

            // enable the draw mode.
            activeDrawMode.OnEnable(lastActiveSceneView);

            // remember the current draw mode.
            lastActiveDrawMode = activeDrawMode;
        }

        /// <summary>Called before a camera renders.</summary>
        /// <param name="camera">The active camera instance.</param>
        private static void OnCameraPreRender(Camera camera)
        {
            if (lastActiveSceneView == null) return;
            if (camera != lastActiveSceneView.camera) return;
            lastActiveDrawMode.OnCameraPreRender(lastActiveSceneView, camera);
        }

        /// <summary>Called after a camera renders.</summary>
        /// <param name="camera">The active camera instance.</param>
        private static void OnCameraPostRender(Camera camera)
        {
            if (lastActiveSceneView == null) return;
            if (camera != lastActiveSceneView.camera) return;
            lastActiveDrawMode.OnCameraPostRender(lastActiveSceneView, camera);
        }
    }
}