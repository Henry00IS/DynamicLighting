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

        /// <summary>Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_SHADOW_SOFT".</summary>
        private GlobalKeyword shadersGlobalKeywordShadowSoft;

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

        /// <summary>
        /// Stores the <see cref="GlobalKeyword"/> of "DYNAMIC_LIGHTING_BOUNCE_6BPP". Bounce
        /// lighting can be compressed by 20% using only 6-bits per pixel. This flag is used to
        /// switch to that decompression mode in the shader.
        /// </summary>
        private GlobalKeyword shadersGlobalKeywordBounce6Bpp;

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

        /// <summary>Gets or sets whether global shader keyword "DYNAMIC_LIGHTING_BOUNCE_6BPP" is enabled.</summary>
        private bool shadersKeywordBounce6BppEnabled
        {
            get => Shader.IsKeywordEnabled(shadersGlobalKeywordBounce6Bpp);
            set => ShadersSetGlobalKeyword(ref shadersGlobalKeywordBounce6Bpp, value);
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
            // push the alpha down to allow for more flexibility in the lower end.
            var a = Mathf.Pow(value.a, 2.0f);
            value.r *= a;
            value.g *= a;
            value.b *= a;
            Shader.SetGlobalColor(shadersGlobalPropertyIdDynamicAmbientColor, value);
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
            shadersGlobalKeywordShadowSoft = GlobalKeyword.Create("DYNAMIC_LIGHTING_SHADOW_SOFT");
            shadersGlobalKeywordBvh = GlobalKeyword.Create("DYNAMIC_LIGHTING_BVH");
            shadersGlobalKeywordBounce = GlobalKeyword.Create("DYNAMIC_LIGHTING_BOUNCE");
            shadersGlobalKeywordBounce6Bpp = GlobalKeyword.Create("DYNAMIC_LIGHTING_BOUNCE_6BPP");

            shadersGlobalPropertyIdDynamicLights = Shader.PropertyToID("dynamic_lights");
            shadersGlobalPropertyIdDynamicLightsCount = Shader.PropertyToID("dynamic_lights_count");
            shadersGlobalPropertyIdRealtimeLightsCount = Shader.PropertyToID("realtime_lights_count");
            shadersGlobalPropertyIdDynamicAmbientColor = Shader.PropertyToID("dynamic_ambient_color");
            shadersGlobalPropertyIdDynamicLightsBvh = Shader.PropertyToID("dynamic_lights_bvh");

            // upon startup (or level transitions):

            // we always enable lighting.
            shadersKeywordLitEnabled = !renderUnlit;

            // disable the bounding volume hierarchy logic as it may be dangerous.
            shadersKeywordBvhEnabled = false;

            // enable the bounce lighting code when used in the scene during raytracing.
            shadersKeywordBounceEnabled = activateBounceLightingInCurrentScene;

            // enable the bounce lighting compression mode used in the scene during raytracing.
            shadersKeywordBounce6BppEnabled = bounceLightingCompressionInCurrentScene == DynamicBounceLightingCompressionMode.SixBitsPerPixel;
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