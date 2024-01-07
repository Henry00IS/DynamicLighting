using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements optimized shader properties, and repairs material thumbnails in the unity editor
    // upon saving the project by making them unlit.

    public partial class DynamicLightManager
    {
        /// <summary>
        /// Gets or sets whether the shaders should be rendered unlit (no lighting and shadows; only
        /// the diffuse texture will be shown).
        /// </summary>
        public bool renderUnlit { get; set; }

        /// <summary>
        /// Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_LIT". Upon material
        /// thumbnail generation in the unity editor, during the saving process of the project, this
        /// global shader keyword will internally be ignored, thus causing unlit previews instead of
        /// black previews.
        /// </summary>
        private GlobalKeyword shadersGlobalKeywordLit;

        /// <summary>Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_SHADOW_SOFT".</summary>
        private GlobalKeyword shadersGlobalKeywordShadowSoft;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for buffer "dynamic_lights".</summary>
        private int shadersGlobalPropertyIdDynamicLights;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for integer "dynamic_lights_count".</summary>
        private int shadersGlobalPropertyIdDynamicLightsCount;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for integer "realtime_lights_count".</summary>
        private int shadersGlobalPropertyIdRealtimeLightsCount;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for color "dynamic_ambient_color".</summary>
        private int shadersGlobalPropertyIdDynamicAmbientColor;

        /// <summary>Stores the value last assigned with <see cref="ShadersSetGlobalDynamicLightsCount"/>.</summary>
        private int shadersLastDynamicLightsCount;

        /// <summary>Stores the value last assigned with <see cref="ShadersSetGlobalRealtimeLightsCount"/>.</summary>
        private int shadersLastRealtimeLightsCount;

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_LIT" is enabled.</summary>
        private bool shadersKeywordLitEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordLit);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordLit, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_SHADOW_SOFT" is enabled.</summary>
        private bool shadersKeywordShadowSoftEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordShadowSoft);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordShadowSoft, value);
        }

        /// <summary>Sets the global shader buffer property "dynamic_lights".</summary>
        private void ShadersSetGlobalDynamicLights(ComputeBuffer buffer)
        {
            Shader.SetGlobalBuffer(shadersGlobalPropertyIdDynamicLights, buffer);
        }

        /// <summary>Sets the global shader integer property "dynamic_lights_count".</summary>
        private void ShadersSetGlobalDynamicLightsCount(int value)
        {
#if UNITY_2021_1_OR_NEWER
            Shader.SetGlobalInteger(shadersGlobalPropertyIdDynamicLightsCount, value);
#else
            Shader.SetGlobalInt(shadersGlobalPropertyIdDynamicLightsCount, value);
#endif
        }

        /// <summary>Sets the global shader integer property "realtime_lights_count".</summary>
        private void ShadersSetGlobalRealtimeLightsCount(int value)
        {
#if UNITY_2021_1_OR_NEWER
            Shader.SetGlobalInteger(shadersGlobalPropertyIdRealtimeLightsCount, value);
#else
            Shader.SetGlobalInt(shadersGlobalPropertyIdRealtimeLightsCount, value);
#endif
        }

        /// <summary>Sets the global shader color property "dynamic_ambient_color".</summary>
        private void ShadersSetGlobalDynamicAmbientColor(Color value)
        {
            Shader.SetGlobalColor(shadersGlobalPropertyIdDynamicAmbientColor, value);
        }

        /// <summary>
        /// Initialization of shader related variables in the DynamicLightManager.Shaders partial class.
        /// </summary>
        private void ShadersInitialize()
        {
            shadersGlobalKeywordLit = GlobalKeyword.Create("DYNAMIC_LIGHTING_LIT");
            shadersGlobalKeywordShadowSoft = GlobalKeyword.Create("DYNAMIC_LIGHTING_SHADOW_SOFT");

            shadersGlobalPropertyIdDynamicLights = Shader.PropertyToID("dynamic_lights");
            shadersGlobalPropertyIdDynamicLightsCount = Shader.PropertyToID("dynamic_lights_count");
            shadersGlobalPropertyIdRealtimeLightsCount = Shader.PropertyToID("realtime_lights_count");
            shadersGlobalPropertyIdDynamicAmbientColor = Shader.PropertyToID("dynamic_ambient_color");

            // upon startup (or level transitions) we always enable lighting.
            shadersKeywordLitEnabled = !renderUnlit;
        }

        /// <summary>Enables or disables a <see cref="GlobalKeyword"/>.</summary>
        /// <param name="globalKeyword">The <see cref="GlobalKeyword"/> to be enabled or disabled.</param>
        /// <param name="enable">
        /// Whether the <see cref="GlobalKeyword"/> should be enabled (true) or disabled (false).
        /// </param>
        private void ShadersSetGlobalKeyword(ref GlobalKeyword globalKeyword, bool enable)
        {
            if (enable)
                Shader.EnableKeyword(globalKeyword);
            else
                Shader.DisableKeyword(globalKeyword);
        }
    }
}