namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialPulsarLight : SpecialLight
    {
        public override string Name => "Pulsar Light";

        public override void Create()
        {
            var light = CreateDynamicLight();
            light.lightEffect = DynamicLightEffect.Pulsar;
            light.lightEffectPulseSpeed = 0.5f;
            light.lightEffectPulseModifier = 0.0f;
        }
    }
}