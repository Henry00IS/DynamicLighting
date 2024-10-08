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
        /// After calculating direct illumination, this method performs a single indirect
        /// illumination pass, where directly illuminated texels cast light onto other surfaces.
        /// This enhances realism by simulating light bounce, resulting in more natural and detailed
        /// lighting effects and avoids the black shadows.
        /// <para>
        /// The bounce lighting operates independently of the initial light intensity, meaning you
        /// can bake it even if the light source is completely dark. The primary factor influencing
        /// bounce lighting is the light radius, which defines how far the indirect illumination
        /// spreads. This method requires an additional 8 bits per texel of VRAM on individual
        /// triangle surfaces receiving bounce light.
        /// </para>
        /// <para>
        /// Indirect illumination can not extend beyond the light radius, so it is recommended to
        /// apply this technique only to important lights with a substantial radius. The quality of
        /// the bounce data degrades and blurs over distance, so avoid setting the light radius too
        /// large for optimal results. Additionally, be aware that this approach may significantly
        /// increase raytracing times due to the additional calculations involved.
        /// </para>
        /// </summary>
        [Tooltip("After calculating direct illumination, this method performs a single indirect illumination pass, where directly illuminated texels cast light onto other surfaces. This enhances realism by simulating light bounce, resulting in more natural and detailed lighting effects and avoids the black shadows.\n\nThe bounce lighting operates independently of the initial light intensity, meaning you can bake it even if the light source is completely dark. The primary factor influencing bounce lighting is the light radius, which defines how far the indirect illumination spreads. This method requires an additional 8 bits per texel of VRAM on individual triangle surfaces receiving bounce light.\n\nIndirect illumination can not extend beyond the light radius, so it is recommended to only apply this technique to important lights with a substantial radius. The quality of the bounce data degrades and blurs over distance, so avoid setting the light radius too large for optimal results. Additionally, be aware that this approach may significantly increase raytracing times due to the additional calculations involved.")]
        SingleBounce = 1,
    }
}