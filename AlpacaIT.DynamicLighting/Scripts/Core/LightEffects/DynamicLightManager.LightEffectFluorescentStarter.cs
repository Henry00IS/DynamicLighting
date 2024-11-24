using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the fluorescent starter light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectFluorescentStarter(DynamicLightCache lightCache, DynamicLight light)
        {
            var sequence = (light.lightEffectPulseOffset + timeTime) % 3.3f;
            if (sequence < 0.5f)
                // initial flicker as the light tries to ignite.
                lightCache.intensity = (0.25f + Mathf.Sin(sequence * Mathf.PI * 50f) * 0.125f) * (1.0f - light.lightEffectPulseModifier); // 50hz electricity on the pre-heat ballast.
            else if (sequence > 2.95f)
                // sudden fade as the light fails, restarting the cycle.
                lightCache.intensity = Mathf.Lerp(1.0f, 0.0f, (sequence - 3.0f) * 20f);
            else
                // light stabilizes at full brightness for a moment.
                lightCache.intensity = 1.0f;
        }
    }
}