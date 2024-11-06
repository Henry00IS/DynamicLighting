namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialDiscoballLight : SpecialLight
    {
        public override string Name => "Discoball Light";

        public override void Create()
        {
            var light = EditorMenus.EditorCreateDynamicDiscoballLight(null);
            ApplyCommonFlags(light);
        }
    }
}