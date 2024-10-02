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

        /// <summary>
        /// The light appears as a volumetric cone of fog. The rotation can be manipulated using the
        /// rotation of the game object. The cone angle is controlled by 'Light Outer Cutoff' and
        /// the effect is best used with the 'Spot' light type.
        /// </summary>
        [Tooltip("The light appears as a volumetric cone of fog. The rotation can be manipulated using the rotation of the game object. The cone angle is controlled by 'Light Outer Cutoff' and the effect is best used with the 'Spot' light type.")]
        ConeZ = 3,

        /// <summary>
        /// The light appears as a volumetric cone of fog. The rotation can be manipulated using the
        /// rotation of the game object. The cone angle is controlled by 'Light Outer Cutoff' and
        /// the effect is best used with the 'Interference', 'Rotor' and 'Disco' light types.
        /// </summary>
        [Tooltip("The light appears as a volumetric cone of fog. The rotation can be manipulated using the rotation of the game object. The cone angle is controlled by 'Light Outer Cutoff' and the effect is best used with the 'Interference', 'Rotor' and 'Disco' light types.")]
        ConeY = 4,
    }
}