using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>
    /// Customizes the material inspector for Dynamic Lighting shaders to display an additional
    /// button that will fix the material preview (if needed).
    /// </summary>
    public class DefaultShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            SuggestPresenceOfDynamicLightManager();

            base.OnGUI(materialEditor, properties);
        }

        /// <summary>
        /// Displays a small warning button to fix a missing [Dynamic Light Manager] instance in the
        /// scene, if it does not exist.
        /// </summary>
        public static void SuggestPresenceOfDynamicLightManager()
        {
            // if we do not have an instance, it may be caused by a C# hot reload.
            if (!DynamicLightManager.hasInstance)
            {
                // try finding an existing instance in the scene.
                var instance = Internal.Compatibility.FindObjectOfType<DynamicLightManager>();

                // ignoring the silly hasInstance state; we know the preview should be visually correct.
                if (instance) return;

                // suggest to the user that they should take action to fix the material preview.
                EditorGUILayout.HelpBox("For an accurate preview of the material, please make sure that the [Dynamic Light Manager] instance is present in the scene.", MessageType.Warning);
                if (GUILayout.Button("Fix Now"))
                    DynamicLightManager.Instance.ToString(); // accessing the instance property will restore the state/create the game object.
                EditorGUILayout.Space();
            }
        }
    }
}