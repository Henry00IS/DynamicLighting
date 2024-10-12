using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// A dynamic light (this struct is mirrored in the shader and can not be modified).
    /// <para>This struct uses 16-byte alignment for performance on the GPU.</para>
    /// </summary>
    internal struct ShaderDynamicLight
    {
        /// <summary>The position of the light in world space.</summary>
        public Vector3 position;
        /// <summary>The maximum cutoff radius where the light is guaranteed to end.</summary>
        public float radiusSqr;

        // -- 16 byte boundary --

        /// <summary>
        /// The channel contains bit flags that determine how the light gets rendered.
        /// <para>32 (bit 6) is used to indicate a realtime light without raytraced shadows.</para>
        /// <para>All other bits are reserved for internal use.</para>
        /// </summary>
        public uint channel;
        /// <summary>The intensity (or brightness) of the light.</summary>
        public float intensity;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat1;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat2;

        // -- 16 byte boundary --

        /// <summary>The Red, Green, Blue color of the light in that order.</summary>
        public Vector3 color;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat3;

        // -- 16 byte boundary --

        /// <summary>The up direction of the light.</summary>
        public Vector3 up;
        /// <summary>The shimmer scale.</summary>
        public float shimmerScale;

        // -- 16 byte boundary --

        /// <summary>The forward direction of the light.</summary>
        public Vector3 forward;
        /// <summary>The shimmer modifier.</summary>
        public float shimmerModifier;

        // -- 16 byte boundary --

        /// <summary>
        /// The volumetric fog intensity modifier changes the transparency of the final fog.
        /// </summary>
        public float volumetricIntensity;

        /// <summary>The visibility in meters within the volumetric fog.</summary>
        public float volumetricVisibility;

        /// <summary>The cookie index for light-cookie texture projections.</summary>
        public uint cookieIndex;

        /// <summary>
        /// The shadow cubemap index for real-time shadows and static shadows on dynamic meshes.
        /// </summary>
        public uint shadowCubemapIndex;

        // -- 16 byte boundary --

        /// <summary>
        /// The falloff parameter controls the decay of light within its radius, providing artistic
        /// flexibility over light attenuation. While setting this above zero deviates from physical
        /// accuracy, it enables unique effects, such as a desk lamp emitting a bright, localized light.
        /// <para>
        /// The value itself doesn't correspond to any easily understood unit. Internally, it's
        /// adjusted to stay consistent with the light radius. A value of 0.0 disables the falloff,
        /// while 1.0 matches the falloff to the light radius. This "magic number" requires some
        /// experimentation to achieve the desired effect but not exceeding 1.0 is a good rule.
        /// </para>
        /// </summary>
        public float falloff;

        /// <summary>The Red, Green, Blue color of the bounce lighting in that order.</summary>
        public Vector3 bounceColor;

        // -- 16 byte boundary --
    };
}