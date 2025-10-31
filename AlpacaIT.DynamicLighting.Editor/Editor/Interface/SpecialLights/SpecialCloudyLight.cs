namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialCloudyLight : SpecialLight
    {
        public override string Name => "Cloudy Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Cloudy;
        }
    }
}