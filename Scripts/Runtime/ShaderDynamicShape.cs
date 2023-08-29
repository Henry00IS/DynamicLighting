using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>A dynamic shape (this struct is mirrored in the shader and can not be modified).</summary>
    internal struct ShaderDynamicShape
    {
        /// <summary>The position of the shape in world space.</summary>
        public Vector3 position;
        /// <summary>The flags of the shape.</summary>
        public uint type;

        // -- 16 byte boundary --

        /// <summary>The size of the shape.</summary>
        public Vector3 size;
        /// <summary>The rotation matrix of the shape.</summary>
        public float3x3 rotation;
    };
}