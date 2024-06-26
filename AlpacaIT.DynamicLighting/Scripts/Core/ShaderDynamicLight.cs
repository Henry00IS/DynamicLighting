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
    };
}