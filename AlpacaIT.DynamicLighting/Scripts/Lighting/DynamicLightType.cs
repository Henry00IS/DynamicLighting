using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The type of dynamic light (e.g. point light or a spot light etc.).</summary>
    public enum DynamicLightType
    {
        /// <summary>
        /// The light is a point light that emits light equally in all directions, similar to a
        /// light bulb or a candle flame, as all rays originate from a single point.
        /// </summary>
        [Tooltip("The light is a point light that emits light equally in all directions, similar to a light bulb or a candle flame, as all rays originate from a single point.")]
        Point,

        /// <summary>
        /// The light is a spot light whose beam can be directed, it appears as a circle of light.
        /// </summary>
        [Tooltip("The light is a spot light whose beam can be directed, it appears as a circle of light.")]
        Spot,

        /// <summary>
        /// The light is a discoball with 24 spot lights, that appear as circles of light.
        /// </summary>
        [Tooltip("The light is a discoball with 24 spot lights, that appear as circles of light.")]
        Discoball,

        /// <summary>The light is a wave that starts from the middle and flows outwards.</summary>
        [Tooltip("The light is a wave that starts from the middle and flows outwards.")]
        Wave,

        /// <summary>The light is a wave that starts at the bottom and flows to the top.</summary>
        [Tooltip("The light is a wave that starts at the bottom and flows to the top.")]
        Interference,

        /// <summary>The light is a rotor much like a fan that rotates around.</summary>
        [Tooltip("The light is a rotor much like a fan that rotates around.")]
        Rotor,

        /// <summary>The light is a pulsating wave that starts from the middle and flows outwards.</summary>
        [Tooltip("The light is a pulsating wave that starts from the middle and flows outwards.")]
        Shock,

        /// <summary>
        /// The light is a disco ball with spot lights that originate from the bottom and flow to
        /// the top while rotating.
        /// </summary>
        [Tooltip("The light is a disco ball with spot lights that originate from the bottom and flow and rotate towards the top.")]
        Disco,
    }
}