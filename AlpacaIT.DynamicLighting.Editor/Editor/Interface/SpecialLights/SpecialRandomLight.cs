namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialRandomLight : SpecialLight
    {
        public override string Name => "Random Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Random;
        }
    }
}