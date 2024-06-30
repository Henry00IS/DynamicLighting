using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a point in 3D space that was illuminated by a light source. This is used to
    /// calculate bounce lighting in a secondary pass.
    /// </summary>
    public struct IlluminationSample
    {
        /// <summary>
        /// The world position that was originally used to trace to the light source (is slightly
        /// offset using the surface normal).
        /// </summary>
        public Vector3 world;

        /// <summary>The normal of the surface the photon hit.</summary>
        public Vector3 normal;

        /// <summary>The color of the surface the photon hit.</summary>
        public Vector3 color;

        /// <summary>Creates a new illumination sample.</summary>
        /// <param name="world">
        /// The world position that was originally used to trace to the light source (is slightly
        /// offset using the surface normal).
        /// </param>
        /// <param name="normal">The normal of the surface the photon hit.</param>
        public IlluminationSample(Vector3 world, Vector3 normal, Vector3 color)
        {
            this.world = world;
            this.normal = normal;
            this.color = color;
        }
    }
}