using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the pulsar light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectPulsar(DynamicLightCache lightCache, DynamicLight light)
        {
            var lightEffectPulseModifier = light.lightEffectPulseModifier;
            var pulsarTime = light.lightEffectPulseOffset + timeTime;

            // calculate the pulsar phase using a sine wave to create a rhythmic intensity burst.
            float phase = Mathf.Sin(pulsarTime * Mathf.PI * 2.0f * light.lightEffectPulseSpeed);
            phase = Mathf.Max(0f, phase); // clip negative values to 0 for no light during dim phases.

            // add a sharp burst for the peak intensity.
            float burst = Mathf.Pow(phase, 5f);

            // combine the base phase with the burst effect.
            var targetIntensity = Mathf.Lerp(
                lightEffectPulseModifier, // minimum intensity during a dip.
                1.0f, // maximum intensity.
                burst
            );

            // gradually move to the target intensity.
            lightCache.intensity = Mathf.MoveTowards(lightCache.intensity, targetIntensity, deltaTime * 2f); // Faster transitions for dynamic behavior.
        }
    }
}