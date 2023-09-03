using System.Collections.Generic;
using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>A node encompassing a subset of bounding volumes or object(s).</summary>
    public struct BvhNode
    {
        /// <summary>The minimal point of the axis-aligned bounding box.</summary>
        public float3 min;

        /// <summary>The maximal point of the axis-aligned bounding box.</summary>
        public float3 max;

        /// <summary>The left bounding volume at this node (if any).</summary>
        public int leftChild;

        /// <summary>The right bounding volume at this node (if any).</summary>
        public int rightChild;
    }
}