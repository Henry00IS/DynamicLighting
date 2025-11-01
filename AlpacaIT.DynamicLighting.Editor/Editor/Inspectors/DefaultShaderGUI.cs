using System.Collections.Generic;
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
        /// <summary>The supported blend modes as seen in the standard shader.</summary>
        public enum BlendMode
        {
            Opaque = 0,
            Cutout = 1,
            Transparent = 3,
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            // try to restore the blend mode.
            BlendMode blendMode = BlendMode.Opaque;
            if (material.HasFloat("_Mode"))
                blendMode = (BlendMode)material.GetFloat("_Mode");

            // if the blend mode is 'fade' from the standard shader:
            if ((int)blendMode == 2)
                blendMode = BlendMode.Transparent;

            // if the blend mode is something completely different:
            if (blendMode != 0 && (int)blendMode != 1 && (int)blendMode != 3)
                blendMode = BlendMode.Opaque;

            // recognize the legacy transparent shader from 2025 and switch to transparent.
            if (oldShader.name.Equals("Dynamic Lighting/Transparent", System.StringComparison.InvariantCultureIgnoreCase))
                blendMode = BlendMode.Transparent;

            // if we support a mode then adjust it accordingly.
            if (material.HasFloat("_Mode"))
            {
                material.SetFloat("_Mode", (float)blendMode);

                // the standard shader uses a bad blend mode and we need to update any third-party configuration.
                SetupMaterialWithBlendMode(material, blendMode);
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            SuggestUpgradeFromTransparentShader(materialEditor);

            SuggestPresenceOfDynamicLightManager();

            var props = new List<MaterialProperty>(properties);
            var propColor = properties.Find("_Color");
            var propMainTex = properties.Find("_MainTex");
            var propCutoff = properties.Find("_Cutoff");
            var propEmissionColor = properties.Find("_EmissionColor");
            var propEmissionMap = properties.Find("_EmissionMap");
            var propMode = properties.Find("_Mode");

            // display the supported blend modes.
            bool changedMode = materialEditor.Dropdown<BlendMode>(props, propMode, out var selectedMode);

            // combine main texture with color.
            materialEditor.Combine(props, propMainTex, propColor);

            // display the cutoff when in cutout mode.
            materialEditor.Combine(props, propCutoff, () =>
            {
                if (selectedMode != BlendMode.Opaque)
                    materialEditor.Property(propCutoff, 2);
            });

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

            // update the blend mode parameters in the material when changed.
            if (changedMode)
                foreach (var target in propMode.targets)
                    SetupMaterialWithBlendMode((Material)target, (BlendMode)((Material)target).GetFloat("_Mode"));

            // render everything else the default way.
            base.OnGUI(materialEditor, props.ToArray());
        }

        /// <summary>Sets up the material to switch to one of the <see cref="BlendMode"/>.</summary>
        /// <param name="material">The material to be modified.</param>
        /// <param name="blendMode">The blend mode to activate.</param>
        private void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = -1;
                    break;

                case BlendMode.Cutout:
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetFloat("_ZWrite", 1.0f);
                    material.EnableKeyword("_ALPHATEST_ON");
                    material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    break;

                case BlendMode.Transparent:
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0.0f);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    break;
            }
        }

        /// <summary>Displays a small warning button to fix a using the legacy transparent shader.</summary>
        private void SuggestUpgradeFromTransparentShader(MaterialEditor materialEditor)
        {
            if (((Material)materialEditor.target).shader.name.Equals("Dynamic Lighting/Transparent", System.StringComparison.InvariantCultureIgnoreCase))
            {
                EditorGUILayout.HelpBox("The 'Transparent' shader has been deprecated and will be removed in 2026, please switch to the 'Diffuse' shader with 'Rendering Mode' set to 'Transparent'.", MessageType.Warning);
                if (GUILayout.Button("Fix Now"))
                {
                    Shader shader = Shader.Find("Dynamic Lighting/Diffuse");
                    if (shader)
                    {
                        foreach (var target in materialEditor.targets)
                        {
                            ((Material)target).shader = shader;
                            ((Material)target).SetFloat("_Mode", (float)BlendMode.Transparent);
                            SetupMaterialWithBlendMode((Material)target, BlendMode.Transparent);
                        }
                    }
                }
                EditorGUILayout.Space();
            }
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