using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFireLight : SpecialLight
    {
        public override string Name => "Fire Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Fire;
            light.lightRadius = 2f;
            light.lightEffectPulseModifier = 0f;
            light.lightColor = new Color(1f, 0.5405405f, 0f);
        }
    }
}