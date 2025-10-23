using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Shorthand to call <see cref="MaterialEditor.ShaderProperty(MaterialProperty, string,
        /// int)"/> with the <see cref="MaterialProperty.displayName"/> as the label parameter. When
        /// the property is of type texture then <see
        /// cref="MaterialEditor.TexturePropertySingleLine(GUIContent, MaterialProperty)"/> is
        /// called instead with the <see cref="MaterialProperty.displayName"/> as the <see
        /// cref="GUIContent"/> parameter.
        /// </summary>
        /// <param name="materialEditor">Handle to the active material editor.</param>
        /// <param name="property">The texture property to be displayed.</param>
        /// <param name="indent">The indentation to move the label to the right.</param>
        public static void Property(this MaterialEditor materialEditor, MaterialProperty property, int indent = 0)
        {
            if (property.type == MaterialProperty.PropType.Texture)
            {
                EditorGUI.indentLevel += indent;
                materialEditor.TexturePropertySingleLine(new GUIContent(property.displayName), property);
                EditorGUI.indentLevel -= indent;
            }
            else
                materialEditor.ShaderProperty(property, property.displayName, indent);
        }

        /// <summary>
        /// When <paramref name="texture"/> is not null and <paramref name="other"/> is not null,
        /// they are rendered using <see cref="MaterialEditor.TexturePropertySingleLine(GUIContent,
        /// MaterialProperty, MaterialProperty)"/> and removed from <paramref name="properties"/>.
        /// </summary>
        /// <param name="materialEditor">Handle to the active material editor.</param>
        /// <param name="properties">The list of properties to be modified.</param>
        /// <param name="texture">The texture selector to be displayed.</param>
        /// <param name="other">The other property to be displayed.</param>
        public static void Combine(this MaterialEditor materialEditor, List<MaterialProperty> properties, MaterialProperty texture, MaterialProperty other)
        {
            if (texture != null && other != null)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent(texture.displayName), texture, other);
                properties.Remove(texture);
                properties.Remove(other);
            }
        }

        /// <summary>
        /// When <paramref name="property"/> is not null, the <paramref name="then"/> action is
        /// invoked and the property is removed from <paramref name="properties"/>.
        /// </summary>
        /// <param name="materialEditor">Handle to the active material editor.</param>
        /// <param name="properties">The list of properties to be modified.</param>
        /// <param name="property">The property that is checked for null.</param>
        /// <param name="then">The action to be executed when the property is not null.</param>
        public static void Combine(this MaterialEditor materialEditor, List<MaterialProperty> properties, MaterialProperty property, Action then)
        {
            if (property != null)
            {
                then?.Invoke();
                properties.Remove(property);
            }
        }

        /// <summary>
        /// When <paramref name="property1"/> is not null and <paramref name="property2"/> is not
        /// null, the <paramref name="then"/> action is invoked and the properties are removed from
        /// <paramref name="properties"/>.
        /// </summary>
        /// <param name="materialEditor">Handle to the active material editor.</param>
        /// <param name="properties">The list of properties to be modified.</param>
        /// <param name="property1">The first property that is checked for null.</param>
        /// <param name="property2">The second property that is checked for null.</param>
        /// <param name="then">The action to be executed when both properties are not null.</param>
        public static void Combine(this MaterialEditor materialEditor, List<MaterialProperty> properties, MaterialProperty property1, MaterialProperty property2, Action then)
        {
            if (property1 != null && property2 != null)
            {
                then?.Invoke();
                properties.Remove(property1);
                properties.Remove(property2);
            }
        }

        /// <summary>Attempts to find a <see cref="MaterialProperty"/> by <paramref name="name"/>.</summary>
        /// <param name="properties">The array of properties to be searched.</param>
        /// <param name="name">The property name to find.</param>
        /// <returns>The <see cref="MaterialProperty"/> when found else null.</returns>
        public static MaterialProperty Find(this MaterialProperty[] properties, string name)
        {
            for (int i = 0; i < properties.Length; i++)
                if (properties[i] != null && properties[i].name == name)
                    return properties[i];
            return null;
        }
    }
}