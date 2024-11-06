namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialWaveLight : SpecialLight
    {
        public override string Name => "Wave Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightType = DynamicLightType.Wave;
        }
    }
}