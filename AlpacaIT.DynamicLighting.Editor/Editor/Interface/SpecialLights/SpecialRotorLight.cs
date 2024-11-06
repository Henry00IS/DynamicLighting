namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialRotorLight : SpecialLight
    {
        public override string Name => "Rotor Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightType = DynamicLightType.Rotor;
            light.lightWaveFrequency = 5f;
        }
    }
}