using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the pulse light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectPulse(DynamicLightCache lightCache, DynamicLight light)
        {
            lightCache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, (1f + Mathf.Sin((light.lightEffectPulseOffset + timeTime * light.lightEffectPulseSpeed) * Mathf.PI * 2f)) * 0.5f);
        }
    }
}