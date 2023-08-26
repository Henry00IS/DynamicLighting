using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>A dynamic shape (this struct is mirrored in the shader and can not be modified).</summary>
    internal struct ShaderDynamicShape
    {
        /// <summary>The position of the shape in world space.</summary>
        public Vector3 position;
        /// <summary>The size of the shape.</summary>
        public Vector3 size;
        /// <summary>The flags of the shape.</summary>
        public uint flags;
    };
}