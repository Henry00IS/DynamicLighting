using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a bounding volume node containing either more bounding volumes or lights
    /// (this struct is mirrored in the shader and can not be modified).
    /// <para>This struct is exactly 32 bytes and fits in half of a 64-byte GPU cache line.</para>
    /// <para>This struct uses 16-byte alignment for performance on the GPU.</para>
    /// </summary>
    internal struct BvhLightNode
    {
        /// <summary>The minimal point of the bounding box.</summary>
        public Vector3 aabbMin;
        /// <summary>When <see cref="isLeaf"/> contains the left node index else the first light index.</summary>
        public int leftFirst;

        // -- 16 byte boundary --

        /// <summary>The maximal point of the bounding box.</summary>
        public Vector3 aabbMax;
        /// <summary>The amount of lights contained within this bounding volume.</summary>
        public int count;

        // -- 16 byte boundary --

        /// <summary>Gets whether this node is a leaf containing light source indices.</summary>
        public bool isLeaf => count > 0;

        /// <summary>Gets the center position of the bounding volume.</summary>
        public Vector3 center => (aabbMin + aabbMax) * 0.5f;

        /// <summary>Gets the size of the bounding volume.</summary>
        public Vector3 size => aabbMax - aabbMin;

        /// <summary>When not <see cref="isLeaf"/> contains the right node index.</summary>
        public int rightNode => leftFirst + 1;
    }
}