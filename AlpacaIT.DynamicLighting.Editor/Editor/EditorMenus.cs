using AlpacaIT.DynamicLighting.Internal;
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

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 1024", false, 20)]
        private static void EditorRaytrace1024()
        {
            DynamicLightManager.Instance.Raytrace(1024);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 2048 (Recommended)", false, 20)]
        private static void EditorRaytrace2048()
        {
            DynamicLightManager.Instance.Raytrace(2048);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 4096", false, 20)]
        private static void EditorRaytrace4096()
        {
            DynamicLightManager.Instance.Raytrace(4096);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Skip Bounce Lighting: Unlimited", false, 40)]
        private static void EditorPreviewUnlimited()
        {
            DynamicLightManager.Instance.Raytrace(23170, DynamicLightingTracerFlags.SkipAll); // 46340 squared is 2,147,395,600 but had OutOfMemoryException.
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Skip Bounce Lighting: 512", false, 60)]
        private static void EditorPreview512()
        {
            DynamicLightManager.Instance.Raytrace(512, DynamicLightingTracerFlags.SkipAll);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Skip Bounce Lighting: 1024", false, 60)]
        private static void EditorPreview1024()
        {
            DynamicLightManager.Instance.Raytrace(1024, DynamicLightingTracerFlags.SkipAll);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Skip Bounce Lighting: 2048 (Recommended)", false, 60)]
        private static void EditorPreview2048()
        {
            DynamicLightManager.Instance.Raytrace(2048, DynamicLightingTracerFlags.SkipAll);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Skip Bounce Lighting: 4096", false, 60)]
        private static void EditorPreview4096()
        {
            DynamicLightManager.Instance.Raytrace(4096, DynamicLightingTracerFlags.SkipAll);
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Preview Scene/Tip: Activate the scene view overlay", false, 80)]
        private static void EditorMenuOverlayTip()
        {
            if (UnityEditor.EditorUtility.DisplayDialog("Dynamic Lighting", "Press the space bar in Scene View to open the Dynamic Lighting Overlay. This overlay makes raytracing lighting easier and provides quick access to various light types.", "Activate", "Cancel"))
                if (DynamicLightingToolbar.instance != null)
                    DynamicLightingToolbar.instance.displayed = true;
        }

#if !UNITY_2021_2_OR_NEWER

        [UnityEditor.MenuItem("Dynamic Lighting/Thank You/Donation: PayPal", false, 60)]
        private static void EditorMenuPayPal()
        {
            Application.OpenURL("https://paypal.me/henrydejongh");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Thank You/Donation: Ko-fi", false, 60)]
        private static void EditorMenuKofi()
        {
            Application.OpenURL("https://ko-fi.com/henry00");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Thank You/Donation: Patreon", false, 60)]
        private static void EditorMenuPatreon()
        {
            Application.OpenURL("https://patreon.com/henrydejongh");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Thank You/Join Discord Server", false, 80)]
        private static void EditorMenuDiscord()
        {
            Application.OpenURL("https://discord.gg/sKEvrBwHtq");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Thank You/GitHub Repository (Star Me!)", false, 100)]
        private static void EditorMenuRepository()
        {
            Application.OpenURL("https://github.com/Henry00IS/DynamicLighting");
        }

#else

        [UnityEditor.MenuItem("Dynamic Lighting/About...", false, 80)]
        private static void EditorMenuAbout()
        {
            AboutWindow.Init();
        }

#endif

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Point Light", false, 40)]
        internal static DynamicLight EditorCreateDynamicPointLight(UnityEditor.MenuCommand menuCommand)
        {
            return EditorCreateDynamicLight("Dynamic Light", menuCommand == null);
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Spot Light", false, 40)]
        internal static DynamicLight EditorCreateDynamicSpotLight(UnityEditor.MenuCommand menuCommand)
        {
            var light = EditorCreateDynamicLight("Dynamic Spot Light", menuCommand == null);
            light.lightType = DynamicLightType.Spot;
            return light;
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Directional Light", false, 40)]
        internal static DynamicLight EditorCreateDynamicDirectionalLight(UnityEditor.MenuCommand menuCommand)
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

            return light;
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Discoball Light", false, 40)]
        internal static DynamicLight EditorCreateDynamicDiscoballLight(UnityEditor.MenuCommand menuCommand)
        {
            var light = EditorCreateDynamicLight("Dynamic Discoball Light", menuCommand == null);
            light.lightType = DynamicLightType.Discoball;
            light.lightCutoff = 12.5f;
            light.lightOuterCutoff = 14.0f;
            return light;
        }

        /// <summary>Adds a new dynamic light game object to the scene.</summary>
        /// <param name="name">The name of the game object that will be created.</param>
        /// <param name="siblings">Whether to add the game object as a sibling not a child.</param>
        /// <returns>The dynamic light component.</returns>
        internal static DynamicLight EditorCreateDynamicLight(string name, bool siblings = true)
        {
            GameObject go = new GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            var parent = UnityEditor.Selection.activeTransform;
            if (parent)
            {
                // place the new game object next to the current selection in the editor.
                if (siblings)
                {
                    var grandgo = parent.parent;
                    if (grandgo)
                        go.transform.SetParent(grandgo, false);
                    go.transform.SetSiblingIndex(parent.GetSiblingIndex() + 1);
                }
                // place the new game object as a child of the current selection in the editor.
                else
                {
                    // keep the game object transform identity.
                    go.transform.SetParent(parent, false);
                }
            }
            else
            {
                // move it in front of the current camera.
                var camera = Utilities.GetSceneViewCamera();
                if (camera)
                {
                    // cast a ray into the scene at the center of the scene view.
                    var ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0.0f));

                    Vector3 hitPoint;
                    if (Physics.Raycast(ray, out RaycastHit hit, 10f))
                        hitPoint = hit.point + hit.normal * 0.25f; // place 0.25m off the surface.
                    else
                        hitPoint = camera.transform.TransformPoint(Vector3.forward * 2f);

#if REALTIME_CSG
                    // while having realtimecsg enabled we use those snapping tools.
                    if (RealtimeCSG.CSGSettings.EnableRealtimeCSG)
                    {
                        if (RealtimeCSG.CSGSettings.GridSnapping)
                        {
                            hitPoint = Snapping.Snap(hitPoint, RealtimeCSG.CSGSettings.SnapVector);
                        }
                    }
                    else
#endif
                    // snap to grid when enabled in the editor.
                    if (UnityEditor.EditorSnapSettings.gridSnapEnabled)
                    {
                        hitPoint = Snapping.Snap(hitPoint, Vector3.one * UnityEditor.EditorSnapSettings.scale);
                    }
                    go.transform.position = hitPoint;
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