using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The compression modes for bounce lighting data on the graphics card.</summary>
    public enum DynamicBounceLightingCompressionMode
    {
        /// <summary>
        /// Stores bounce lighting data with 4 pixels in 32-bit units on the graphics card, each
        /// pixel using 8-bit (0-255) depth (default).
        /// <para>
        /// Note that any adjustments to this setting require raytracing the scene again for the
        /// changes to take effect.
        /// </para>
        /// </summary>
        [Tooltip("Stores bounce lighting data with 4 pixels in 32-bit units on the graphics card, each pixel using 8-bit (0-255) depth (default).\n\nNote that any adjustments to this setting require raytracing the scene again for the changes to take effect.")]
        EightBitsPerPixel = 0,

        /// <summary>
        /// Stores bounce lighting data with 5 pixels in 32-bit units on the graphics card, each
        /// pixel using 6-bit (0-63) depth. This reduces the amount of VRAM used by 20% (e.g., 4GiB
        /// becomes 3.2GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering).
        /// <para>
        /// Note that any adjustments to this setting require raytracing the scene again for the
        /// changes to take effect.
        /// </para>
        /// </summary>
        [Tooltip("Stores bounce lighting data with 5 pixels in 32-bit units on the graphics card, each pixel using 6-bit (0-63) depth. This reduces the amount of VRAM used by 20% (e.g., 4GiB becomes 3.2GiB). However, it may cause noticeable shading differences (color banding), which are softened by adding a slight noise pattern (dithering).\n\nNote that any adjustments to this setting require raytracing the scene again for the changes to take effect.")]
        SixBitsPerPixel = 1,
    }
}