namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFireShimmerLight : SpecialLight
    {
        public override string Name => "Fire Shimmer Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightShimmer = DynamicLightShimmer.Random;
        }
    }
}