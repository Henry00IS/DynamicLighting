using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Provides access to resources for the Dynamic Lighting package using the properties of a <see
    /// cref="ScriptableObject"/> stored in the "Resources" folder. This allows Unity to manage and
    /// serialize the asset references. Use the static property <see cref="Instance"/> to access
    /// (and load once) the singleton into memory.
    /// </summary>
    [CreateAssetMenu(fileName = "DynamicLightingResources", menuName = "ScriptableObjects/DynamicLightingResources", order = 1)]
    internal class DynamicLightingResources : ScriptableObject
    {
        private static DynamicLightingResources s_Instance;

        /// <summary>Gets the singleton dynamic lighting resources instance or creates it.</summary>
        public static DynamicLightingResources Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // load the shape editor resources from the resources directory.
                LoadResources();

                return s_Instance;
            }
        }

        /// <summary>
        /// Before the first scene loads, we access the instance property to load all resources.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void BeforeSceneLoad()
        {
            // load the shape editor resources from the resources directory.
            LoadResources();
        }

        /// <summary>Loads the shape editor resources from the resources directory.</summary>
        private static void LoadResources()
        {
            s_Instance = (DynamicLightingResources)Resources.Load("DynamicLightingResources");
        }

        public Texture2D dynamicPointLightIcon;
        public Material dynamicLightingPostProcessingMaterial;
        public Shader shadowCameraDepthShader;
        public Shader photonCubeShader;
    }
}