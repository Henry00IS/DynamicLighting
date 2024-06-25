using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Represents an object that can be put inside of <see cref="BvhAccelerationStructure{T}"/>.</summary>
    internal interface IBvhObject
    {
        /// <summary>Gets the world position of the object.</summary>
        public Vector3 position { get; }

        /// <summary>The <see cref="Bounds"/> of the object inside of the Bounding Volume Hierarchy.</summary>
        public Bounds bounds { get; }
    }
}