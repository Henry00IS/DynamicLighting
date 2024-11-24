using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialCandleLight : SpecialLight
    {
        public override string Name => "Candle Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Candle;
            light.lightRadius = 1f;
            light.lightColor = new Color(0.8509804f, 0.6f, 0.3568628f);
        }
    }
}