using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The compression modes for bounce lighting data on the graphics card.</summary>
    public enum DynamicBounceLightingCompressionMode
    {
        /// <summary>
        /// Inherits the bounce lighting data compression mode from the dynamic light manager in the scene.
        /// </summary>
        [Tooltip("Inherits the bounce lighting data compression mode from the dynamic light manager in the scene.")]
        Inherit = 0,

        /// <summary>
        /// Stores bounce lighting data with 4 pixels in 32-bit units on the graphics card, each
        /// pixel using 8-bit (0-255) depth (default).
        /// </summary>
        [Tooltip("Stores bounce lighting data with 4 pixels in 32-bit units on the graphics card, each pixel using 8-bit (0-255) depth (default).")]
        [InspectorName("8 Bits Per Pixel (Lossless)")]
        EightBitsPerPixel = 8,

        /// <summary>
        /// Stores bounce lighting data with 5 pixels in 32-bit units on the graphics card, each
        /// pixel using 6-bit (0-63) depth. This reduces the amount of VRAM used by 20% (e.g., 4GiB
        /// becomes 3.2GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering).
        /// </summary>
        [Tooltip("Stores bounce lighting data with 5 pixels in 32-bit units on the graphics card, each pixel using 6-bit (0-63) depth. This reduces the amount of VRAM used by 20% (e.g., 4GiB becomes 3.2GiB). However, it may cause noticeable shading differences (color banding), which are softened by adding a slight noise pattern (dithering).")]
        [InspectorName("6 Bits Per Pixel (20%)")]
        SixBitsPerPixel = 6,

        /// <summary>
        /// Stores bounce lighting data with 6 pixels in 32-bit units on the graphics card, each
        /// pixel using 5-bit (0-31) depth. This reduces the amount of VRAM used by 34% (e.g., 4GiB
        /// becomes 2.7GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering). This mode works well
        /// for games with low resolution textures, as the pixelation hides the dithering.
        /// </summary>
        [Tooltip("Stores bounce lighting data with 6 pixels in 32-bit units on the graphics card, each pixel using 5-bit (0-31) depth. This reduces the amount of VRAM used by 34% (e.g., 4GiB becomes 2.7GiB). However, it may cause noticeable shading differences (color banding), which are softened by adding a slight noise pattern (dithering). This mode works well for games with low resolution textures, as the pixelation hides the dithering.")]
        [InspectorName("5 Bits Per Pixel (34%)")]
        FiveBitsPerPixel = 5,

        /// <summary>
        /// Stores bounce lighting data with 8 pixels in 32-bit units on the graphics card, each
        /// pixel using 4-bit (0-15) depth. This reduces the amount of VRAM used by 50% (e.g., 4GiB
        /// becomes 2GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering). This mode works well
        /// for games with low resolution textures, as the pixelation hides the dithering. Avoid
        /// using this mode near white or single-colored textures as the noise becomes very obvious.
        /// </summary>
        [Tooltip("Stores bounce lighting data with 8 pixels in 32-bit units on the graphics card, each pixel using 4-bit (0-15) depth. This reduces the amount of VRAM used by 50% (e.g., 4GiB becomes 2GiB). However, it may cause noticeable shading differences (color banding), which are softened by adding a slight noise pattern (dithering). This mode works well for games with low resolution textures, as the pixelation hides the dithering. Avoid using this mode near white or single-colored textures as the noise becomes very obvious.")]
        [InspectorName("4 Bits Per Pixel (50%)")]
        FourBitsPerPixel = 4,
    }
}