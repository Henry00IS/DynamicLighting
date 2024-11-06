namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialInterferenceLight : SpecialLight
    {
        public override string Name => "Interference Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightType = DynamicLightType.Interference;
        }
    }
}