using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the candle light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectCandle(DynamicLightCache lightCache, DynamicLight light)
        {
            var lightEffectPulseModifier = light.lightEffectPulseModifier;
            var flickerTime = light.lightEffectPulseOffset + timeTime;

            // smooth oscillation using perlin noise for randomness.
            float baseFlicker = Mathf.PerlinNoise(flickerTime * 0.5f, 0f); // slowly varying baseline flicker.
            float randomDip = Mathf.PerlinNoise(flickerTime * 2.0f, 10f); // faster smaller-scale variations.

            // scale and combine base flicker and random dips to create the final intensity.
            var targetIntensity = Mathf.Lerp(
                lightEffectPulseModifier, // minimum intensity during a dip.
                1.0f, // maximum intensity.
                baseFlicker - (randomDip > 0.8f ? (randomDip - 0.8f) * 2.0f : 0f) // occasional sharp dips.
            );

            // add rare sharp drops for dramatic effect.
            if (Mathf.PerlinNoise(flickerTime * 5f, 20f) > 0.7f)
                targetIntensity *= lightEffectPulseModifier; // rare deep dip to simulate a gust of air or sudden flicker.

            // gradually move to the target intensity.
            lightCache.intensity = Mathf.Clamp(lightEffectPulseModifier, Mathf.MoveTowards(lightCache.intensity, targetIntensity, deltaTime), 1f);
        }
    }
}