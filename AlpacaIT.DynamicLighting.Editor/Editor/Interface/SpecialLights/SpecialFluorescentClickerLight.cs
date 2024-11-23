namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFluorescentClickerLight : SpecialLight
    {
        public override string Name => "Fluorescent Clicker Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.FluorescentClicker;
        }
    }
}