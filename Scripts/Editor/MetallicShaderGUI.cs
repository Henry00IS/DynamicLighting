#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class MetallicShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            foreach (var target in materialEditor.targets)
            {
                var material = target as Material;
                var changed = false;
                var previous = material.IsKeywordEnabled("METALLIC_TEXTURE_UNASSIGNED");

                if (!material.GetTexture("_MetallicGlossMap"))
                {
                    material.EnableKeyword("METALLIC_TEXTURE_UNASSIGNED");
                    changed = previous == false;
                }
                else
                {
                    material.DisableKeyword("METALLIC_TEXTURE_UNASSIGNED");
                    changed = previous == true;
                }

                if (changed)
                    EditorUtility.SetDirty(material);
            }
        }
    }
}

#endif