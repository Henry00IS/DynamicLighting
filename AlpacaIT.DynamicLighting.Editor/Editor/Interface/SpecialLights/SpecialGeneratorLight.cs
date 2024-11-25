namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialGeneratorLight : SpecialLight
    {
        public override string Name => "Generator Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Generator;
        }
    }
}