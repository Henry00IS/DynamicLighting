using AlpacaIT.DynamicLighting.Internal;
using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Adds the "Dynamic Lighting" menu to Unity Editor.</summary>
    public static class EditorMenus
    {
        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: Unlimited", false, 0)]
        private static void EditorRaytraceUnlimited()
        {
            DynamicLightManager.Instance.Raytrace(23170); // 46340 squared is 2,147,395,600 but had OutOfMemoryException.
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 512", false, 20)]
        private static void EditorRaytrace512()
        {
            DynamicLightManager.Instance.Raytrace(512);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 1024", false, 21)]
        private static void EditorRaytrace1024()
        {
            DynamicLightManager.Instance.Raytrace(1024);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 2048 (Recommended)", false, 21)]
        private static void EditorRaytrace2048()
        {
            DynamicLightManager.Instance.Raytrace(2048);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 4096", false, 21)]
        private static void EditorRaytrace4096()
        {
            DynamicLightManager.Instance.Raytrace(4096);
        }

        [UnityEditor.MenuItem( "Dynamic Lighting/About", false, 60 )]
        private static void EditorMenuAbout() {
            AboutWindow.Init();
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
                var camera = Utilities.GetSceneViewCamera();
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
    }
}
