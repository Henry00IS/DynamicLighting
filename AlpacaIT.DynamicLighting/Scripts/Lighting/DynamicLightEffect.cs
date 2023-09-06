using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different effects that can be applied to a dynamic light.</summary>
    public enum DynamicLightEffect
    {
        /// <summary>The light is stable and consistent without any effects (default)</summary>
        [Tooltip("The light is stable and consistent without any effects (default).")]
        Steady = 0,

        /// <summary>
        /// The light smoothly pulses between fully bright and dark over time using a sine wave. How
        /// dim the light will become and how many times per second the light will pulse can be configured.
        /// </summary>
        [Tooltip("The light smoothly pulses between fully bright and dark over time. How dim the light will become and how many times per second the light will pulse can be configured.")]
        Pulse = 1,

        /// <summary>
        /// The light randomly picks an intensity between fully bright and fully dark every update
        /// of the configurable framerate independent fixed timestep frequency.
        /// </summary>
        [Tooltip("The light randomly picks an intensity between fully bright and fully dark every update of the configurable framerate independent fixed timestep frequency.")]
        Random = 2,

        /// <summary>
        /// The light randomly toggles the intensity between fully bright and fully dark every
        /// update of the configurable framerate independent fixed timestep frequency to simulate a
        /// strobe light.
        /// </summary>
        [Tooltip("The light randomly toggles the intensity between fully bright and fully dark every update of the configurable framerate independent fixed timestep frequency to simulate a strobe light.")]
        Strobe = 3,

        /// <summary>
        /// The light randomly picks a number between 0 and 1. If the number is smaller than 0.5 the
        /// light will be fully dark otherwise it multiplies the fully bright intensity with the
        /// number. It does this every update of the configurable framerate independent fixed
        /// timestep frequency. This is similar to random intensity except that the light turns off
        /// more often giving that sense that the light is either broken or electricity is sparking etc.
        /// </summary>
        [Tooltip("The light randomly picks a number between 0 and 1. If the number is smaller than 0.5 the light will be fully dark otherwise it multiplies the fully bright intensity with the number. It does this every update of the configurable framerate independent fixed timestep frequency. This is similar to random intensity except that the light turns off more often giving that sense that the light is either broken or electricity is sparking etc.")]
        Flicker = 4,
    }
}