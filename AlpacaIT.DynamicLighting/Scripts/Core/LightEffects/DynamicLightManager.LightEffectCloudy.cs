using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the cloudy light effect logic.

    public partial class DynamicLightManager
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ComputeLightEffectCloudy(DynamicLightCache lightCache, DynamicLight light)
        {
            var generatorTime = light.lightEffectPulseOffset + timeTime * light.lightEffectPulseSpeed;
            var pulseModifier = light.lightEffectPulseModifier;

            // fractional brownian motion with 2 octaves for long, sparse cloud cycles.
            float noise = 0f;
            float freq = 1f;
            float amp = 1f;
            for (int octave = 0; octave < 2; octave++)
            {
                float t = generatorTime * freq;
                float perlin = Mathf.PerlinNoise(t * 0.05f, t * 0.005f);
                // simplified (levels) luminance adjustment (0.5f, 0.75f, 0.75f): thresholds low noise to 0, ramps [0.5,0.75]->[0,1], caps at 1.
                float adjusted = Mathf.Clamp01((perlin - 0.5f) * 4f);
                noise += adjusted * amp;
                freq *= 2f;
                amp *= 0.5f;
            }

            // normalize to [0, 1].
            float raw = noise / 1.5f;

            // inverse smoothstep: bias toward 1.0 for sunny dominance (flips clustering to highs).
            float invRaw = 1f - raw;
            float stretched = invRaw * invRaw * (3f - 2f * invRaw);  // sunny factor [0,1].

            // remap to [modifier (e.g., 0.6 shadow floor), 1.0]: bright sunlight until a cloud hits.
            lightCache.intensity = Mathf.Lerp(pulseModifier, 1f, stretched);
        }
    }
}