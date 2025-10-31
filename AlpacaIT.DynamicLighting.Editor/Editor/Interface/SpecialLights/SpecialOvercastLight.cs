namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialOvercastLight : SpecialLight
    {
        public override string Name => "Overcast Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Overcast;
            light.lightEffectPulseModifier = 0.1f;
        }
    }
}