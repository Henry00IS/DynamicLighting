using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different shimmering that can be applied to a dynamic light.</summary>
    public enum DynamicLightShimmer
    {
        /// <summary>The light does not project any shimmering on world geometry (default)</summary>
        [Tooltip("The light does not project any shimmering on world geometry (default).")]
        None = 0,

        /// <summary>
        /// The water shimmer effect overlays the world with random blocks that smoothly change
        /// between dark and bright. Combined with trilinear filtering it is meant to look like
        /// shimmering water, but can also be useful for other scenarios.
        /// </summary>
        [Tooltip("The water shimmer effect overlays the world with random blocks that smoothly change between dark and bright. Combined with trilinear filtering it is meant to look like shimmering water, but can also be useful for other scenarios.")]
        Water = 1,

        /// <summary>
        /// The random shimmer effect overlays the world with random blocks that change intensity
        /// randomly and unpredictably. Combined with trilinear filtering it is meant to look like
        /// shimmering fire, but can also be useful for other scenarios.
        /// </summary>
        [Tooltip("The random shimmer effect overlays the world with random blocks that change intensity randomly and unpredictably. Combined with trilinear filtering it is meant to look like shimmering fire, but can also be useful for other scenarios.")]
        Random = 2,
    }
}