using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The dynamic light manager processes all of the lighting in the scene.</summary>
    [ExecuteInEditMode]
    public partial class DynamicLightManager : MonoBehaviour
    {
        private static DynamicLightManager s_Instance;

        /// <summary>Gets the singleton dynamic lighting manager instance or creates it.</summary>
        public static DynamicLightManager Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // C# hot reloading support: try finding an existing instance in the scene.
                s_Instance = FindObjectOfType<DynamicLightManager>();

                // otherwise create a new instance in scene.
                if (!s_Instance)
                    s_Instance = new GameObject("[Dynamic Light Manager]").AddComponent<DynamicLightManager>();

                return s_Instance;
            }
        }

        /// <summary>Whether an instance of the dynamic lighting manager has been created.</summary>
        public static bool hasInstance => s_Instance;

        /// <summary>Used to detect hot C# reloading during play.</summary>
        [System.NonSerialized]
        private bool isInitialized = false;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        /// <summary>Immediately reloads the lighting.</summary>
        public void Reload()
        {
            Cleanup();
            Initialize(true);
        }

        private void Initialize(bool reload = false)
        {
            // always immediately force an update in case we are budgeting.
            lastCameraMetricGridPosition = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            // only execute the rest if not initialized yet.
            if (isInitialized) return;
            isInitialized = true;

            Initialize_Lights(reload);
            Initialize_Shapes(reload);
        }

        private void Cleanup()
        {
            // only execute if currently initialized.
            if (!isInitialized) return;
            isInitialized = false;

            Cleanup_Lights();
            Cleanup_Shapes();
        }

        /// <summary>This handles the CPU side lighting effects.</summary>
        private void Update()
        {
            var camera = Camera.main;

#if UNITY_EDITOR
            // editor scene view support.
            if (!Application.isPlaying)
            {
                camera = EditorUtilities.GetSceneViewCamera();
            }
            else
            {
                Debug.Assert(camera != null, "Could not find a camera that is tagged \"MainCamera\" for lighting calculations.");
            }

            Update_Editor();
#endif

            Update_Lights(camera);
            Update_Shapes(camera);

            // update the ambient lighting color.
            Shader.SetGlobalColor("dynamic_ambient_color", ambientColor);

            // update the shadow filtering algorithm.
            switch (QualitySettings.shadows)
            {
                case ShadowQuality.Disable:
                case ShadowQuality.HardOnly:
                    Shader.DisableKeyword("DYNAMIC_LIGHTING_SHADOW_SOFT");
                    Shader.EnableKeyword("DYNAMIC_LIGHTING_SHADOW_HARD");
                    break;

                case ShadowQuality.All:
                    Shader.EnableKeyword("DYNAMIC_LIGHTING_SHADOW_SOFT");
                    Shader.DisableKeyword("DYNAMIC_LIGHTING_SHADOW_HARD");
                    break;
            }
        }
    }
}