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
            var lightningTime = timeTime * 5f + light.lightEffectPulseOffset;
            lightCache.intensity = Mathf.MoveTowards(lightCache.intensity, light.lightEffectPulseModifier, deltaTime * 10f);

            // cause a random flash at random intervals:
            if (Mathf.PerlinNoise(lightningTime * light.lightEffectPulseSpeed, 4815.162342f) > 0.78f)
                lightCache.intensity = 1.0f;
        }
    }
}