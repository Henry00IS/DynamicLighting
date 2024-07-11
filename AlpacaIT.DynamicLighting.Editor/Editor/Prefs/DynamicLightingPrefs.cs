using UnityEditor;

namespace AlpacaIT.DynamicLighting.Editor
{
    public static class DynamicLightingPrefs
    {
        private const string PREF_ROOT = "de.alpacait.dynamiclighting.editor.";

        private const string BOUNCE_LIGHTING_BY_DEFAULT_PREF = PREF_ROOT + "defaultBounceLighting";
        private const string BAKE_RESOLUTION_PREF            = PREF_ROOT + "bakeResolution";
        private const string SHOULD_SNAP_GRID_PREF           = PREF_ROOT + "snapToGridWhenPlaced";

        /// <summary>Do we default to creating all lights with bounce lighting?</summary>
        public static bool DefaultToBounceLighting
        {
            get => EditorPrefs.GetBool( BOUNCE_LIGHTING_BY_DEFAULT_PREF, false ); // don't turn this on by default.
            set => EditorPrefs.SetBool( BOUNCE_LIGHTING_BY_DEFAULT_PREF, value );
        }

        /// <summary>The default bake resolution we desire.</summary>
        public static int BakeResolution
        {
            get => EditorPrefs.GetInt( BAKE_RESOLUTION_PREF, 2048 ); // internal default setting
            set => EditorPrefs.SetInt( BAKE_RESOLUTION_PREF, value );
        }

        /// <summary>When placing a light, this will determine if it will snap to the grid.</summary>
        public static bool SnapToGridWhenPlaced
        {
            get => EditorPrefs.GetBool( SHOULD_SNAP_GRID_PREF, true );
            set => EditorPrefs.SetBool( SHOULD_SNAP_GRID_PREF, value );
        }
    }
}
