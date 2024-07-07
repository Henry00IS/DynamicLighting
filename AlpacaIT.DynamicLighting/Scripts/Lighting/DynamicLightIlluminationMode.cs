using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different illumination modes that can be applied to a dynamic light.</summary>
    public enum DynamicLightIlluminationMode
    {
        /// <summary>
        /// This option computes only direct illumination, using minimal graphics card memory by
        /// storing a 1-bit per pixel shadow occlusion bitmask on individual triangles illuminated
        /// by the light (default).
        /// </summary>
        [Tooltip("This option computes only direct illumination, using minimal graphics card memory by storing a 1-bit per pixel shadow occlusion bitmask on individual triangles illuminated by the light (default).")]
        DirectIllumination = 0,

        /// <summary>
        /// After calculating direct illumination, performs a single indirect illumination pass
        /// where directly illuminated texels cast light onto other surfaces. This enhances realism
        /// by simulating light bounce, providing more natural and detailed lighting effects. This
        /// method uses 32 bits per texel of memory on individual triangles illuminated by the light.
        /// <para>
        /// Indirect illumination can not spread beyond the light radius, it is recommended to use
        /// this option on important lights with a large radius. The quality of the bounce data
        /// degrades and blurs over distance, so avoid setting the light radius too large for
        /// optimal results. Additionally, be aware that this approach will significantly increase
        /// raytracing times.
        /// </para>
        /// </summary>
        [Tooltip("After calculating direct illumination, performs a single indirect illumination pass where directly illuminated texels cast light onto other surfaces. This enhances realism by simulating light bounce, providing more natural and detailed lighting effects. This method uses 32 bits per texel of memory on individual triangles illuminated by the light.\n\nIndirect illumination can not spread beyond the light radius, it is recommended to use this option on important lights with a large radius. The quality of the bounce data degrades and blurs over distance, so avoid setting the light radius too large for optimal results. Additionally, be aware that this approach will significantly increase raytracing times.")]
        SingleBounce = 1,
    }
}