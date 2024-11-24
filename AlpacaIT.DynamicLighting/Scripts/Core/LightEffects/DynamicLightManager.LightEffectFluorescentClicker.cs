using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the fluorescent clicker light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectFluorescentClicker(DynamicLightCache lightCache, DynamicLight light)
        {
            var sequenceTime = (light.lightEffectPulseOffset + timeTime) % 6.0f;
            if (sequenceTime < 0.3f)
                // initial faint flicker as the light tries to ignite.
                lightCache.intensity = (0.1f + Mathf.Sin(sequenceTime * Mathf.PI * 20f) * 0.05f);
            else if (sequenceTime < 1.5f)
                // sporadic flashes with brief pauses, mimicking the glow starter clicking.
                lightCache.intensity = (sequenceTime % 0.2f < 0.05f) ? 1.0f : 0.0f;
            else if (sequenceTime < 4.5f)
                // light stabilizes at full brightness for a moment.
                lightCache.intensity = 1.0f;
            else
                // sudden fade as the light fails, restarting the cycle.
                lightCache.intensity = Mathf.Lerp(1.0f, 0.0f, (sequenceTime - 4.5f) / 0.0625f);
        }
    }
}