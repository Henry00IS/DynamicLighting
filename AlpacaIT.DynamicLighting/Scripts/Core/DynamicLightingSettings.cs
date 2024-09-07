using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>An object containing settings for <see cref="DynamicLightManager"/> components.</summary>
    [CreateAssetMenu(fileName = "DynamicLightingSettings.asset", menuName = "Dynamic Lighting/Settings")]
    public sealed class DynamicLightingSettings : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The ambient lighting color is added to the whole scene, thus making it look like there is always some scattered light, even when there is no direct light source. This prevents absolute black, dark patches from appearing in the scene that are impossible to see through (unless this is desired). This color should be very dark to achieve the best effect. You can make use of the alpha color to finetune the brightness.")]
        private Color _ambientColor;

        [SerializeField]
        [Tooltip("The number of realtime dynamic lights that can be active at the same time. Realtime lights have no shadows and can move around the scene. They are useful for glowing particles, car headlights, etc. If this budget is exceeded, lights that are out of view or furthest away from the camera will automatically fade out in a way that the player will hopefully not notice. A conservative budget as per the game requirements will help older graphics hardware when there are many realtime lights in the scene. Budgeting does not begin until the number of active realtime dynamic lights actually exceeds this number.")]
        [Min(0)]
        private int _realtimeLightBudget;

        [SerializeField]
        [Tooltip("The layer mask used while raytracing to determine which hits to ignore. There are many scenarios where you have objects that should collide with everything in the scene, but not cause shadows. You should consider creating a physics layer (click on 'Layer: Default' at the top of the Game Object Inspector -> Add Layer...) and naming it 'Collision'. You can then remove this layer from the list so it will be ignored by the raytracer. The rest of the scene will still have regular collisions with it. You can also do the opposite by creating a physics layer called 'Lighting' and disabling regular collisions with other colliders (in Edit -> Project Settings -> Physics), but leaving the layer checked in this list. Now you have a special shadow casting collision that nothing else can touch or interact with. These were just two example names, you can freely choose the names of the physics layers.")]
        private LayerMask _raytraceLayers;

        [SerializeField]
        [Tooltip("The layer mask used for real-time shadows to discern which objects are shadow casters. In some cases, specific objects may require raytracing for baked lighting and visual appeal, yet they shouldn't contribute to real-time shadow casting (e.g. thin objects).")]
        private LayerMask _realtimeShadowLayers;

        [SerializeField]
        [Tooltip("The desired pixel density (e.g. 128 for 128x128 per meter squared). This lighting system does not require \"power of two\" textures. You may have heard this term before because graphics cards can render textures in such sizes much faster. This system relies on binary data on the GPU using compute buffers and it's quite different. Without going into too much detail, this simply means that we can choose any texture size. An intelligent algorithm calculates the surface area of the meshes and determines exactly how many pixels are needed to cover them evenly with shadow pixels, regardless of the ray tracing resolution (unless it exceeds that maximum ray tracing resolution, of course, then those shadow pixels will start to increase in size). Here you can set how many pixels should cover a square meter. It can result in a 47x47 texture or 328x328, exactly the amount needed to cover all polygons with the same amount of shadow pixels. Higher details require more VRAM (exponentially)!")]
        [Min(1)]
        private int _pixelDensityPerSquareMeter;

        /// <summary>Gets the default <see cref="DynamicLightingSettings"/> asset.</summary>
        public static DynamicLightingSettings defaultSettings => Resources.Load<DynamicLightingSettings>("DynamicLightingSettings/Default");

        /// <inheritdoc cref="DynamicLightManager.ambientColor"/>
        public Color ambientColor
        {
            get => _ambientColor;
            set { ThrowInvalidOperationExceptionIfDefault(); _ambientColor = value; }
        }

        /// <inheritdoc cref="DynamicLightManager.realtimeLightBudget"/>
        public int realtimeLightBudget
        {
            get => _realtimeLightBudget;
            set { ThrowInvalidOperationExceptionIfDefault(); _realtimeLightBudget = value; }
        }

        /// <inheritdoc cref="DynamicLightManager.raytraceLayers"/>
        public LayerMask raytraceLayers
        {
            get => _raytraceLayers;
            set { ThrowInvalidOperationExceptionIfDefault(); _raytraceLayers = value; }
        }

        /// <inheritdoc cref="DynamicLightManager.realtimeShadowLayers"/>
        public LayerMask realtimeShadowLayers
        {
            get => _realtimeShadowLayers;
            set { ThrowInvalidOperationExceptionIfDefault(); _realtimeShadowLayers = value; }
        }

        /// <inheritdoc cref="DynamicLightManager.pixelDensityPerSquareMeter"/>
        public int pixelDensityPerSquareMeter
        {
            get => _pixelDensityPerSquareMeter;
            set { ThrowInvalidOperationExceptionIfDefault(); _pixelDensityPerSquareMeter = value; }
        }

        /// <summary>Resets the settings object to the default state.</summary>
        private void Reset()
        {
            _ambientColor = new Color(1.0f, 1.0f, 1.0f, 0.1254902f);
            _realtimeLightBudget = 32;
            _raytraceLayers = ~0;
            _realtimeShadowLayers = ~(4 | 16 | 32);
            _pixelDensityPerSquareMeter = 128;
        }

        /// <summary>Throws an <see cref="InvalidOperationException"/> if the current instance is <see cref="defaultSettings"/>.</summary>
        [DoesNotReturn]
        private void ThrowInvalidOperationExceptionIfDefault()
        {
            if (this == defaultSettings)
            {
                throw new InvalidOperationException();
            }
        }
    }
}
