using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialLightningLight : SpecialLight
    {
        public override string Name => "Lightning Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Lightning;
            light.lightIntensity = 4f;
            light.lightColor = new Color(0.886f, 0.424f, 1.0f);
            light.lightEffectPulseModifier = 0f;
            light.lightEffectPulseSpeed = 3f;
        }
    }
}