using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the generator light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectGenerator(DynamicLightCache lightCache, DynamicLight light)
        {
            var generatorTime = light.lightEffectPulseOffset + timeTime * light.lightEffectPulseSpeed;
            float flicker = Mathf.Clamp(-1f + Mathf.PerlinNoise(generatorTime, generatorTime * 0.1f) * 2.5f, -1f, 0f) * (1.0f - light.lightEffectPulseModifier);
            lightCache.intensity = 1.0f + flicker;
        }
    }
}