using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Dynamic light sources can be moved in the scene, where they will be treated as real-time
    /// lights without shadows. While this approach is easy to work with, it requires a background
    /// process to continuously track the positions of all light sources. This uses some
    /// computational power which may not be available in your project. Moving raytraced lights
    /// (with the intention to use them as real-time lights) also incurs a performance cost on the
    /// GPU compared to actual real-time light sources and is therefore not recommended.
    /// Alternatively, it is possible to only update all positions when required, such as a
    /// raytraced light (or the game object with a raytraced light) getting enabled in the scene.
    /// This relaxes the system and reduces the computational overhead. Note that volumetric fog
    /// that uses the game object scale will also not be updated. An exception is the light rotation
    /// which will always be updated no matter which mode is used.
    /// </summary>
    public enum DynamicLightTrackingMode
    {
        /// <summary>
        /// The position and scale of raytraced dynamic light sources are continuously updated every
        /// frame. This is also the default behavior in edit mode of the Unity Editor. This uses the
        /// Unity Job System and is usually very fast (default).
        /// </summary>
        [Tooltip("The position and scale of raytraced dynamic light sources are continuously updated every frame. This is also the default behavior in edit mode of the Unity Editor. This uses the Unity Job System and is usually very fast (default).")]
        LiveTracking,

        /// <summary>
        /// The position and scale of raytraced dynamic light sources are rarely updated, such as
        /// when a light source or the game object gets enabled or when manually requested from C#
        /// using <see cref="DynamicLightManager.RequestLightTrackingUpdate"/>. This only applies
        /// during play mode or in builds.
        /// </summary>
        [Tooltip("The position and scale of raytraced dynamic light sources are rarely updated, such as when a light source or the game object gets enabled or when manually requested from C#. This only applies during play mode or in builds.")]
        RelaxedTracking,
    }
}