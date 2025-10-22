using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal static class Extensions
    {
        /// <summary>Checks whether the keyword is disabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keyword">The keyword to find.</param>
        /// <returns>True when the keyword is disabled else false.</returns>
        public static bool IsDisabled(this ShaderKeywordSet shaderKeywordSet, ShaderKeyword keyword)
        {
            return !shaderKeywordSet.IsEnabled(keyword);
        }

        /// <summary>Checks whether all keywords are enabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keywords">The keywords to match.</param>
        /// <returns>True when all keywords are enabled else false.</returns>
        public static bool AllEnabled(this ShaderKeywordSet shaderKeywordSet, params ShaderKeyword[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (!shaderKeywordSet.IsEnabled(keywords[i]))
                    return false;
            return true;
        }

        /// <summary>Checks whether any keyword is enabled.</summary>
        /// <param name="shaderKeywordSet">The keyword set to be checked.</param>
        /// <param name="keywords">The keywords to match.</param>
        /// <returns>True when any of the keywords are enabled else false.</returns>
        public static bool AnyEnabled(this ShaderKeywordSet shaderKeywordSet, params ShaderKeyword[] keywords)
        {
            for (int i = 0; i < keywords.Length; i++)
                if (shaderKeywordSet.IsEnabled(keywords[i]))
                    return true;
            return false;
        }

        /// <summary>Displays a checkbox to toggle a shader keyword (supports multiple-selection).</summary>
        /// <param name="materialEditor">Handle to the active material editor.</param>
        /// <param name="keyword">The keyword as defined in the shader.</param>
        /// <param name="label">The label to be displayed to the user.</param>
        /// <returns>True when enabled, false when not or mixed state.</returns>
        public static bool MaterialKeywordCheckbox(this MaterialEditor materialEditor, string keyword, string label)
        {
            if (materialEditor.targets.Length == 0) return false;

            // check whether all selected materials have the keyword enabled.
            // detect uniform vs. mixed states.
            bool allEnabled = true;
            bool allDisabled = true;
            foreach (var target in materialEditor.targets)
            {
                var material = target as Material;
                bool isEnabled = material.IsKeywordEnabled(keyword);
                if (!isEnabled) allEnabled = false;
                else allDisabled = false;
                if (!allEnabled && !allDisabled) break; // early out: mixed confirmed.
            }
            bool isMixed = !allEnabled && !allDisabled;

            // use first material's state for display value (unity convention for mixed).
            bool displayValue = (materialEditor.targets[0] as Material).IsKeywordEnabled(keyword);

            // save/restore show mixed value to isolate effect.
            bool oldShowMixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = isMixed;

            // display the toggle to enable or disable the keyword.
            EditorGUI.BeginChangeCheck();
            bool newValue = EditorGUILayout.Toggle(label, displayValue);
            if (EditorGUI.EndChangeCheck())
            {
                // apply to _all_ materials (resolves mixed by unifying).
                foreach (var target in materialEditor.targets)
                {
                    var material = target as Material;
                    if (newValue)
                        material.EnableKeyword(keyword);
                    else
                        material.DisableKeyword(keyword);
                }
                materialEditor.PropertiesChanged();
            }

            EditorGUI.showMixedValue = oldShowMixed;
            return newValue; // return the (now uniform) state.
        }
    }
}