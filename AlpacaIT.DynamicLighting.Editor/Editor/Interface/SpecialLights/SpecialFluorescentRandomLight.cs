namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialFluorescentRandomLight : SpecialLight
    {
        public override string Name => "Fluorescent Random Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.FluorescentRandom;
            light.lightEffectPulseModifier = 0.0f;
        }
    }
}