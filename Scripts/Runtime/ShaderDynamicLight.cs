using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>A dynamic light (this struct is mirrored in the shader and can not be modified).</summary>
    internal struct ShaderDynamicLight
    {
        /// <summary>The position of the light in world space.</summary>
        public Vector3 position;
        /// <summary>The Red, Green, Blue color of the light in that order.</summary>
        public Vector3 color;
        /// <summary>The intensity (or brightness) of the light.</summary>
        public float intensity;
        /// <summary>The maximum cutoff radius where the light is guaranteed to end.</summary>
        public float radiusSqr;
        /// <summary>
        /// The channel 0-31 representing the bit in the lightmap that the light uses for raytraced shadows.
        /// <para>32 is used to indicate a realtime light without raytraced shadows.</para>
        /// <para>All other bits are reserved for internal use.</para>
        /// </summary>
        public uint channel;

        /// <summary>The up direction of the light.</summary>
        public Vector3 up;
        /// <summary>The forward direction of the light.</summary>
        public Vector3 forward;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat1;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat2;
        /// <summary>General purpose floating point value.</summary>
        public float gpFloat3;
        /// <summary>The shimmer scale.</summary>
        public float shimmerScale;
        /// <summary>The shimmer modifier.</summary>
        public float shimmerModifier;
    };
}