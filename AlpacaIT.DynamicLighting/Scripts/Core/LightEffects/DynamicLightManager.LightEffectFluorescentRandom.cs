using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the fluorescent random light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectFluorescentRandom(DynamicLightCache lightCache, DynamicLight light)
        {
            // pick a random state at random intervals:
            if (timeTime > lightCache.fluorescentRandomTime)
            {
                lightCache.fluorescentRandomState = Random.Range(0, 3);
                lightCache.fluorescentRandomTime = timeTime + Random.value;
            }

            // the current behaviour changes depending on the random state:
            switch (lightCache.fluorescentRandomState)
            {
                case 0:
                    // the light dims or fails.
                    lightCache.intensity = Mathf.MoveTowards(lightCache.intensity, light.lightEffectPulseModifier, deltaTime * 10f);
                    break;

                case 1:
                    // initial flicker as the light tries to ignite.
                    lightCache.intensity = (0.25f + Mathf.Sin(timeTime * Mathf.PI * 50f) * 0.125f) * (1.0f - light.lightEffectPulseModifier); // 50hz electricity on the pre-heat ballast.
                    break;

                case 2:
                    // light stabilizes at full brightness for a moment.
                    lightCache.intensity = Mathf.MoveTowards(lightCache.intensity, 1.0f, deltaTime * 20f);
                    break;
            }
        }
    }
}