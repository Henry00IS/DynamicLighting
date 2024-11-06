namespace AlpacaIT.DynamicLighting.Editor
{
    internal class SpecialDirectionalLight : SpecialLight
    {
        public override string Name => "Directional Light";

        public override void Create()
        {
            var light = EditorMenus.EditorCreateDynamicDirectionalLight(null);
            ApplyCommonFlags(light);
        }
    }
}