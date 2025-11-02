using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>
    /// Customizes the material inspector for Dynamic Lighting shaders to display an additional
    /// button that will fix the material preview (if needed) and helps set material keywords for
    /// metallic fallback calculations when there is no metallic texture assigned.
    /// </summary>
    public class MetallicShaderGUI : ShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            DefaultShaderGUI.SuggestPresenceOfDynamicLightManager();

            var props = new List<MaterialProperty>(properties);
            var propColor = properties.Find("_Color");
            var propMainTex = properties.Find("_MainTex");
            var propMetallicGlossMap = properties.Find("_MetallicGlossMap");
            var propGlossMapScale = properties.Find("_GlossMapScale");
            var propMetallic = properties.Find("_Metallic");
            var propBumpMap = properties.Find("_BumpMap");
            var propBumpScale = properties.Find("_BumpScale");
            var propOcclusionMap = properties.Find("_OcclusionMap");
            var propOcclusionStrength = properties.Find("_OcclusionStrength");
            var propEmissionColor = properties.Find("_EmissionColor");
            var propEmissionMap = properties.Find("_EmissionMap");
            var propCull = properties.Find("_Cull");

            // combine main texture with color.
            materialEditor.Combine(props, propMainTex, propColor);

            // known group: metallic gloss with gloss scale.
            materialEditor.Combine(props, propMetallicGlossMap, propGlossMapScale, () =>
            {
                materialEditor.Property(propMetallicGlossMap);

                materialEditor.Combine(props, propMetallic, () =>
                {
                    if (propMetallicGlossMap.textureValue == null)
                        materialEditor.Property(propMetallic, 2);
                });

                materialEditor.Property(propGlossMapScale, 2);
            });

            // combine normal texture with scale.
            materialEditor.Combine(props, propBumpMap, propBumpScale);

            // combine occlusion texture with strength.
            materialEditor.Combine(props, propOcclusionMap, propOcclusionStrength);

            // combine emission texture with color.
            materialEditor.Combine(props, propEmissionMap, propEmissionColor, () =>
            {
                if (materialEditor.MaterialKeywordCheckbox("_EMISSION", "Emission"))
                    materialEditor.TexturePropertyWithHDRColor(new GUIContent("Color"), propEmissionMap, propEmissionColor, false);
            });

            // combine main texture only rendering scale and offset properties.
            materialEditor.Combine(props, propMainTex, () =>
                materialEditor.TextureScaleOffsetProperty(propMainTex)
            );

            // display the supported cull modes.
            bool changedCull = materialEditor.Dropdown<DefaultShaderGUI.CullMode>(props, propCull, out var selectedCull);

            // update the cull mode parameters in the material when changed.
            if (changedCull)
                foreach (var target in propCull.targets)
                    DefaultShaderGUI.SetupMaterialWithCullMode((Material)target, (DefaultShaderGUI.CullMode)((Material)target).GetFloat("_Cull"));

            // render everything else the default way.
            base.OnGUI(materialEditor, props.ToArray());

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