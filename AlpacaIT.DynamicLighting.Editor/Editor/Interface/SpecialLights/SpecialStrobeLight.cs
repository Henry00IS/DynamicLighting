namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialStrobeLight : SpecialLight
    {
        public override string Name => "Strobe Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Strobe;
        }
    }
}