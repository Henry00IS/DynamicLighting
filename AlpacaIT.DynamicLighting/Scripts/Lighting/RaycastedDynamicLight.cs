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

        /// <summary>The original position the immovable dynamic light was raycasted at.</summary>
        public Vector3 origin = Vector3.positiveInfinity;

        /// <summary>Creates a new instance for the given <see cref="DynamicLight"/>.</summary>
        /// <param name="dynamicLight">The <see cref="DynamicLight"/> to be referenced.</param>
        public RaycastedDynamicLight(DynamicLight dynamicLight)
        {
            light = dynamicLight;
            origin = dynamicLight.transform.position;
        }
    }
}