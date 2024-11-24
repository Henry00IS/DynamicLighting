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
            light.lightColor = new Color(0.9490196f, 0.4901961f, 0.04705882f);
        }
    }
}