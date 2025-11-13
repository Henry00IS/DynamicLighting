using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>
    /// Provides access to editor resources for the Dynamic Lighting package using the properties of
    /// a <see cref="ScriptableObject"/> stored in the "Resources" folder. This allows Unity to
    /// manage and serialize the asset references. Use the static property <see cref="Instance"/> to
    /// access (and load once) the singleton into memory.
    /// </summary>
    // [CreateAssetMenu(fileName = "DynamicLightingEditorResources", menuName = "ScriptableObjects/DynamicLightingEditorResources", order = 1)]
    internal class DynamicLightingEditorResources : ScriptableObject
    {
        private static DynamicLightingEditorResources s_Instance;

        /// <summary>Gets the singleton dynamic lighting editor resources instance or creates it.</summary>
        public static DynamicLightingEditorResources Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // load the dynamic lighting resources from the resources directory.
                LoadResources();

                return s_Instance;
            }
        }

        /// <summary>Loads the dynamic lighting resources from the resources directory.</summary>
        private static void LoadResources()
        {
            s_Instance = (DynamicLightingEditorResources)Resources.Load("DynamicLightingEditorResources");
        }

        public Texture2D paypalIcon;
        public Texture2D kofiIcon;
        public Texture2D patreonIcon;
        public Texture2D patreonIconWhite;
        public Texture2D discordIcon;
        public Texture2D gitHubIcon;
        public Texture2D gitHubIconWhite;

        public Shader dynamicLightingDiffuseShader;
    }
}