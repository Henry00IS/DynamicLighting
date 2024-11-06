using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialRotaryWarningLight : SpecialLight
    {
        public override string Name => "Rotary Warning Light";

        public override void Create()
        {
            var light = CreateDynamicLight(rotateByNormal: true);
            light.lightType = DynamicLightType.Rotor;
            light.lightRadius = 1f;
            light.lightWaveFrequency = 2f;
            light.lightWaveSpeed = 2f;
            light.lightColor = new Color(1f, 0.5f, 0f);
            light.lightRotorCenter = -0.02f;
        }
    }
}