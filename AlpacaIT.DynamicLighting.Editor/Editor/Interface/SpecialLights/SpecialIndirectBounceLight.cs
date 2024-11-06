using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialIndirectBounceLight : SpecialLight
    {
        public override string Name => "Indirect Bounce Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightIllumination = DynamicLightIlluminationMode.SingleBounce;
            light.lightColor = Color.black;
            light.lightBounceColor = Color.white;
        }
    }
}