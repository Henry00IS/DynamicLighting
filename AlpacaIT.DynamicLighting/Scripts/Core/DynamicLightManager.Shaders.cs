using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements optimized shader properties, and repairs material thumbnails in the unity editor
    // upon saving the project by making them unlit.

    public partial class DynamicLightManager
    {
#if !UNITY_2021_2_OR_NEWER
        /// <summary>
        /// Fallback for Unity 2021.1.0 and below that do not have the GlobalKeyword struct yet.
        /// Simply uses the name as a string using an implicit operator.
        /// </summary>
        private struct GlobalKeyword
        {
            public string name;

            public GlobalKeyword(string name)
            {
                this.name = name;
            }

            public static GlobalKeyword Create(string name)
            {
                return new GlobalKeyword(name);
            }

            public static implicit operator string(GlobalKeyword gk) => gk.name;
        }
#endif

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

        /// <summary>
        /// Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_BVH". Because the Bounding
        /// Volume Hierarchy requires a valid StructuredBuffer on the GPU, we use this flag to have
        /// a fallback implementation by default. The GPU uses while loops and it would be dangerous
        /// to read an unbound buffer.
        /// </summary>
        private GlobalKeyword shadersGlobalKeywordBvh;

        /// <summary>
        /// Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_BOUNCE". Bounce lighting
        /// takes more VRAM in the dynamic triangles data structure and does more operations in the
        /// shader. This flag is used to only store and execute the additional work when bounce
        /// lighting is actually used in a scene.
        /// </summary>
        private GlobalKeyword shadersGlobalKeywordBounce;

        /// <summary>Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS".</summary>
        private GlobalKeyword shadersGlobalKeywordIntegratedGraphics;

        /// <summary>Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_QUALITY_LOW".</summary>
        private GlobalKeyword shadersGlobalKeywordQualityLow;

        /// <summary>Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_QUALITY_HIGH".</summary>
        private GlobalKeyword shadersGlobalKeywordQualityHigh;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for buffer "dynamic_lights".</summary>
        private int shadersGlobalPropertyIdDynamicLights;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for integer "dynamic_lights_count".</summary>
        private int shadersGlobalPropertyIdDynamicLightsCount;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for integer "realtime_lights_count".</summary>
        private int shadersGlobalPropertyIdRealtimeLightsCount;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for color "dynamic_ambient_color".</summary>
        private int shadersGlobalPropertyIdDynamicAmbientColor;

        /// <summary>Global <see cref="Shader.PropertyToID"/> for buffer "dynamic_lights_bvh".</summary>
        private int shadersGlobalPropertyIdDynamicLightsBvh;

        /// <summary>Stores the value last assigned with <see cref="ShadersSetGlobalDynamicLightsCount"/>.</summary>
        private int shadersLastDynamicLightsCount;

        /// <summary>Stores the value last assigned with <see cref="ShadersSetGlobalRealtimeLightsCount"/>.</summary>
        private int shadersLastRealtimeLightsCount;

        /// <summary>Stores the raw value last assigned with <see cref="ShadersSetGlobalDynamicAmbientColor"/>.</summary>
        private Color shadersLastRawAmbientColor;

        /// <summary>Stores the keyword state assigned with <see cref="ShadersSetKeywordLitEnabled"/>.</summary>
        private bool shadersLastKeywordLitEnabled;

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_LIT" is enabled.</summary>
        private bool shadersKeywordLitEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordLit);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordLit, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_BVH" is enabled.</summary>
        private bool shadersKeywordBvhEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordBvh);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordBvh, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_BOUNCE" is enabled.</summary>
        private bool shadersKeywordBounceEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordBounce);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordBounce, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS" is enabled.</summary>
        private bool shadersKeywordIntegratedGraphicsEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordIntegratedGraphics);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordIntegratedGraphics, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_QUALITY_LOW" is enabled.</summary>
        private bool shadersKeywordQualityLowEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordQualityLow);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordQualityLow, value);
        }

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_QUALITY_HIGH" is enabled.</summary>
        private bool shadersKeywordQualityHighEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordQualityHigh);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordQualityHigh, value);
        }

        /// <summary>Gets or sets the global shader integer property "dynamic_lights_count".</summary>
        private int shadersPropertyDynamicLightsCount
        {
#if UNITY_2021_1_OR_NEWER
            get => Shader.GetGlobalInteger(shadersGlobalPropertyIdDynamicLightsCount);
            set => Shader.SetGlobalInteger(shadersGlobalPropertyIdDynamicLightsCount, value);
#else
            get => Shader.GetGlobalInt(shadersGlobalPropertyIdDynamicLightsCount);
            set => Shader.SetGlobalInt(shadersGlobalPropertyIdDynamicLightsCount, value);
#endif
        }

        /// <summary>Gets or sets the global shader integer property "realtime_lights_count".</summary>
        private int shadersPropertyRealtimeLightsCount
        {
#if UNITY_2021_1_OR_NEWER
            get => Shader.GetGlobalInteger(shadersGlobalPropertyIdRealtimeLightsCount);
            set => Shader.SetGlobalInteger(shadersGlobalPropertyIdRealtimeLightsCount, value);
#else
            get => Shader.GetGlobalInt(shadersGlobalPropertyIdRealtimeLightsCount);
            set => Shader.SetGlobalInt(shadersGlobalPropertyIdRealtimeLightsCount, value);
#endif
        }

        /// <summary>Gets or sets the global shader color property "dynamic_ambient_color".</summary>
        private Color shadersPropertyAmbientColor
        {
            get => Shader.GetGlobalColor(shadersGlobalPropertyIdDynamicAmbientColor);
            set => Shader.SetGlobalColor(shadersGlobalPropertyIdDynamicAmbientColor, value);
        }

        /// <summary>Sets the global shader buffer property "dynamic_lights".</summary>
        private void ShadersSetGlobalDynamicLights(ComputeBuffer buffer)
        {
            Shader.SetGlobalBuffer(shadersGlobalPropertyIdDynamicLights, buffer);
        }

        /// <summary>Sets whether global shader keyword "DYNAMIC_LIGHTING_LIT" is enabled.</summary>
        private void ShadersSetKeywordLitEnabled(bool value)
        {
            // overwrite the cache and shader keyword when a change is detected.
            if (shadersLastKeywordLitEnabled != value)
            {
                shadersLastKeywordLitEnabled = value;
                shadersKeywordLitEnabled = value;
            }
        }

        /// <summary>Sets the global shader integer property "dynamic_lights_count".</summary>
        private void ShadersSetGlobalDynamicLightsCount(int value)
        {
            // overwrite the cache and shader property when a change is detected.
            if (shadersLastDynamicLightsCount != value)
            {
                shadersLastDynamicLightsCount = value;
                shadersPropertyDynamicLightsCount = value;
            }
        }

        /// <summary>Sets the global shader integer property "realtime_lights_count".</summary>
        private void ShadersSetGlobalRealtimeLightsCount(int value)
        {
            // overwrite the cache and shader property when a change is detected.
            if (shadersLastRealtimeLightsCount != value)
            {
                shadersLastRealtimeLightsCount = value;
                shadersPropertyRealtimeLightsCount = value;
            }
        }

        /// <summary>Sets the global shader color property "dynamic_ambient_color".</summary>
        private unsafe void ShadersSetGlobalDynamicAmbientColor(Color value)
        {
            // only update the shader property when a change is detected.
            if (shadersLastRawAmbientColor.FastNotEquals(value))
            {
                shadersLastRawAmbientColor = value;

                // unity stores colors in gamma and we must convert them to linear.
                if (colorSpace == ColorSpace.Linear)
                    UMath.GammaToLinearSpace((Vector3*)&value);

                // push the alpha down to allow for more flexibility in the lower end.
                var a = Mathf.Pow(value.a, 2.0f);
                value.r *= a;
                value.g *= a;
                value.b *= a;

                shadersPropertyAmbientColor = value;
            }
        }

        /// <summary>Sets the global shader buffer property "dynamic_lights_bvh".</summary>
        private void ShadersSetGlobalDynamicLightsBvh(ComputeBuffer buffer)
        {
            Shader.SetGlobalBuffer(shadersGlobalPropertyIdDynamicLightsBvh, buffer);
        }

        /// <summary>
        /// Initialization of shader related variables in the DynamicLightManager.Shaders partial class.
        /// </summary>
        private void ShadersInitialize()
        {
            shadersGlobalKeywordLit = GlobalKeyword.Create("DYNAMIC_LIGHTING_LIT");
            shadersGlobalKeywordBvh = GlobalKeyword.Create("DYNAMIC_LIGHTING_BVH");
            shadersGlobalKeywordBounce = GlobalKeyword.Create("DYNAMIC_LIGHTING_BOUNCE");
            shadersGlobalKeywordIntegratedGraphics = GlobalKeyword.Create("DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS");
            shadersGlobalKeywordQualityLow = GlobalKeyword.Create("DYNAMIC_LIGHTING_QUALITY_LOW");
            shadersGlobalKeywordQualityHigh = GlobalKeyword.Create("DYNAMIC_LIGHTING_QUALITY_HIGH");

            shadersGlobalPropertyIdDynamicLights = Shader.PropertyToID("dynamic_lights");
            shadersGlobalPropertyIdDynamicLightsCount = Shader.PropertyToID("dynamic_lights_count");
            shadersGlobalPropertyIdRealtimeLightsCount = Shader.PropertyToID("realtime_lights_count");
            shadersGlobalPropertyIdDynamicAmbientColor = Shader.PropertyToID("dynamic_ambient_color");
            shadersGlobalPropertyIdDynamicLightsBvh = Shader.PropertyToID("dynamic_lights_bvh");

            // upon startup (or level transitions):

            // we always enable lighting.
            ShadersSetKeywordLitEnabled(!renderUnlit);

            // disable the bounding volume hierarchy logic as it may be dangerous.
            shadersKeywordBvhEnabled = false;

            // enable the bounce lighting code when used in the scene during raytracing.
            shadersKeywordBounceEnabled = activateBounceLightingInCurrentScene;

            // switch to the default medium quality mode.
            ShadersSetRuntimeQuality(runtimeQuality);

            // set the ambient color to black to synchronize the raw ambient color.
            shadersPropertyAmbientColor = shadersLastRawAmbientColor = Color.black;

            // fetch the current shader properties.
            shadersLastDynamicLightsCount = shadersPropertyDynamicLightsCount;
            shadersLastRealtimeLightsCount = shadersPropertyRealtimeLightsCount;
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

        /// <summary>
        /// Enables or disables <see cref="shadersKeywordIntegratedGraphicsEnabled"/> and <see
        /// cref="shadersKeywordQualityLowEnabled"/> and <see
        /// cref="shadersKeywordQualityHighEnabled"/> depending on the desired quality setting.
        /// <para>This sets field <see cref="activeRuntimeQuality"/> accordingly.</para>
        /// </summary>
        /// <param name="quality">The runtime quality to be applied.</param>
        private void ShadersSetRuntimeQuality(DynamicLightingRuntimeQuality quality)
        {
            if (activeRuntimeQuality != quality)
            {
                activeRuntimeQuality = quality;

                switch (quality)
                {
                    case DynamicLightingRuntimeQuality.IntegratedGraphics:
                        shadersKeywordIntegratedGraphicsEnabled = true;
                        shadersKeywordQualityLowEnabled = false;
                        shadersKeywordQualityHighEnabled = false;
                        break;

                    case DynamicLightingRuntimeQuality.Low:
                        shadersKeywordIntegratedGraphicsEnabled = false;
                        shadersKeywordQualityLowEnabled = true;
                        shadersKeywordQualityHighEnabled = false;
                        break;

                    case DynamicLightingRuntimeQuality.Medium:
                        shadersKeywordIntegratedGraphicsEnabled = false;
                        shadersKeywordQualityLowEnabled = false;
                        shadersKeywordQualityHighEnabled = false;
                        break;

                    case DynamicLightingRuntimeQuality.High:
                        shadersKeywordIntegratedGraphicsEnabled = false;
                        shadersKeywordQualityLowEnabled = false;
                        shadersKeywordQualityHighEnabled = true;
                        break;
                }
            }
        }
    }
}