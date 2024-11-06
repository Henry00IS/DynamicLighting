namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFlickeringLight : SpecialLight
    {
        public override string Name => "Flickering Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Flicker;
        }
    }
}