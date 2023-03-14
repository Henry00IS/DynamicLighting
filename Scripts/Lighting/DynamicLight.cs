using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicLight : MonoBehaviour
    {
        /// <summary>
        /// The color of the light. It would be best to keep this number within the RGB range (i.e.
        /// not HDR with super bright colors) so that both current and future shader math
        /// calculations are correct.
        /// </summary>
        [Tooltip("The color of the light. It would be best to keep this number within the RGB range (i.e. not HDR with super bright colors) so that both current and future shader math calculations are correct.")]
        public Color lightColor = Color.white;

        /// <summary>
        /// The intensity of the light, which does not affect the range of the light, only how
        /// bright it is.
        /// </summary>
        [Tooltip("The intensity of the light, which does not affect the range of the light, only how bright it is.")]
        public float lightIntensity = 2.0f;

        /// <summary>
        /// The spherical radius that the light will occupy. The light cannot exceed this radius and
        /// is guaranteed to be completely gone when it reaches the end. Each light type (e.g. spot
        /// lights) will always use this entire radius for their calculations. There must never be
        /// more than 32 lights all overlapping each other (i.e. some pixel in the level getting
        /// illuminated by 32 lights- quite a lot), otherwise an error will occur.
        /// </summary>
        [Tooltip("The spherical radius that the light will occupy. The light cannot exceed this radius and is guaranteed to be completely gone when it reaches the end. Each light type (e.g. spot lights) will always use this entire radius for their calculations.\n\nThere must never be more than 32 lights all overlapping each other (i.e. some pixel in the level getting illuminated by 32 lights- quite a lot), otherwise an error will occur.")]
        [Min(0f)]
        public float lightRadius = 4.0f;

        /// <summary>
        /// This is the channel that the light occupies. This value is automatically assigned to the
        /// light during ray tracing. Dynamic realtime lights must always use channel 32 (they can
        /// move around the scene without shadows). This value is a crucial part of the inner
        /// workings of the lighting system. You should not touch this field unless you know what
        /// you are doing or to simply set it to 32 to make it a realtime light. The raytracer will
        /// ignore lights that are on channel 32 (i.e. it will not reset this channel to something
        /// else). It can be useful to temporarily set the channel to 32 to adjust the light in an
        /// already raytraced scene. As a realtime light, it will be easier to see what it will look
        /// like. Then select any value between 0-31 and raytrace the scene again. This will then
        /// assign a proper channel. This channel is assigned a number that no other overlapping
        /// light uses so that each light has its own unique space in memory to store shadows in.
        /// </summary>
        [Tooltip("This is the channel that the light occupies. This value is automatically assigned to the light during ray tracing. Dynamic realtime lights must always use channel 32 (they can move around the scene without shadows). This value is a crucial part of the inner workings of the lighting system. You should not touch this field unless you know what you are doing or to simply set it to 32 to make it a realtime light. The raytracer will ignore lights that are on channel 32 (i.e. it will not reset this channel to something else).\n\nIt can be useful to temporarily set the channel to 32 to adjust the light in an already raytraced scene. As a realtime light, it will be easier to see what it will look like. Then select any value between 0-31 and raytrace the scene again. This will then assign a proper channel.\n\nThis channel is assigned a number that no other overlapping light uses so that each light has its own unique space in memory to store shadows in.")]
        [Range(0, 32)]
        public uint lightChannel = 0;

        /// <summary>The type of dynamic light (e.g. point light or a spot light etc.).</summary>
        [Tooltip("The type of dynamic light (e.g. point light or a spot light etc.).")]
        public DynamicLightType lightType = DynamicLightType.Point;

        /// <summary>
        /// When using the 'Spot' light type, this specifies the outer cutoff angle in degrees where
        /// the light is darkest. There is a smooth transition between the inner and outer cutoff
        /// angle. The outer cutoff angle must be larger or equal to the inner cutoff angle
        /// otherwise the light will appear to turn off.
        /// </summary>
        [Tooltip("When using the 'Spot' light type, this specifies the outer cutoff angle in degrees where the light is darkest. There is a smooth transition between the inner and outer cutoff angle. The outer cutoff angle must be larger or equal to the inner cutoff angle otherwise the light will appear to turn off.")]
        [Range(0f, 180f)]
        public float lightOuterCutoff = 30.0f;

        /// <summary>
        /// When using the 'Spot' light type, this specifies the inner cutoff angle in degrees where
        /// the light is brightest. There is a smooth transition between the inner and outer cutoff
        /// angle. The outer cutoff angle must be larger or equal to the inner cutoff angle
        /// otherwise the light will appear to turn off.
        /// </summary>
        [Tooltip("When using the 'Spot' light type, this specifies the inner cutoff angle in degrees where the light is brightest. There is a smooth transition between the inner and outer cutoff angle. The outer cutoff angle must be larger or equal to the inner cutoff angle otherwise the light will appear to turn off.")]
        [Range(0f, 180f)]
        public float lightCutoff = 26.0f;

        /// <summary>
        /// When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this
        /// specifies how fast the waves move around the light source. This number can be negative
        /// to reverse the effect.
        /// </summary>
        [Tooltip("When using the 'Wave' or 'Interference' light types, this specifies how fast the waves move around the light source. This number can be negative to reverse the effect.")]
        public float lightWaveSpeed = 1f;

        /// <summary>
        /// When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this
        /// changes the frequency of the waves. A higher number produces more waves that are closer together.
        /// </summary>
        [Tooltip("When using the 'Wave' or 'Interference' light types, this changes the frequency of the waves. A higher number produces more waves that are closer together.")]
        [Min(0f)]
        public float lightWaveFrequency = 1f;

        /// <summary>
        /// When using the 'Rotor' light type, this changes the scale of the blob of light or shadow
        /// in the center. A negative number adds a shadow instead of a blob of light.
        /// </summary>
        [Tooltip("When using the 'Rotor' light type, this changes the scale of the blob of light or shadow in the center. A negative number adds a shadow instead of a blob of light.")]
        [Range(-1f, 1f)]
        public float lightRotorCenter = 0.2f;

        /// <summary>
        /// When using the 'Disco' light type, this specifies how fast the lights move vertically
        /// around the light source. This number can be negative to reverse the effect.
        /// </summary>
        [Tooltip("When using the 'Disco' light type, this specifies how fast the lights move vertically around the light source. This number can be negative to reverse the effect.")]
        public float lightDiscoVerticalSpeed = 1.0f;

        /// <summary>
        /// The shimmer effects overlay the world with random blocks that project water caustics or
        /// fire wavering.
        /// </summary>
        [Tooltip("The shimmer effects overlay the world with random blocks that project water caustics or fire wavering.")]
        public DynamicLightShimmer lightShimmer = DynamicLightShimmer.None;

        /// <summary>
        /// When using a shimmering light, this specifies the scale of the caustics. In the shader,
        /// the world is essentially overlaid with persistent random value blocks from 0.0 to 1.0
        /// that do not change. Then sine waves and time are multiplied against these blocks to
        /// create the effect. This property changes the size of the blocks. This is all
        /// mathematics, there is no difference in performance.
        /// </summary>
        [Tooltip("When using a shimmering light, this specifies the scale of the caustics. In the shader, the world is essentially overlaid with persistent random value blocks from 0.0 to 1.0 that do not change. Then sine waves and time are multiplied against these blocks to create the effect. This property changes the size of the blocks. This is all mathematics, there is no difference in performance.")]
        [Min(0f)]
        public float lightShimmerScale = 12.25f;

        /// <summary>
        /// When using a shimmering light, this specifies how dim the caustics can become per
        /// projected block, where 0 is completely off and 1 does nothing.
        /// </summary>
        [Tooltip("When using a shimmering light, this specifies how dim the caustics can become per projected block, where 0 is completely off and 1 does nothing.")]
        [Range(0f, 1f)]
        public float lightShimmerModifier = 0.8f;

        /// <summary>The effect applied to this dynamic light.</summary>
        [Tooltip("The effect applied to this dynamic light.")]
        public DynamicLightEffect lightEffect = DynamicLightEffect.Steady;

        /// <summary>
        /// When using the 'Pulse' light effect, this specifies how many times per second the light
        /// should pulse (as a multiplier), where 1 means once per second.
        /// </summary>
        [Tooltip("When using the 'Pulse' light effect, this specifies how many times per second the light should pulse (as a multiplier), where 1 means once per second.")]
        public float lightEffectPulseSpeed = 1.0f;

        /// <summary>
        /// When using the 'Pulse', 'Random', 'Strobe' or 'Flicker' light effect, this specifies how
        /// dim the light can become per pulse, where 0 is completely off and 1 does nothing.
        /// </summary>
        [Tooltip("When using the 'Pulse', 'Random', 'Strobe' or 'Flicker' light effect, this specifies how dim the light can become per pulse, where 0 is completely off and 1 does nothing.")]
        [Range(0f, 1f)]
        public float lightEffectPulseModifier = 0.25f;

        /// <summary>
        /// The framerate independent fixed timestep frequency for lighting effects in seconds. For
        /// example a frequency of 30Hz would be achieved using the formula 1f / 30f.
        /// <para>
        /// Used to decouple the lighting calculations from the framerate. If you have a flickering
        /// light or strobe light, the light may be on over several frames. If you are playing VR at
        /// 144Hz then the light may only turn on and off 30 times per second, giving you that sense
        /// of reality, opposed to having a light flicker at 144 times per second causing visual
        /// noise but no distinct on/off period.
        /// </para>
        /// </summary>
        [Min(0.00001f)]
        [Tooltip("The framerate independent fixed timestep frequency for lighting effects in seconds. For example a frequency of 30Hz would be achieved using the formula 1f / 30f.\n\nUsed to decouple the lighting calculations from the framerate. If you have a flickering light or strobe light, the light may be on over several frames. If you are playing VR at 144Hz then the light may only turn on and off 30 times per second, giving you that sense of reality, opposed to having a light flicker at 144 times per second causing visual noise but no distinct on/off period.")]
        public float lightEffectTimestepFrequency = 1f / 30f;

        /// <summary>Gets whether this dynamic light is realtime (no shadows, channel 32).</summary>
        public bool realtime { get => lightChannel == 32; }

        /// <summary>Stores dynamic light runtime effect values that change at irregular intervals.</summary>
        internal DynamicLightCache cache = new DynamicLightCache();

        private void OnEnable()
        {
            DynamicLightManager.Instance.RegisterDynamicLight(this);
        }

        private void OnDisable()
        {
            if (DynamicLightManager.hasInstance)
                DynamicLightManager.Instance.UnregisterDynamicLight(this);
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = lightColor;

            Gizmos.DrawIcon(transform.position, "Packages/de.alpacait.dynamiclighting/Gizmos/DynamicLightingPointLight.psd", true, lightColor);
        }

#endif
    }
}