using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The lighting mode for dynamic geometry that was not raytraced.</summary>
    public enum DynamicGeometryLightingMode
    {
        /// <summary>
        /// Dynamic geometry that has not been raytraced, will receive lighting from all nearby
        /// light sources. These light sources will leak through walls.
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will receive lighting from all nearby light sources. These light sources will leak through walls.")]
        LightingOnly = 0,

        /// <summary>
        /// Dynamic geometry that has not been raytraced, will read low-resolution shadows casted by
        /// static geometry that never update using a technique similar to shadow mapping (default).
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will read low-resolution shadows casted by static geometry that never update using a technique similar to shadow mapping (default).")]
        DistanceCubes = 1,
    }
}