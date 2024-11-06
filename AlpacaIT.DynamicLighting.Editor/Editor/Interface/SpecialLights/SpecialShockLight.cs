namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialShockLight : SpecialLight
    {
        public override string Name => "Shock Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightType = DynamicLightType.Shock;
        }
    }
}