using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Stores metadata for a dynamic light in the scene used by <see cref="DynamicLightManager"/>.
    /// This is a reverse approach to associate additional data with a scene object without
    /// modifying said object.
    /// </summary>
    [System.Serializable]
    internal class RaycastedDynamicLight
    {
        /// <summary>The reference dynamic light in the scene that this metadata is for.</summary>
        public DynamicLight light;
    }
}