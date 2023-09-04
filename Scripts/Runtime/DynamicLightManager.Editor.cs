using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public partial class DynamicLightManager : MonoBehaviour
    {
#if UNITY_EDITOR

        [UnityEditor.MenuItem("Dynamic Lighting/PayPal Donation", false, 41)]
        private static void EditorPayPalDonation()
        {
            Application.OpenURL("https://paypal.me/henrydejongh");
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Point Light", false, 40)]
        private static void EditorCreateDynamicPointLight()
        {
            EditorCreateDynamicLight("Dynamic Light");
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Spot Light", false, 40)]
        private static void EditorCreateDynamicSpotLight()
        {
            var light = EditorCreateDynamicLight("Dynamic Spot Light");
            light.lightType = DynamicLightType.Spot;
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Directional Light", false, 40)]
        private static void EditorCreateDynamicDirectionalLight()
        {
            // create the outer object.
            var name = "Dynamic Directional Light";
            GameObject parent = new GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(parent, "Create " + name);

            // create the sun point light far away from the scene.
            GameObject sun = new GameObject("Dynamic Sun Light");
            sun.transform.parent = parent.transform;
            sun.transform.localPosition = new Vector3(0f, 0f, -2500f); // 2.5km

            // rotate the outer object to look down (identical to a new scene directional light).
            parent.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);

            // add the dynamic light component.
            var light = sun.AddComponent<DynamicLight>();
            light.lightIntensity = 2.0f;
            light.lightRadius = 10000.0f; // 10km

            // make sure it's selected and unity editor will let the user rename the game object.
            UnityEditor.Selection.activeGameObject = parent;
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Discoball Light", false, 40)]
        private static void EditorCreateDynamicDiscoballLight()
        {
            var light = EditorCreateDynamicLight("Dynamic Discoball Light");
            light.lightType = DynamicLightType.Discoball;
            light.lightCutoff = 12.5f;
            light.lightOuterCutoff = 14.0f;
        }

        /// <summary>Adds a new dynamic light game object to the scene.</summary>
        /// <param name="name">The name of the game object that will be created.</param>
        /// <returns>The dynamic light component.</returns>
        private static DynamicLight EditorCreateDynamicLight(string name)
        {
            GameObject go = new GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            // place the new game object as a child of the current selection in the editor.
            var parent = UnityEditor.Selection.activeTransform;
            if (parent)
            {
                // keep the game object transform identity.
                go.transform.SetParent(parent, false);
            }
            else
            {
                // move it in front of the current camera.
                var camera = EditorUtilities.GetSceneViewCamera();
                if (camera)
                {
                    go.transform.position = camera.transform.TransformPoint(Vector3.forward * 2f);
                }
            }

            // add the dynamic light component.
            var light = go.AddComponent<DynamicLight>();

            // make sure it's selected and unity editor will let the user rename the game object.
            UnityEditor.Selection.activeGameObject = go;
            return light;
        }

        /// <summary>
        /// Called by <see cref="DynamicLightingTracer"/> to properly free up the compute buffers
        /// before clearing the lightmaps collection. Then deletes all lightmap files from disk.
        /// This method call must be followed up by a call to <see cref="Reload"/>
        /// </summary>
        internal void EditorDeleteLightmaps()
        {
            // free up the compute buffers.
            Cleanup();

            // clear the lightmap scene data.
            lightmaps.Clear();

            // delete the lightmap files from disk.
            EditorUtilities.DeleteLightmapData();

            // make sure the user gets prompted to save their scene.
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Delete Scene Lightmaps", false, 20)]
        private static void EditorDeleteLightmapsNow()
        {
            Instance.EditorDeleteLightmaps();
            Instance.Reload();
        }

        private void Update_Editor()
        {
            // respect the scene view lighting toggle.
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView)
                {
                    var sceneLighting = sceneView.sceneLighting;
                    var shaderUnlit = Shader.IsKeywordEnabled("DYNAMIC_LIGHTING_UNLIT");

                    if (sceneLighting && shaderUnlit)
                        Shader.DisableKeyword("DYNAMIC_LIGHTING_UNLIT");
                    else if (!sceneLighting && !shaderUnlit)
                        Shader.EnableKeyword("DYNAMIC_LIGHTING_UNLIT");
                }
            }
        }

        private void OnDrawGizmos()
        {
            // the scene view continuous preview toggle.
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView && sceneView.sceneViewState.fxEnabled && sceneView.sceneViewState.alwaysRefresh)
            {
                // ensure continuous update calls.
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
        }

#endif
    }
}