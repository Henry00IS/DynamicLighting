using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the fire light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectFire(DynamicLightCache lightCache, DynamicLight light)
        {
            var lightEffectPulseModifier = light.lightEffectPulseModifier;
            var flickerTime = light.lightEffectPulseOffset * 100f + timeTime;

            // smooth oscillation using perlin noise for randomness.
            float baseFlicker = Mathf.PerlinNoise(flickerTime * 0.5f, 0f); // slowly varying baseline flicker.
            float chaoticFlicker = Mathf.PerlinNoise(flickerTime * 3.0f, 10f); // faster smaller-scale variations.

            // scale and combine base flicker and random dips to create the final intensity.
            var targetIntensity = Mathf.Lerp(
                lightEffectPulseModifier, // minimum intensity during a dip.
                1.0f, // maximum intensity.
                baseFlicker * 0.6f + chaoticFlicker * 0.4f
            );

            // add rare sharp drops for dramatic effect.
            if (Mathf.PerlinNoise(flickerTime * 5f, 20f) > 0.7f)
                targetIntensity = 1.0f; // rare spike to simulate a flame.

            // gradually move to the target intensity.
            lightCache.intensity = Mathf.Clamp(lightEffectPulseModifier, Mathf.MoveTowards(lightCache.intensity, targetIntensity, deltaTime * 3f), 1f);
        }
    }
}