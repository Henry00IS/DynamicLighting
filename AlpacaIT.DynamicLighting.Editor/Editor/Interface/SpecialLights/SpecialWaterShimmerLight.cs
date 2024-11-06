namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialWaterShimmerLight : SpecialLight
    {
        public override string Name => "Water Shimmer Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightShimmer = DynamicLightShimmer.Water;
        }
    }
}