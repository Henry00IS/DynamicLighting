using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Stores metadata for a mesh renderer in the scene used by <see cref="DynamicLightManager"/>.
    /// This is a reverse approach to associate additional data with a scene object without
    /// modifying said object.
    /// </summary>
    [System.Serializable]
    internal class RaycastedMeshRenderer
    {
        /// <summary>The reference renderer in the scene that this metadata is for.</summary>
        public MeshRenderer renderer;

        /// <summary>The unique identifier used to find the binary data files on disk.</summary>
        public int identifier;

        /// <summary>The lightmap resolution equal width and height.</summary>
        public int resolution;

        /// <summary>The binary triangle data buffer uploaded to the graphics card.</summary>
        public ComputeBuffer buffer;
    }
}