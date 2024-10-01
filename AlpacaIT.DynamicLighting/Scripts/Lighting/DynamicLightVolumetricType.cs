using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different volumetric types that can be applied to a dynamic light.</summary>
    public enum DynamicLightVolumetricType
    {
        /// <summary>The light does not appear as a volumetric shape (default).</summary>
        [Tooltip("The light does not appear volumetric (default).")]
        None = 0,

        /// <summary>The light appears as a volumetric sphere of fog.</summary>
        [Tooltip("The light appears as a volumetric sphere of fog.")]
        Sphere = 1,

        /// <summary>
        /// The light appears as a volumetric box of fog. The size can be manipulated using the
        /// scale of the game object and the volumetric radius.
        /// </summary>
        [Tooltip("The light appears as a volumetric box of fog. The size can be manipulated using the scale of the game object and the volumetric radius.")]
        Box = 2,
    }
}