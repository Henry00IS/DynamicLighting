namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialDiscoLight : SpecialLight
    {
        public override string Name => "Disco Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightType = DynamicLightType.Disco;
            light.lightWaveFrequency = 10f;
        }
    }
}