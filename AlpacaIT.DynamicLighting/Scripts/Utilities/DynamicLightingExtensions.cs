using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Extension methods for developers using the Dynamic Lighting library.</summary>
    public static class DynamicLightingExtensions
    {
        /// <summary>Adds a realtime dynamic light component to the object.</summary>
        /// <returns>The realtime light source.</returns>
        public static DynamicLight AddDynamicLightComponent(this GameObject go)
        {
            var light = go.AddComponent<DynamicLight>();
            light.lightChannel = DynamicLightManager.realtimeLightChannel;
            return light;
        }
    }
}