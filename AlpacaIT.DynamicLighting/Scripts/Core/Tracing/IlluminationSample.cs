using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a point in 3D space that was illuminated by a light source. This is used to
    /// calculate bounce lighting.
    /// </summary>
    public struct IlluminationSample
    {
        /// <summary>
        /// The world position that was originally used to trace to the light source (is slightly
        /// offset using the triangle normal).
        /// </summary>
        public Vector3 world;

        /// <summary>The triangle normal the photon hit.</summary>
        public Vector3 normal;

        public IlluminationSample(Vector3 world, Vector3 normal)
        {
            this.world = world;
            this.normal = normal;
            //this.pointLightIndex = pointLightIndex;
        }
    }
}