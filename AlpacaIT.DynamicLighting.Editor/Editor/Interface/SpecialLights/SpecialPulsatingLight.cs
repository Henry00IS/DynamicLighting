namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialPulsatingLight : SpecialLight
    {
        public override string Name => "Pulsating Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Pulse;
        }
    }
}