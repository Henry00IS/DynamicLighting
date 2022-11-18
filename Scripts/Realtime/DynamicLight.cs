using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>A dynamic light (this struct is mirrored in the shader and can not be modified).</summary>
    public struct DynamicLight
    {
        /// <summary>The position of the light in world space.</summary>
        public Vector3 position;
        /// <summary>The Red, Green, Blue color of the light in that order.</summary>
        public Vector3 color;
        /// <summary>The intensity (or brightness) of the light.</summary>
        public float intensity;
        /// <summary>The maximum cutoff radius where the light is guaranteed to end.</summary>
        public float radius;
        /// <summary>The channel 0-31 bit in the lightmap that the light uses for raytraced shadows.</summary>
        public uint channel;
    };
}