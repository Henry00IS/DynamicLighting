using System;
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

        /// <summary>
        /// The active sub mesh count based on the amount of materials (valid after <see cref="DynamicLightManager.OnEnable"/>).
        /// </summary>
        [NonSerialized]
        internal int activeSubMeshCount;

        /// <summary>Used in the editor to detect mesh data changes on raycasted geometry.</summary>
        [NonSerialized]
        internal MeshFilter lastMeshFilter;

        /// <summary>Used in the editor to detect mesh data changes on raycasted geometry.</summary>
        [NonSerialized]
        internal int lastMeshHash;

        /// <summary>Used in the editor to remember that the mesh data changed on this geometry.</summary>
        [NonSerialized]
        internal bool lastMeshModified;
    }
}