using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The raytracing engine modes that handle calculating shadows in the scene.</summary>
    public enum DynamicLightingTracerEngine
    {
        /// <summary>
        /// The default raytracing engine that has been tested thoroughly. It has precision problems
        /// with directional light sources that are far away.
        /// </summary>
        [Tooltip("The default raytracing engine that has been tested thoroughly. It has precision problems with directional light sources that are far away.")]
        Default,

        /// <summary>
        /// The latest experimental raytracing engine. It solves the precision problems with
        /// directional light sources that are far away. It will generate the most precise shadows.
        /// </summary>
        [InspectorName("Adaptive (Experimental)")]
        [Tooltip("The latest experimental raytracing engine. It solves the precision problems with directional light sources that are far away. It will also generate the most precise shadows.")]
        Adaptive,
    }
}