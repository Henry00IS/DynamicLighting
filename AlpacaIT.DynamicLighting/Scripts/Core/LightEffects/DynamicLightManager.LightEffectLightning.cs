using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the lightning light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectLightning(DynamicLightCache lightCache, DynamicLight light)
        {
            var lightningTime = timeTime + light.lightEffectPulseOffset;
            var pulseModifier = light.lightEffectPulseModifier;

            // time makes it more difficult to dim towards dark.
            var slowWave = 0.5f + Mathf.Sin(lightningTime * Mathf.PI * 2f) * 0.5f;
            lightCache.intensity = Mathf.MoveTowards(lightCache.intensity, pulseModifier, deltaTime * 10f * slowWave);

            var idx = Mathf.RoundToInt(Mathf.Abs(light.lightEffectPulseSpeed));

            // compute up to three layers of lightning.
            ComputeLightEffectLightningLayer(lightCache, light, pulseModifier, lightningTime, 0.2f);
            if (idx >= 2)
                ComputeLightEffectLightningLayer(lightCache, light, pulseModifier, lightningTime + 121.81f, 4.5f);
            if (idx >= 3)
                ComputeLightEffectLightningLayer(lightCache, light, pulseModifier, lightningTime + 281.24f, 7.8f);
        }

        private void ComputeLightEffectLightningLayer(DynamicLightCache lightCache, DynamicLight light, float pulseModifier, float lightningTime, float unique)
        {
            var slowWave = 0.5f + Mathf.Sin((lightningTime + unique) * Mathf.PI * 2f) * 0.5f;
            var pulseModifierRemainder = (1.0f - pulseModifier) * slowWave;

            // cause a random flash at random intervals:
            if (Mathf.PerlinNoise(lightningTime * 5f, unique) > 0.74f)
                lightCache.intensity = pulseModifier + (pulseModifierRemainder + Mathf.Sin(lightningTime * Mathf.PI * 40f) * pulseModifierRemainder);
        }
    }
}