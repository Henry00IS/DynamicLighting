namespace AlpacaIT.DynamicLighting.Editor
{
    internal abstract class SpecialLight
    {
        public static SpecialDirectionalLight SpecialDirectionalLight = new SpecialDirectionalLight();
        public static SpecialIndirectBounceLight SpecialIndirectBounceLight = new SpecialIndirectBounceLight();

        public static SpecialDiscoballLight SpecialDiscoballLight = new SpecialDiscoballLight();
        public static SpecialWaveLight SpecialWaveLight = new SpecialWaveLight();
        public static SpecialInterferenceLight SpecialInterferenceLight = new SpecialInterferenceLight();
        public static SpecialRotorLight SpecialRotorLight = new SpecialRotorLight();
        public static SpecialShockLight SpecialShockLight = new SpecialShockLight();
        public static SpecialDiscoLight SpecialDiscoLight = new SpecialDiscoLight();

        public static SpecialCandleLight SpecialCandleLight = new SpecialCandleLight();
        public static SpecialFireLight SpecialFireLight = new SpecialFireLight();
        public static SpecialFlickeringLight SpecialFlickeringLight = new SpecialFlickeringLight();
        public static SpecialGeneratorLight SpecialGeneratorLight = new SpecialGeneratorLight();
        public static SpecialLightningLight SpecialLightningLight = new SpecialLightningLight();
        public static SpecialPulsarLight SpecialPulsarLight = new SpecialPulsarLight();
        public static SpecialPulsatingLight SpecialPulsatingLight = new SpecialPulsatingLight();
        public static SpecialRandomLight SpecialRandomLight = new SpecialRandomLight();
        public static SpecialStrobeLight SpecialStrobeLight = new SpecialStrobeLight();

        public static SpecialFluorescentStarterLight SpecialFluorescentStarterLight = new SpecialFluorescentStarterLight();
        public static SpecialFluorescentClickerLight SpecialFluorescentClickerLight = new SpecialFluorescentClickerLight();
        public static SpecialFluorescentRandomLight SpecialFluorescentRandomLight = new SpecialFluorescentRandomLight();

        public static SpecialWaterShimmerLight SpecialWaterShimmerLight = new SpecialWaterShimmerLight();
        public static SpecialFireShimmerLight SpecialFireShimmerLight = new SpecialFireShimmerLight();

        public static SpecialCloudyLight SpecialCloudyLight = new SpecialCloudyLight();
        public static SpecialOvercastLight SpecialOvercastLight = new SpecialOvercastLight();

        public static SpecialRotaryWarningLight SpecialRotaryWarningLight = new SpecialRotaryWarningLight();

        public static SpecialLight LastSpecialLight = new SpecialDirectionalLight();

        public abstract string Name { get; }

        public abstract void Create();

        public static void Create(SpecialLight instance)
        {
            LastSpecialLight = instance;
            instance.Create();
        }

        protected void ApplyCommonFlags(DynamicLight light)
        {
            light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
            light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
        }

        protected DynamicLight CreateDynamicLight(bool siblings = true, bool rotateByNormal = false, bool spotlight = false)
        {
            var light = EditorMenus.EditorCreateDynamicLight("Dynamic " + Name, siblings, rotateByNormal, spotlight);
            light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
            light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
            return light;
        }
    }
}