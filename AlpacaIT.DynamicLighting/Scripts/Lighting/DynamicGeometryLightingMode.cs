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
        /// Dynamic geometry that has not been raytraced, will receive low-resolution shadows casted
        /// by static geometry that never update using a technique similar to shadow mapping.
        /// <para>
        /// This technique treats the world as voxels for interpolation, this works well but you can
        /// see the boxes (sometimes floating boxes of shadow in mid-air).
        /// </para>
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will receive low-resolution shadows casted by static geometry that never update using a technique similar to shadow mapping.\r\n\r\nThis technique treats the world as voxels for interpolation, this works well but you can see the boxes (sometimes floating boxes of shadow in mid-air).")]
        [InspectorName("Cubic Interpolation (Deprecated)")]
        DistanceCubes = 1,

        /// <summary>
        /// Dynamic geometry that has not been raytraced, will receive shadows using high-quality
        /// angular filtering (default).
        /// <para>
        /// This technique treats light as a series of expanding wedges radiating from the source.
        /// It calculates smooth gradients between shadow pixels, resulting in soft, natural
        /// penumbras that realistically blur as the distance from the shadow-caster increases.
        /// </para>
        /// </summary>
        [Tooltip("Dynamic geometry that has not been raytraced, will receive shadows using high-quality angular filtering (default).\r\n\r\nThis technique treats light as a series of expanding wedges radiating from the source. It calculates smooth gradients between shadow pixels, resulting in soft, natural penumbras that realistically blur as the distance from the shadow-caster increases.")]
        [InspectorName("Angular Interpolation")]
        Angular = 2,
    }
}