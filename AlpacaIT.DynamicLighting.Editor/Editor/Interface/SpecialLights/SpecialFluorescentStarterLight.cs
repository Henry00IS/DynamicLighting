namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFluorescentStarterLight : SpecialLight
    {
        public override string Name => "Fluorescent Starter Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.FluorescentStarter;
        }
    }
}