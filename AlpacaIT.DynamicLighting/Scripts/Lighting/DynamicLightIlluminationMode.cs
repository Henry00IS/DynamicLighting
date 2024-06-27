using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different illumination modes that can be applied to a dynamic light.</summary>
    public enum DynamicLightIlluminationMode
    {
        /// <summary>This only computes direct illumination without bounce lighting (default).</summary>
        [Tooltip("This only computes direct illumination without bounce lighting (default).")]
        DirectIllumination = 0,

        /// <summary>
        /// Does a single indirect illumination light bouncing pass where directly illuminated
        /// texels cast light to other surfaces.
        /// </summary>
        [Tooltip("Does a single indirect illumination light bouncing pass where directly illuminated texels cast light to other surfaces.")]
        SingleBounce = 1,
    }
}