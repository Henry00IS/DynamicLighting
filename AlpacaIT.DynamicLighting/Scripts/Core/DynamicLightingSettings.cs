using UnityEngine;
using UnityEngine.Serialization;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// An object containing settings shared across scenes for <see cref="DynamicLightManager"/> components.
    /// </summary>
    [CreateAssetMenu(fileName = "DynamicLightingSettings.asset", menuName = "Dynamic Lighting/Settings")]
    public sealed class DynamicLightingSettings : ScriptableObject
    {
        /// <inheritdoc cref="DynamicLightManager.ambientColor"/>
        [SerializeField]
        [Tooltip("The ambient lighting color is added to the whole scene, thus making it look like there is always some scattered light, even when there is no direct light source. This prevents absolute black, dark patches from appearing in the scene that are impossible to see through (unless this is desired). This color should be very dark to achieve the best effect. You can make use of the alpha color to finetune the brightness.")]
        [FormerlySerializedAs("_ambientColor")]
        private Color ambientColor;

        /// <inheritdoc cref="DynamicLightManager.realtimeLightBudget"/>
        [SerializeField]
        [Tooltip("The number of realtime dynamic lights that can be active at the same time. Realtime lights have no shadows and can move around the scene. They are useful for glowing particles, car headlights, etc. If this budget is exceeded, lights that are out of view or furthest away from the camera will automatically fade out in a way that the player will hopefully not notice. A conservative budget as per the game requirements will help older graphics hardware when there are many realtime lights in the scene. Budgeting does not begin until the number of active realtime dynamic lights actually exceeds this number.")]
        [Min(0)]
        [FormerlySerializedAs("_realtimeLightBudget")]
        private int realtimeLightBudget;

        /// <inheritdoc cref="DynamicLightManager.raytraceLayers"/>
        [SerializeField]
        [Tooltip("The layer mask used while raytracing to determine which hits to ignore. There are many scenarios where you have objects that should collide with everything in the scene, but not cause shadows. You should consider creating a physics layer (click on 'Layer: Default' at the top of the Game Object Inspector -> Add Layer...) and naming it 'Collision'. You can then remove this layer from the list so it will be ignored by the raytracer. The rest of the scene will still have regular collisions with it. You can also do the opposite by creating a physics layer called 'Lighting' and disabling regular collisions with other colliders (in Edit -> Project Settings -> Physics), but leaving the layer checked in this list. Now you have a special shadow casting collision that nothing else can touch or interact with. These were just two example names, you can freely choose the names of the physics layers.")]
        [FormerlySerializedAs("_raytraceLayers")]
        private LayerMask raytraceLayers;

        /// <inheritdoc cref="DynamicLightManager.realtimeShadowLayers"/>
        [SerializeField]
        [Tooltip("The layer mask used for real-time shadows to discern which objects are shadow casters. In some cases, specific objects may require raytracing for baked lighting and visual appeal, yet they shouldn't contribute to real-time shadow casting (e.g. thin objects).")]
        [FormerlySerializedAs("_realtimeShadowLayers")]
        private LayerMask realtimeShadowLayers;

        /// <inheritdoc cref="DynamicLightManager.pixelDensityPerSquareMeter"/>
        [SerializeField]
        [Tooltip("The desired pixel density (e.g. 128 for 128x128 per meter squared). This lighting system does not require \"power of two\" textures. You may have heard this term before because graphics cards can render textures in such sizes much faster. This system relies on binary data on the GPU using compute buffers and it's quite different. Without going into too much detail, this simply means that we can choose any texture size. An intelligent algorithm calculates the surface area of the meshes and determines exactly how many pixels are needed to cover them evenly with shadow pixels, regardless of the ray tracing resolution (unless it exceeds that maximum ray tracing resolution, of course, then those shadow pixels will start to increase in size). Here you can set how many pixels should cover a square meter. It can result in a 47x47 texture or 328x328, exactly the amount needed to cover all polygons with the same amount of shadow pixels. Higher details require more VRAM (exponentially)!")]
        [Min(1)]
        [FormerlySerializedAs("_pixelDensityPerSquareMeter")]
        private int pixelDensityPerSquareMeter;

        /// <inheritdoc cref="DynamicLightManager.bounceLightingCompression"/>
        [SerializeField]
        [Tooltip("The compression level for bounce lighting data. Choosing a higher compression can reduce VRAM usage, but may result in reduced visual quality. For best results, adjust based on your VRAM availability and visual preferences.")]
        private DynamicBounceLightingDefaultCompressionMode bounceLightingCompression;

        /// <inheritdoc cref="DynamicLightManager.lightTrackingMode"/>
        [Tooltip("Dynamic light sources can be moved in the scene, where they will be treated as real-time lights without shadows. While this approach is easy to work with, it requires a background process to continuously track the positions of all light sources. This uses some computational power which may not be available in your project. Moving raytraced lights (with the intention to use them as real-time lights) also incurs a performance cost on the GPU compared to actual real-time light sources and is therefore not recommended. Alternatively, it is possible to only update all positions when required, such as a raytraced light (or the game object with a raytraced light) getting enabled in the scene. This relaxes the system and reduces the computational overhead. Note that volumetric fog that uses the game object scale will also not be updated. An exception is the light rotation which will always be updated no matter which mode is used.")]
        private DynamicLightTrackingMode lightTrackingMode;

        /// <summary>
        /// Creates a new instance of the <see cref="DynamicLightingSettings"/> with default values.
        /// </summary>
        public DynamicLightingSettings()
        {
            Reset();
        }

        /// <summary>Resets the settings object to the default state.</summary>
        private void Reset()
        {
            ambientColor = new Color(1.0f, 1.0f, 1.0f, 0.1254902f);
            realtimeLightBudget = 32;
            raytraceLayers = ~0;
            realtimeShadowLayers = ~(4 | 16 | 32);
            pixelDensityPerSquareMeter = 128;
            bounceLightingCompression = DynamicBounceLightingDefaultCompressionMode.EightBitsPerPixel;
            lightTrackingMode = DynamicLightTrackingMode.LiveTracking;

            TryApply();
        }

        /// <summary>
        /// Applies the settings stored in this instance to the current <see
        /// cref="DynamicLightManager"/> in the scene (and creates the instance if it does not exist).
        /// </summary>
        public void Apply()
        {
            var dynamicLightManager = DynamicLightManager.Instance;
            dynamicLightManager.ambientColor = ambientColor;
            dynamicLightManager.realtimeLightBudget = realtimeLightBudget;
            dynamicLightManager.raytraceLayers = raytraceLayers;
            dynamicLightManager.realtimeShadowLayers = realtimeShadowLayers;
            dynamicLightManager.pixelDensityPerSquareMeter = pixelDensityPerSquareMeter;
            dynamicLightManager.bounceLightingCompression = bounceLightingCompression;
            dynamicLightManager.lightTrackingMode = lightTrackingMode;
        }

        /// <summary>
        /// When there is an instance of the <see cref="DynamicLightManager"/> in the scene (it will
        /// not be created automatically) the current settings are read from it and written into
        /// this instance.
        /// </summary>
        /// <returns>True when an instance of <see cref="DynamicLightManager"/> exists and was read else false.</returns>
        public bool ImportFromScene()
        {
            if (DynamicLightManager.hasInstance)
            {
                var dynamicLightManager = DynamicLightManager.Instance;
                ambientColor = dynamicLightManager.ambientColor;
                realtimeLightBudget = dynamicLightManager.realtimeLightBudget;
                raytraceLayers = dynamicLightManager.raytraceLayers;
                realtimeShadowLayers = dynamicLightManager.realtimeShadowLayers;
                pixelDensityPerSquareMeter = dynamicLightManager.pixelDensityPerSquareMeter;
                bounceLightingCompression = dynamicLightManager.bounceLightingCompression;
                lightTrackingMode = dynamicLightManager.lightTrackingMode;
                return true;
            }
            return false;
        }

#if UNITY_EDITOR

        /// <summary>Called when the script is loaded or when a value is changed in the inspector.</summary>
        private void OnValidate()
        {
            // when the current settings template is modified then update the scene accordingly.
            if (!Application.isPlaying)
                TryApply();
        }

#endif

        /// <summary>
        /// When the current settings template is modified then the scene is updated accordingly.
        /// </summary>
        private void TryApply()
        {
            // when the current settings template is modified then update the scene accordingly.
            if (DynamicLightManager.hasInstance)
            {
                var dynamicLightManager = DynamicLightManager.Instance;
                if (dynamicLightManager.settingsTemplate == this)
                    Apply();
            }
        }
    }
}