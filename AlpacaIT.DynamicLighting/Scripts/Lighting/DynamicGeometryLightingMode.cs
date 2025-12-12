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
        /// <para>
        /// This technique treats the world as voxels for interpolation, this works well but you can
        /// see the boxes (sometimes floating boxes of shadow in mid-air).
        /// </para>
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will read low-resolution shadows casted by static geometry that never update using a technique similar to shadow mapping (default).\r\n\r\nThis technique treats the world as voxels for interpolation, this works well but you can see the boxes (sometimes floating boxes of shadow in mid-air).")]
        [InspectorName("Cubic Interpolation")]
        DistanceCubes = 1,

        /// <summary>
        /// Dynamic geometry that has not been raytraced, will read low-resolution shadows casted by
        /// static geometry that never update using a technique similar to shadow mapping.
        /// <para>
        /// This is a new technique that works more like regular shadow mapping, accurate smooth
        /// shadows. Extreme angles can cause visible wedges that look out of place.
        /// </para>
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will read low-resolution shadows casted by static geometry that never update using a technique similar to shadow mapping.\r\n\r\nThis is a new technique that works more like regular shadow mapping, accurate smooth shadows. Extreme angles can cause visible wedges that look out of place.")]
        [InspectorName("Angular Interpolation (New!)")]
        Angular = 2,
    }
}