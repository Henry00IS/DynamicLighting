using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the overcast light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectOvercast(DynamicLightCache lightCache, DynamicLight light)
        {
            var generatorTime = light.lightEffectPulseOffset + timeTime * light.lightEffectPulseSpeed;
            var pulseModifier = light.lightEffectPulseModifier;

            // fractional Brownian motion with 3 octaves (layers of clouds).
            float noise = 0f;
            float freq = 1f;
            float amp = 1f;
            for (int octave = 0; octave < 3; octave++)
            {
                float t = generatorTime * freq;
                float perlin = Mathf.Clamp01(Mathf.PerlinNoise(t * 0.1f, t * 0.01f));
                noise += perlin * amp;
                freq *= 2f;
                amp *= 0.5f;
            }

            // normalize to [0, 1].
            float raw = noise / 1.75f;

            // apply smoothstep to organically stretch toward 0 and 1 (debias the central clustering).
            float stretched = raw * raw * (3f - 2f * raw);

            // remap to [modifier, 1.0] linearly so that it hits closer to bounds naturally.
            lightCache.intensity = Mathf.Lerp(pulseModifier, 1f, stretched);
        }
    }
}