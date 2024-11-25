using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// A dynamic light source that illuminates the scene. Use this to control all aspects of a
    /// dynamic light source in the scene such as color changes or flickering.
    /// </summary>
    [ExecuteInEditMode]
    public class DynamicLight : MonoBehaviour
    {
        /// <summary>
        /// The color of the light emitted by this point light source. The selected color affects
        /// how the light interacts with objects in the scene, influencing the overall mood and
        /// tone. Use warm colors like yellows and oranges for a cozy or natural feel, or cool
        /// colors like blues and greens for a calm or eerie atmosphere. Experimenting with
        /// different colors can enhance the realism or stylization of your scene.
        /// <para>
        /// When using bounce lighting with a custom bounce lighting color, you can set this color
        /// to black to only render the bounce lighting.
        /// </para>
        /// <para>
        /// It would be best to keep this number within the RGB range (i.e. not HDR with super
        /// bright colors) so that both current and future shader math calculations are correct.
        /// </para>
        /// </summary>
        [Tooltip("The color of the light emitted by this point light source. The selected color affects how the light interacts with objects in the scene, influencing the overall mood and tone. Use warm colors like yellows and oranges for a cozy or natural feel, or cool colors like blues and greens for a calm or eerie atmosphere. Experimenting with different colors can enhance the realism or stylization of your scene.\n\nWhen using bounce lighting with a custom bounce lighting color, you can set this color to black to only render the bounce lighting.")]
        [ColorUsage(showAlpha: false)]
        public Color lightColor = Color.white;

        /// <summary>
        /// The intensity of the light, which does not affect the range of the light, only how
        /// bright it is.
        /// </summary>
        [Tooltip("The intensity of the light, which does not affect the range of the light, only how bright it is.")]
        [Min(0f)]
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
        /// The falloff parameter controls the decay of light within its radius, providing artistic
        /// flexibility over light attenuation. While setting this above zero deviates from physical
        /// accuracy, it enables unique effects, such as a desk lamp emitting a bright, localized light.
        /// <para>
        /// The value itself doesn't correspond to any easily understood unit. Internally, it's
        /// adjusted to stay consistent with the light radius. A value of 0.0 disables the falloff,
        /// while 1.0 matches the falloff to the light radius. This "magic number" requires some
        /// experimentation to achieve the desired effect but not exceeding 1.0 is a good rule.
        /// </para>
        /// </summary>
        [Tooltip("The falloff parameter controls the decay of light within its radius, providing artistic flexibility over light attenuation. While setting this above zero deviates from physical accuracy, it enables unique effects, such as a desk lamp emitting a bright, localized light.\n\nThe value itself doesn't correspond to any easily understood unit. Internally, it's adjusted to stay consistent with the light radius. A value of 0.0 disables the falloff, while 1.0 matches the falloff to the light radius. This \"magic number\" requires some experimentation to achieve the desired effect but not exceeding 1.0 is a good rule.")]
        [Min(0f)]
        public float lightFalloff = 0.0f;

        /// <summary>
        /// This is the channel that the light occupies. This value is automatically assigned to the
        /// light during ray tracing. Dynamic realtime lights must always use channel 32 (they can
        /// move around the scene without shadows). This value is a crucial part of the inner
        /// workings of the lighting system. You should not touch this field unless you know what
        /// you are doing or to simply set it to 32 to make it a realtime light. The raytracer will
        /// ignore lights that are on channel 32 (i.e. it will not reset this channel to something
        /// else). This will then assign a proper channel. This channel is assigned a number that no
        /// other overlapping light uses so that each light has its own unique space in memory to
        /// store shadows in.
        /// </summary>
        [Tooltip("This is the channel that the light occupies. This value is automatically assigned to the light during ray tracing. Dynamic realtime lights must always use channel 32 (they can move around the scene without shadows). This value is a crucial part of the inner workings of the lighting system. You should not touch this field unless you know what you are doing or to simply set it to 32 to make it a realtime light. The raytracer will ignore lights that are on channel 32 (i.e. it will not reset this channel to something else).\n\nThis channel is assigned a number that no other overlapping light uses so that each light has its own unique space in memory to store shadows in.")]
        [Range(0, 32)]
        public uint lightChannel = 0;

        /// <summary>The type of dynamic light (e.g. point light or a spot light etc.).</summary>
        [Tooltip("The type of dynamic light (e.g. point light or a spot light etc.).")]
        public DynamicLightType lightType = DynamicLightType.Point;

        /// <summary>The shadow casting mode of the dynamic light (e.g. enabling real-time shadows).</summary>
        [Tooltip("The shadow casting mode of the dynamic light (e.g. enabling real-time shadows).")]
        public DynamicLightShadowMode lightShadows = DynamicLightShadowMode.RaytracedShadows;

        /// <summary>The illumination mode of the dynamic light (e.g. enabling bounce lighting).</summary>
        [Tooltip("The illumination mode of the dynamic light (e.g. enabling bounce lighting).")]
        public DynamicLightIlluminationMode lightIllumination = DynamicLightIlluminationMode.DirectIllumination;

        /// <summary>
        /// The transparency handling mode of the dynamic light (e.g. shining through transparent textures).
        /// </summary>
        [Tooltip("The transparency handling mode of the dynamic light (e.g. shining through transparent textures).")]
        public DynamicLightTransparencyMode lightTransparency = DynamicLightTransparencyMode.Disabled;

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
        [Tooltip("When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this specifies how fast the waves move around the light source. This number can be negative to reverse the effect.")]
        public float lightWaveSpeed = 1f;

        /// <summary>
        /// When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this
        /// changes the frequency of the waves. A higher number produces more waves that are closer together.
        /// </summary>
        [Tooltip("When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this changes the frequency of the waves. A higher number produces more waves that are closer together.")]
        [Min(0f)]
        public float lightWaveFrequency = 1f;

        /// <summary>
        /// When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this
        /// changes the time offset of the waves. This prevents lights from moving in sync and can
        /// also be used to programatically control the wave animation.
        /// </summary>
        [Tooltip("When using the 'Wave', 'Interference', 'Rotor', 'Shock' or 'Disco' light types, this changes the time offset of the waves. This prevents lights from moving in sync and can also be used to programatically control the wave animation.")]
        [Range(0f, 1f)]
        public float lightWaveOffset = 0f;

        /// <summary>
        /// When using the 'Rotor' light type, this changes the scale of the blob of light or shadow
        /// in the center. A negative number adds a shadow instead of a blob of light.
        /// </summary>
        [Tooltip("When using the 'Rotor' light type, this changes the scale of the blob of light or shadow in the center. A negative number adds a shadow instead of a blob of light.")]
        [Range(-1f, 1f)]
        public float lightRotorCenter = 0.1f;

        /// <summary>
        /// When using the 'Disco' light type, this specifies how fast the lights move vertically
        /// around the light source. This number can be negative to reverse the effect.
        /// </summary>
        [Tooltip("When using the 'Disco' light type, this specifies how fast the lights move vertically around the light source. This number can be negative to reverse the effect.")]
        public float lightDiscoVerticalSpeed = 1.0f;

        /// <summary>
        /// When using the 'Spot' light type, this texture can be used as a grayscale shadow cookie
        /// that will be projected within the radius of the spot light. Animated render textures are supported.
        /// </summary>
        [Tooltip("When using the 'Spot' light type, this texture can be used as a grayscale shadow cookie that will be projected within the radius of the spot light. Animated render textures are supported.")]
        public Texture lightCookieTexture;

        /// <summary>
        /// The color of the bounce lighting. Use the alpha component to blend between the current
        /// light color and this color. Bounce lighting is grayscale by design, and separating it
        /// from the main light color allows for greater creative control, to help the usually dark
        /// bounce lighting better match the environment.
        /// </summary>
        [Header("Bounce Lighting:")]
        [Tooltip("The color of the bounce lighting. Use the alpha component to blend between the current light color and this color. Bounce lighting is grayscale by design, but separating it from the main light color allows for greater creative control, to help the usually dark bounce lighting better match the environment.")]
        [ColorUsage(showAlpha: true)]
        public Color lightBounceColor = new Color(1.0f, 1.0f, 1.0f, 0.0f);

        /// <summary>
        /// Adjusts the visibility of bounce lighting by modifying its transparency. At 0, the
        /// bounce lighting will be fully transparent, while at 1, it will be fully visible. This
        /// setting does not affect the raytracing or compression process like <see
        /// cref="lightBounceIntensity"/> does, as it only changes the final rendered transparency
        /// of the bounce light.
        /// </summary>
        [Tooltip("Adjusts the visibility of bounce lighting by modifying its transparency. At 0, the bounce lighting will be fully transparent, while at 1, it will be fully visible. This setting does not affect the raytracing or compression process like 'Light Bounce Intensity' does, as it only changes the final rendered transparency of the bounce light.")]
        [Range(0f, 1f)]
        public float lightBounceModifier = 1.0f;

        /// <summary>
        /// The intensity of the bounce lighting when using 'Single Bounce' illumination. This is
        /// useful for brightening up areas where the bounce lighting appears too dim.
        /// <para>
        /// Note that any adjustments to this setting require raytracing the scene again for the
        /// changes to take effect.
        /// </para>
        /// </summary>
        [Header("Raytracing Settings:")]
        [Tooltip("The intensity of the bounce lighting when using 'Single Bounce' illumination. This is useful for brightening up areas where the bounce lighting appears too dim.\n\nNote that any adjustments to this setting require raytracing the scene again for the changes to take effect.")]
        [Min(0f)]
        public float lightBounceIntensity = 1f;

        /// <summary>
        /// The number of light samples taken from the scene when using 'Single Bounce'
        /// illumination. Increasing the number of samples helps reduce graininess or noise,
        /// resulting in smoother and more accurate lighting. However, higher sample counts may
        /// increase rendering times. A sample count of 128 delivers high quality results, providing
        /// smooth and accurate lighting with minimal graininess.
        /// <para>
        /// Note that any adjustments to this setting require raytracing the scene again for the
        /// changes to take effect.
        /// </para>
        /// </summary>
        [Tooltip("The number of light samples taken from the scene when using 'Single Bounce' illumination. Increasing the number of samples helps reduce graininess or noise, resulting in smoother and more accurate lighting. However, higher sample counts may increase rendering times. A sample count of 128 delivers high quality results, providing smooth and accurate lighting with minimal graininess.\n\nNote that any adjustments to this setting require raytracing the scene again for the changes to take effect.")]
        [Min(2f)]
        public int lightBounceSamples = 32;

        /// <summary>
        /// The compression level for bounce lighting data. Choosing a higher compression can reduce
        /// VRAM usage, but may result in reduced visual quality. For best results, adjust based on
        /// your VRAM availability and visual preferences.
        /// <para>
        /// Note that any adjustments to this setting require raytracing the scene again for the
        /// changes to take effect.
        /// </para>
        /// </summary>
        [Tooltip("The compression level for bounce lighting data. Choosing a higher compression can reduce VRAM usage, but may result in reduced visual quality. For best results, adjust based on your VRAM availability and visual preferences.\n\nNote that any adjustments to this setting require raytracing the scene again for the changes to take effect.")]
        public DynamicBounceLightingCompressionMode lightBounceCompression = DynamicBounceLightingCompressionMode.Inherit;

        /// <summary>
        /// The shimmer effects overlay the world with random blocks that project water caustics or
        /// fire wavering.
        /// </summary>
        [Header("Shimmering:")]
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

        /// <summary>The light intensity effect applied to this dynamic light.</summary>
        [Header("Intensity Effects:")]
        [Tooltip("The light intensity effect applied to this dynamic light.")]
        public DynamicLightEffect lightEffect = DynamicLightEffect.Steady;

        /// <summary>
        /// When using the 'Pulse', 'Pulsar' or 'Generator' light effect, this specifies how many
        /// times per second the light should pulse (as a multiplier), where 1 means once per
        /// second; or controls the speed of the animation.
        /// </summary>
        [Tooltip("When using the 'Pulse', 'Pulsar' or 'Generator' light effect, this specifies how many times per second the light should pulse (as a multiplier), where 1 means once per second; or controls the speed of the animation.")]
        public float lightEffectPulseSpeed = 1.0f;

        /// <summary>
        /// When using the 'Pulse', 'Pulsar', 'Fire', 'Flicker', 'Generator', 'Random', 'Strobe',
        /// 'Candle', 'FluorescentStarter' or 'FluorescentRandom' light effect, this specifies how
        /// dim the light can become per pulse, where 0 is completely off and 1 does nothing.
        /// </summary>
        [Tooltip("When using the 'Pulse', 'Pulsar', 'Fire', 'Flicker', 'Generator', 'Random', 'Strobe', 'Candle', 'FluorescentStarter' or 'FluorescentRandom' light effect, this specifies how dim the light can become per pulse, where 0 is completely off and 1 does nothing.")]
        [Range(0f, 1f)]
        public float lightEffectPulseModifier = 0.25f;

        /// <summary>
        /// When using the 'Pulse', 'Pulsar', 'Candle', 'Fire', 'Generator', 'FluorescentStarter' or
        /// 'FluorescentClicker' light effect, this changes the time offset of the pulses. This
        /// prevents lights from pulsing in sync and can also be used to programatically control the
        /// pulse animation.
        /// </summary>
        [Tooltip("When using the 'Pulse', 'Pulsar', 'Candle', 'Fire', 'Generator', 'FluorescentStarter' or 'FluorescentClicker' light effect, this changes the time offset of the pulses. This prevents lights from pulsing in sync and can also be used to programatically control the pulse animation.")]
        [Range(0f, 1f)]
        public float lightEffectPulseOffset = 0f;

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

        /// <summary>
        /// The different volumetric types that can be applied to a dynamic light. This requires the
        /// post processing script to be attached to the camera.
        /// </summary>
        [Header("Post Processing:")]
        [Tooltip("The different volumetric types that can be applied to a dynamic light. This requires the post processing script to be attached to the camera.")]
        public DynamicLightVolumetricType lightVolumetricType = DynamicLightVolumetricType.None;

        /// <summary>
        /// The spherical radius that the volumetric light will occupy. The volumetric fog cannot
        /// exceed this radius and is guaranteed to be completely gone when it reaches the end.
        /// </summary>
        [Tooltip("The spherical radius that the volumetric light will occupy. The volumetric fog cannot exceed this radius and is guaranteed to be completely gone when it reaches the end.")]
        [Min(0f)]
        public float lightVolumetricRadius = 4.0f;

        /// <summary>
        /// The volumetric fog thickness makes it increasingly more difficult to see through the
        /// fog. It appears as a solid color.
        /// </summary>
        [Tooltip("The volumetric fog thickness makes it increasingly more difficult to see through the fog. It appears as a solid color.")]
        [Min(1f)]
        public float lightVolumetricThickness = 1.0f;

        /// <summary>
        /// The volumetric fog intensity modifier changes the transparency of the final fog where at
        /// 0 it's completely transparent and 1 it's fully visible.
        /// </summary>
        [Tooltip("The volumetric fog intensity modifier changes the transparency of the final fog where at 0 it's completely transparent and 1 it's fully visible.")]
        [Range(0f, 1f)]
        public float lightVolumetricIntensity = 0.75f;

        /// <summary>
        /// The visibility in meters within the volumetric fog, is the measure of the distance at
        /// which an object can still be clearly discerned. This allows you to see nearby objects
        /// relatively clearly whilst surrounded by thick fog.
        /// </summary>
        [Tooltip("The visibility in meters within the volumetric fog, is the measure of the distance at which an object can still be clearly discerned. This allows you to see nearby objects relatively clearly whilst surrounded by thick fog.")]
        [Min(0f)]
        public float lightVolumetricVisibility = 2.0f;

        /// <summary>Gets whether this dynamic light is realtime (no shadows, channel 32).</summary>
        public bool realtime { get => lightChannel == 32; }

        /// <summary>
        /// Unity stores <see cref="Color"/> (when assigned in an inspector) in <see
        /// cref="ColorSpace.Gamma"/>, regardless of the project setting. This will return the
        /// current <see cref="lightColor"/> and convert <see cref="ColorSpace.Gamma"/> colors to
        /// <see cref="ColorSpace.Linear"/> when necessary.
        /// </summary>
        public unsafe Color lightColorAdjusted
        {
            get
            {
                Color result = lightColor;
                if (DynamicLightManager.colorSpace == ColorSpace.Linear)
                    UMath.GammaToLinearSpace((Vector3*)&result);
                return result;
            }
        }

        /// <summary>
        /// Unity stores <see cref="Color"/> (when assigned in an inspector) in <see
        /// cref="ColorSpace.Gamma"/>, regardless of the project setting. This will return the
        /// current <see cref="lightBounceColor"/> and convert <see cref="ColorSpace.Gamma"/> colors
        /// to <see cref="ColorSpace.Linear"/> when necessary.
        /// </summary>
        public unsafe Color lightBounceColorAdjusted
        {
            get
            {
                Color result = lightBounceColor;
                if (DynamicLightManager.colorSpace == ColorSpace.Linear)
                    UMath.GammaToLinearSpace((Vector3*)&result);
                return result;
            }
        }

        /// <summary>Stores dynamic light runtime effect values that change at irregular intervals.</summary>
        internal DynamicLightCache cache = new DynamicLightCache();

        /// <summary>The cached <see cref="Transform"/> of this <see cref="GameObject"/>.</summary>
        [System.NonSerialized]
        private Transform transformInstance = null;

        /// <summary>
        /// Gets the <see cref="Transform"/> attached to this <see cref="GameObject"/>.
        /// <para>The <see cref="Transform"/> is cached automatically and fast to access.</para>
        /// </summary>
        public new Transform transform
        {
            get
            {
                if (ReferenceEquals(transformInstance, null))
                    transformInstance = base.transform;
                return transformInstance;
            }
        }

        /// <summary>
        /// The largest spherical radius that the volumetric light will occupy <see
        /// cref="lightVolumetricRadius"/>. This value is internally used for culling off-camera lights.
        /// </summary>
        internal float currentVolumetricRadius
        {
            get
            {
                switch (lightVolumetricType)
                {
                    // calculate a radius that encompasses the box:
                    case DynamicLightVolumetricType.Box:
                        var size = lightVolumetricRadius * cache.transformScale;
                        return Mathf.Sqrt((size.x * size.x) + (size.y * size.y) + (size.z * size.z));

                    case DynamicLightVolumetricType.ConeZ:
                    case DynamicLightVolumetricType.ConeY:
                        {
                            // try to calculate a radius encompassing the cone:
                            float angle = Mathf.Clamp(lightOuterCutoff, 0f, 75f);
                            if (lightOuterCutoff > 90f)
                                angle = (1.0f - Mathf.InverseLerp(115f, 180f, lightOuterCutoff)) * 75f;

                            // calculate the maximal distance from the tip to the base edge.
                            return lightVolumetricRadius / Mathf.Cos(angle * Mathf.Deg2Rad);
                        }

                    default:
                        return lightVolumetricRadius;
                }
            }
        }

        /// <summary>
        /// The largest spherical radius that the light will occupy (either <see
        /// cref="lightRadius"/> or <see cref="lightVolumetricRadius"/>). This value is internally
        /// used for culling off-camera lights.
        /// </summary>
        internal float largestLightRadius
        {
            get
            {
                var volumetricRadius = currentVolumetricRadius;

                // always return the light radius if greater than the volumetric radius.
                if (lightRadius > volumetricRadius)
                    return lightRadius;

                // the volumetric radius is greater but must to be enabled to affect this light.
                if (lightVolumetricType != DynamicLightVolumetricType.None)
                    return volumetricRadius;

                return lightRadius;
            }
        }

        /// <summary>
        /// Contains the current intensity of the light source after the <see
        /// cref="DynamicLightManager"/> updates. This value changes based on the <see
        /// cref="lightEffect"/>. If the <see cref="DynamicLightManager"/> has not yet updated, it
        /// will reflect the intensity from the previous frame.
        /// </summary>
        public float currentIntensity => lightIntensity * cache.intensity;

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
            var transformPosition = cache.transformPosition;
            var lightColor = lightColorAdjusted;
            Color white;
            white.r = 1f;
            white.g = 1f;
            white.b = 1f;
            white.a = 1f;

            switch (lightType)
            {
                case DynamicLightType.Spot:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingSpotLight.png", true, lightColor);
                    break;

                case DynamicLightType.Disco:
                case DynamicLightType.Discoball:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingPointLightDisco.png", true, lightColor);
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingTypeDisco.png", true, white);
                    break;

                case DynamicLightType.Wave:
                case DynamicLightType.Interference:
                case DynamicLightType.Shock:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingPointLightWave.png", true, lightColor);
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingTypeWave.png", true, white);
                    break;

                case DynamicLightType.Rotor:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingPointLightRotor.png", true, lightColor);
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingTypeRotor.png", true, white);
                    break;

                default:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingPointLight.png", true, lightColor);
                    break;
            }

            if (realtime || cache.movedFromOrigin)
            {
                switch (lightType)
                {
                    case DynamicLightType.Spot:
                        Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingSpotLightRealtime.png", true, white);
                        break;

                    default:
                        Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingPointLightRealtime.png", true, white);
                        break;
                }
            }

            if (lightShadows == DynamicLightShadowMode.RealtimeShadows)
            {
                Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingRealtimeShadows.png", true, white);
            }

            if (lightIllumination == DynamicLightIlluminationMode.SingleBounce)
            {
                Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingBounce.png", true, white);
            }

            if (lightTransparency != DynamicLightTransparencyMode.Disabled)
            {
                if (lightTransparency == DynamicLightTransparencyMode.EnabledMax)
                {
                    Color orange;
                    orange.r = 1f;
                    orange.g = 0.5f;
                    orange.b = 0f;
                    orange.a = 1f;
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingTransparent.png", true, orange);
                }
                else
                {
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingTransparent.png", true, white);
                }
            }

            switch (lightShimmer)
            {
                case DynamicLightShimmer.Water:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingShimmerWater.png", true, white);
                    break;

                case DynamicLightShimmer.Random:
                    Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingShimmerFire.png", true, white);
                    break;
            }

            if (lightEffect != DynamicLightEffect.Steady)
            {
                Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingEffects.png", true, lightColor * currentIntensity);
            }

            if (lightVolumetricType != DynamicLightVolumetricType.None)
            {
                Gizmos.DrawIcon(transformPosition, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingVolumetric.png", true, white);
            }
        }

        private void OnDrawGizmosSelected()
        {
            var lightColor = lightColorAdjusted;

            switch (lightType)
            {
                case DynamicLightType.Spot:
                    var lightColorDarker25 = lightColor * 0.25f;
                    Gizmos.color = lightColorDarker25;
                    Gizmos.DrawWireSphere(cache.transformPosition, largestLightRadius);
                    if (lightOuterCutoff >= lightCutoff)
                    {
                        var lightColorDarker75 = lightColor * 0.75f;
                        Gizmos.color = lightColor;
                        GizmosEx.DrawWireSpot(cache.transformPosition, lightRadius, lightOuterCutoff, transform.forward, transform.up);
                        Gizmos.color = lightColorDarker75;
                        GizmosEx.DrawWireSpot(cache.transformPosition, lightRadius, lightCutoff, transform.forward, transform.up);
                    }
                    else
                    {
                        Gizmos.color = Color.red;
                        GizmosEx.DrawWireSpot(cache.transformPosition, lightRadius, lightOuterCutoff, transform.forward, transform.up);
                        Gizmos.color = lightColorDarker25;
                        GizmosEx.DrawWireSpot(cache.transformPosition, lightRadius, lightCutoff, transform.forward, transform.up);
                    }
                    if (lightOuterCutoff < 90f && lightCookieTexture)
                        GizmosDrawArrow(true, true, false);
                    break;

                case DynamicLightType.Discoball:
                    Gizmos.color = lightColor;
                    Gizmos.DrawWireSphere(cache.transformPosition, largestLightRadius);
                    GizmosDrawArrow(true, true, true);
                    break;

                case DynamicLightType.Interference:
                    Gizmos.color = lightColor;
                    Gizmos.DrawWireSphere(cache.transformPosition, largestLightRadius);
                    GizmosDrawArrow(false, false, true);
                    break;

                case DynamicLightType.Rotor:
                case DynamicLightType.Disco:
                    Gizmos.color = lightColor;
                    Gizmos.DrawWireSphere(cache.transformPosition, largestLightRadius);
                    GizmosDrawArrow(false, true, true);
                    break;

                default:
                    Gizmos.color = lightColor;
                    Gizmos.DrawWireSphere(cache.transformPosition, largestLightRadius);
                    break;
            }
        }

        /// <summary>
        /// Draws an arrow gizmo for dynamic light sources in the editor. It will try to hit the
        /// surface it's pointing at or if there's no obstacle it will point all the way to the
        /// light radius.
        /// </summary>
        /// <param name="forward">Whether this arrow points forwards or upwards.</param>
        /// <param name="twistable">
        /// Whether this arrow can be twisted (e.g. rotating a spotlight makes no difference so it's
        /// not twistable).
        /// </param>
        /// <param name="bidirectional">
        /// Whether the effect looks the same in both directions (e.g. a spotlight is one-directional).
        /// </param>
        private void GizmosDrawArrow(bool forward, bool twistable, bool bidirectional)
        {
            var raytraceLayers = DynamicLightManager.Instance.raytraceLayers;
            var root = cache.transformPosition;
            var trans = transform;
            var f = trans.forward;
            var u = trans.up;
            var r = trans.right;

            // when not facing forwards we swap the up and forward directions.
            if (!forward)
                (u, f) = (f, u);

            // trace the forwards direction.
            Vector3 head;
            if (Physics.Raycast(root, f, out var hit1, lightRadius, raytraceLayers, QueryTriggerInteraction.Ignore))
                head = hit1.point;
            else
                head = root + f * lightRadius;

            // trace the backwards direction.
            if (bidirectional)
            {
                if (Physics.Raycast(root, -f, out var hit2, lightRadius, raytraceLayers, QueryTriggerInteraction.Ignore))
                    root = hit2.point;
                else
                    root -= f * lightRadius;
            }

            // blue direction line.
            Color blue;
            blue.r = 0f;
            blue.g = 0f;
            blue.b = 1f;
            blue.a = 1f;
            Gizmos.color = blue;
            Gizmos.DrawLine(root, head);

            // only draw the orientation lines when the effect is twistable.
            if (!twistable) return;

            // green upwards line.
            {
                Color green;
                green.r = 0f;
                green.g = 1f;
                green.b = 0f;
                green.a = 1f;
                Gizmos.color = green;
                var a = (forward ? 1.0f : -1.0f) * 0.25f * u;
                Gizmos.DrawLine(head, head + a);
                if (bidirectional)
                    Gizmos.DrawLine(root, root + a);
            }

            // red arrow lines.
            {
                Color red;
                red.r = 1f;
                red.g = 0f;
                red.b = 0f;
                red.a = 1f;
                Gizmos.color = red;
                var a = (f - r) * 0.25f;
                var b = (f + r) * 0.25f;
                Gizmos.DrawLine(head, head - a);
                Gizmos.DrawLine(head, head - b);
                if (bidirectional)
                {
                    Gizmos.DrawLine(root, root + a);
                    Gizmos.DrawLine(root, root + b);
                }
            }
        }

#endif
    }
}