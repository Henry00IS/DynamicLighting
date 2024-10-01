using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Stores dynamic light runtime effect values that change at irregular intervals.</summary>
    internal class DynamicLightCache
    {
        /// <summary>Whether the cache has been initialized (used to detect the first frame).</summary>
        public bool initialized = false;

        /// <summary>
        /// The framerate independent fixed timestep calculator at 30Hz for lighting effects.
        /// <para>
        /// Used to decouple the lighting calculations from the framerate. If you have a flickering
        /// light or strobe light, the light may be on over several frames. If you are playing VR at
        /// 144Hz then the light may only turn on and off 30 times per second, giving you that sense
        /// of reality, opposed to having a light flicker at 144 times per second causing visual
        /// noise but no distinct on/off period.
        /// </para>
        /// </summary>
        public MathEx.FixedTimestep fixedTimestep = new MathEx.FixedTimestep(1f / 30f);

        /// <summary>The intensity (or brightness) of the light.</summary>
        public float intensity;

        /// <summary>
        /// [<see cref="DynamicLightEffect.Strobe"/>] whether the strobe light is active or not.
        /// </summary>
        public bool strobeActive;

        /// <summary>
        /// Used by raycasted lights to remember whether they already acted on changing the position
        /// away from the origin or back to the origin.
        /// </summary>
        public bool movedFromOrigin;

        /// <summary>
        /// It is expensive to call <see cref="Transform.position"/> and this vector stores it
        /// during <see cref="DynamicLightManager.Update"/>.
        /// </summary>
        public Vector3 transformPosition;

        /// <summary>
        /// It is expensive to call <see cref="Transform.localScale"/> and this vector stores it
        /// during <see cref="DynamicLightManager.Update"/>.
        /// </summary>
        public Vector3 transformScale;
    }
}