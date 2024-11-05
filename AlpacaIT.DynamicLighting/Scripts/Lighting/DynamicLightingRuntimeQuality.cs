using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// The runtime quality options for Dynamic Lighting in the scene. These options should be
    /// available in your in-game settings menu to allow players to adjust lighting quality based on
    /// their hardware performance.
    /// </summary>
    public enum DynamicLightingRuntimeQuality
    {
        /// <summary>
        /// Special graphics mode for underpowered laptops with integrated graphics.
        /// <para>Disables support for real-time shadows.</para>
        /// <para>Disables support for bounce lighting.</para>
        /// <para>Uses very cheap bilinear filtering for shadows.</para>
        /// </summary>
        [Tooltip("Special graphics mode for underpowered laptops with integrated graphics.\n\nDisables support for real-time shadows.\n\nDisables support for bounce lighting.\n\nUses very cheap bilinear filtering for shadows.")]
        IntegratedGraphics,

        /// <summary>
        /// Optimized for fast performance, using 3x3 Percentage Closer Filtering (PCF) for shadows.
        /// <para>Provides a balanced visual experience with a focus on frame rate.</para>
        /// <para>
        /// Results in a slightly pixelated appearance, but ensures straight lines for shadows,
        /// unlike cheap bilinear filtering, which may make shadow lines appear crooked.
        /// </para>
        /// </summary>
        [Tooltip("Optimized for fast performance, using 3x3 Percentage Closer Filtering (PCF) for shadows.\n\nProvides a balanced visual experience with a focus on frame rate.\n\nResults in a slightly pixelated appearance, but ensures straight lines for shadows, unlike cheap bilinear filtering, which may make shadow lines appear crooked.")]
        Low,

        /// <summary>
        /// Uses bilinear filtering to smooth shadows, offering a step up in visual quality.
        /// <para>Balances performance and quality, delivering improved shadow edges and smoother gradients.</para>
        /// <para>Suitable for mid-range systems aiming for stable performance with higher fidelity.</para>
        /// </summary>
        [Tooltip("Uses bilinear filtering to smooth shadows, offering a step up in visual quality.\n\nBalances performance and quality, delivering improved shadow edges and smoother gradients.\n\nSuitable for mid-range systems aiming for stable performance with higher fidelity.")]
        Medium,

        /// <summary>
        /// Advanced shadow filtering with bilinear filtering and a 5x5 Gaussian filter for smoother results.
        /// <para>Enhances shadow softness and reduces visual artifacts, providing a high-quality lighting experience.</para>
        /// <para>Recommended for high-end systems prioritizing visual quality and immersive shadow detail.</para>
        /// </summary>
        [Tooltip("Advanced shadow filtering with bilinear filtering and a 5x5 Gaussian filter for smoother results.\n\nEnhances shadow softness and reduces visual artifacts, providing a high-quality lighting experience.\n\nRecommended for high-end systems prioritizing visual quality and immersive shadow detail.")]
        High,
    }
}