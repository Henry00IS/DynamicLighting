using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different light intensity effects that can be applied to a dynamic light.</summary>
    public enum DynamicLightEffect
    {
        /// <summary>The light is stable and consistent without any effects (default)</summary>
        [Tooltip("The light is stable and consistent without any effects (default).")]
        Steady = 0,

        /// <summary>These entries are menu separators in Unity Editor and not intended for use.</summary>
        [InspectorName("")]
        ᐅ_1 = int.MaxValue,

        /// <summary>
        /// Simulates the gentle, natural flicker of a candle flame. The light intensity varies
        /// smoothly with subtle oscillations and occasional sharper dips, creating a realistic
        /// effect. Rare, dramatic flickers simulate the impact of a gust of air or a sudden shift
        /// in the flame.
        /// </summary>
        [Tooltip("Simulates the gentle, natural flicker of a candle flame. The light intensity varies smoothly with subtle oscillations and occasional sharper dips, creating a realistic effect. Rare, dramatic flickers simulate the impact of a gust of air or a sudden shift in the flame.")]
        Candle = 8,

        /// <summary>
        /// The light mimics the dynamic and lively behavior of a bonfire or torch, with smooth
        /// oscillations and chaotic variations. Periodic bursts of intensity simulate flames
        /// leaping higher.
        /// </summary>
        [Tooltip("The light mimics the dynamic and lively behavior of a bonfire or torch, with smooth oscillations and chaotic variations. Periodic bursts of intensity simulate flames leaping higher.")]
        Fire = 10,

        /// <summary>
        /// The light randomly picks a number between 0 and 1. If the number is smaller than 0.5 the
        /// light will be fully dark otherwise it multiplies the fully bright intensity with the
        /// number. It does this every update of the configurable framerate independent fixed
        /// timestep frequency. This is similar to random intensity except that the light turns off
        /// more often giving that sense that the light is either broken or electricity is sparking etc.
        /// </summary>
        [Tooltip("The light randomly picks a number between 0 and 1. If the number is smaller than 0.5 the light will be fully dark otherwise it multiplies the fully bright intensity with the number. It does this every update of the configurable framerate independent fixed timestep frequency. This is similar to random intensity except that the light turns off more often giving that sense that the light is either broken or electricity is sparking etc.")]
        [InspectorName("Flickering")]
        Flicker = 4,

        /// <summary>
        /// The light simulates the behavior of a faulty voltage controller on a generator, causing
        /// smooth but unpredictable dimming of the light. This effect is often seen in mine shafts
        /// or other underground settings.
        /// </summary>
        [Tooltip("The light simulates the behavior of a faulty voltage controller on a generator, causing smooth but unpredictable dimming of the light. This effect is often seen in mine shafts or other underground settings.")]
        Generator = 11,

        /// <summary>
        /// The light simulates sudden and unpredictable flashes of light, mimicking the behavior of
        /// natural lightning. The light intensity remains low during non-flash periods and randomly
        /// spikes up to full brightness. The <see cref="DynamicLight.lightEffectPulseSpeed"/> can
        /// be set to 1, 2 or 3 for up to 3 layers of thunder.
        /// </summary>
        [Tooltip("The light simulates sudden and unpredictable flashes of light, mimicking the behavior of natural lightning. The light intensity remains low during non-flash periods and randomly spikes up to full brightness. The 'Light Effect Pulse Speed' can be set to 1, 2 or 3 for up to 3 layers of thunder.")]
        Lightning = 12,

        /// <summary>
        /// The light pulses rhythmically, mimicking the behavior of an astronomical pulsar.
        /// Intensity smoothly cycles between dim phases and sharp, focused bursts of brightness.
        /// </summary>
        [Tooltip("The light pulses rhythmically, mimicking the behavior of an astronomical pulsar. Intensity smoothly cycles between dim phases and sharp, focused bursts of brightness.")]
        Pulsar = 9,

        /// <summary>
        /// The light smoothly pulses between fully bright and dark over time using a sine wave. How
        /// dim the light will become and how many times per second the light will pulse can be configured.
        /// </summary>
        [Tooltip("The light smoothly pulses between fully bright and dark over time. How dim the light will become and how many times per second the light will pulse can be configured.")]
        [InspectorName("Pulsating")]
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

        /// <summary>These entries are menu separators in Unity Editor and not intended for use.</summary>
        [InspectorName(" ")]
        ᐅ_2 = int.MaxValue,

        /// <summary>
        /// The fluorescent light flickers as it attempts to ignite. In the initial phase, the light
        /// exhibits a rapid, low-intensity flicker resembling the 50Hz electrical hum of a preheat
        /// ballast. After stabilizing briefly at full brightness, it suddenly dims and powers down
        /// in a rapid fade, only to restart the cycle. This effect captures the erratic and
        /// frustrating behavior of a failing fluorescent light.
        /// </summary>
        [Tooltip("The fluorescent light flickers as it attempts to ignite. In the initial phase, the light exhibits a rapid, low-intensity flicker resembling the 50Hz electrical hum of a preheat ballast. After stabilizing briefly at full brightness, it suddenly dims and powers down in a rapid fade, only to restart the cycle. This effect captures the erratic and frustrating behavior of a failing fluorescent light.")]
        FluorescentStarter = 5,

        /// <summary>
        /// The fluorescent light struggles to start, producing faint, rapid flickers before
        /// emitting sporadic, bright flashes interspersed with darkness. After stabilizing at full
        /// brightness for a short period, the light abruptly dims, mimicking the cycle of a glow
        /// switch starter. This effect captures the erratic and frustrating behavior of a failing
        /// fluorescent light.
        /// </summary>
        [Tooltip("The fluorescent light struggles to start, producing faint, rapid flickers before emitting sporadic, bright flashes interspersed with darkness. After stabilizing at full brightness for a short period, the light abruptly dims, mimicking the cycle of a glow switch starter. This effect captures the erratic and frustrating behavior of a failing fluorescent light.")]
        FluorescentClicker = 6,

        /// <summary>
        /// The fluorescent light exhibits chaotic and unpredictable behavior, randomly alternating
        /// between dimming, rapid flickers as it tries to ignite, and brief moments of full
        /// brightness. This effect captures the erratic and frustrating behavior of a failing
        /// fluorescent light.
        /// </summary>
        [Tooltip("The fluorescent light exhibits chaotic and unpredictable behavior, randomly alternating between dimming, rapid flickers as it tries to ignite, and brief moments of full brightness. This effect captures the erratic and frustrating behavior of a failing fluorescent light.")]
        FluorescentRandom = 7,

        /// <summary>These entries are menu separators in Unity Editor and not intended for use.</summary>
        [InspectorName("  ")]
        ᐅ_3 = int.MaxValue,

        /// <summary>
        /// Simulates the mostly unobscured sunlight of a partly cloudy day, with long periods of
        /// stable brightness interrupted by infrequent, transient dips in intensity. These brief
        /// reductions mimic the passage of sparse, high-altitude clouds, maintaining an overall
        /// sunny dominance while adding subtle natural variation. A nostalgic nod to childhood
        /// summers spent focusing sunlight through a magnifying glass to spark flames- only for
        /// that one pesky cloud to drift by and snuff out the fun just as things heat up.
        /// </summary>
        [Tooltip("Simulates the mostly unobscured sunlight of a partly cloudy day, with long periods of stable brightness interrupted by infrequent, transient dips in intensity. These brief reductions mimic the passage of sparse, high-altitude clouds, maintaining an overall sunny dominance while adding subtle natural variation. A nostalgic nod to childhood summers spent focusing sunlight through a magnifying glass to spark flames- only for that one pesky cloud to drift by and snuff out the fun just as things heat up.")]
        Cloudy = 13,

        /// <summary>
        /// Simulates the diffused and variable illumination of an overcast sky with multiple
        /// overlapping cloud layers. The light intensity fluctuates smoothly through cycles of
        /// dimming and relative brightening, driven by the complex motion of dense cloud
        /// formations, creating a consistently subdued yet dynamic atmospheric effect. A nostalgic
        /// reminder of those childhood afternoons when mom banished you outside to play instead of
        /// video games- only for the sky to thicken with clouds, darkening the day into a somber,
        /// endless gray that matched your reluctant mood perfectly.
        /// </summary>
        [Tooltip("Simulates the diffused and variable illumination of an overcast sky with multiple overlapping cloud layers. The light intensity fluctuates smoothly through cycles of dimming and relative brightening, driven by the complex motion of dense cloud formations, creating a consistently subdued yet dynamic atmospheric effect. A nostalgic reminder of those childhood afternoons when mom banished you outside to play instead of video games- only for the sky to thicken with clouds, darkening the day into a somber, endless gray that matched your reluctant mood perfectly.")]
        Overcast = 14,
    }
}