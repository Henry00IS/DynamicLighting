// * * * * * * * * * * * * * * * * * * * * * *
//  Author:  Lindsey Keene (nukeandbeans)
//  Contact: Twitter @nukeandbeans, Discord @nukeandbeans
//
//  Description:
//
//  * * * * * * * * * * * * * * * * * * * * * *

using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor {
    [Overlay( typeof(SceneView), "Dynamic Lighting" )]
    internal class DynamicLightingToolbar : ToolbarOverlay {
        private static readonly string[] elementIDs = {
            LightGroupElement.ID, BakeButtonGroup.ID
        };

        public DynamicLightingToolbar() : base( elementIDs ) {
        }
    }

#region Bake Buttons

    [EditorToolbarElement( ID, typeof(SceneView) )]
    internal class BakeButtonGroup : VisualElement {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeButtonGroup);

        public BakeButtonGroup() {
            name = ID;

            Add( new BakeButton() );
            Add( new BakeOptionsButton() );

            EditorToolbarUtility.SetupChildrenAsButtonStrip( this );
        }
    }

    internal class BakeButton : EditorToolbarButton, IAccessContainerWindow {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeButton);

        public BakeButton() {
            name    = ID;
            icon    = EditorGUIUtility.TrIconContent( "Refresh@2x" ).image as Texture2D;
            text    = "Bake";
            tooltip = "Bakes scene lighting.";

            clicked += () => {
                DynamicLightManager.Instance.Raytrace( DynamicLightingPrefs.BakeResolution );
            };
        }

        public EditorWindow containerWindow { get; set; }
    }

    internal class BakeOptionsButton : EditorToolbarDropdown {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeOptionsButton);

        private GenericMenu _Menu = new();

        public BakeOptionsButton() {
            name    = ID;
            tooltip = "Scene bake options.";

            style.minWidth = 14f;

            clicked += () => {
                _Menu ??= new GenericMenu();
                CreateMenu( _Menu );

                _Menu.ShowAsContext();
            };
        }

        private void CreateMenu( GenericMenu menu ) {
            addItem( "Bake Resolution: 512", set512, isResolution( 512 ) );
            addItem( "Bake Resolution: 1024", set1024, isResolution( 1024 ) );
            addItem( "Bake Resolution: 2048", set2048, isResolution( 2048 ) );
            addItem( "Bake Resolution: 4096", set4096, isResolution( 4096 ) );
            addItem( "Bake Resolution: Unlimited", setUnlimited, isResolution( 23170 ) );

            addSeparator();

            addItem(
                "Clear Lightmap Data", () => {
                    DynamicLightManager.Instance.EditorDeleteLightmaps();
                }
            );

            addSeparator();

            addItem(
                "New Lights Use Bounce Pass", () => {
                    DynamicLightingPrefs.DefaultToBounceLighting = !DynamicLightingPrefs.DefaultToBounceLighting;
                }, DynamicLightingPrefs.DefaultToBounceLighting
            );

            return; // local methods

            void addItem( string label, GenericMenu.MenuFunction func, bool enabled = false ) {
                menu.AddItem( new GUIContent( label ), enabled, func );
            }

            void addSeparator( string path = "" ) => menu.AddSeparator( path );

            void setResolution( int res ) {
                DynamicLightingPrefs.BakeResolution = res;
            }

            bool isResolution( int res ) => DynamicLightingPrefs.BakeResolution == res;

            void set512()  => setResolution( 512 );
            void set1024() => setResolution( 1024 );
            void set2048() => setResolution( 2048 );
            void set4096() => setResolution( 4096 );

            void setUnlimited() => setResolution( 23170 );
        }
    }

#endregion Bake Buttons

#region Basic Light Types

    [EditorToolbarElement( ID, typeof(SceneView) )]
    internal class LightGroupElement : VisualElement {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(LightGroupElement);

        public LightGroupElement() {
            Add( new PointLightButton() );
            Add( new SpotLightButton() );
            Add( new DirectionalLightButton() );
            Add( new OtherLightsButton() );

            EditorToolbarUtility.SetupChildrenAsButtonStrip( this );
        }
    }

    internal class PointLightButton : EditorToolbarButton {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(PointLightButton);

        public PointLightButton() {
            name    = ID;
            icon    = EditorGUIUtility.TrIconContent( "PointLight Gizmo" ).image as Texture2D;
            text    = "Point";
            tooltip = "Creates a new point light in the scene.";

            clicked += () => {
                GameObject go = new( "Point Light" );

                Undo.RegisterCreatedObjectUndo( go, "Create new point light" );

                DynamicLight light = go.AddComponent<DynamicLight>();
                light.lightType         = DynamicLightType.Point;
                light.lightIllumination = DynamicLightingPrefs.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;

                ToolbarUtilities.PlaceInScene( go.transform );
                ToolbarUtilities.Select( go );
            };
        }
    }

    internal class SpotLightButton : EditorToolbarButton {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(SpotLightButton);

        public SpotLightButton() {
            name    = ID;
            icon    = EditorGUIUtility.TrIconContent( "SpotLight Gizmo" ).image as Texture2D;
            text    = "Spot";
            tooltip = "Creates a new spot light in the scene.";

            clicked += () => {
                GameObject go = new( "Spot Light" );

                Undo.RegisterCreatedObjectUndo( go, "Create new point light" );

                DynamicLight light = go.AddComponent<DynamicLight>();
                light.lightType         = DynamicLightType.Spot;
                light.lightIllumination = DynamicLightingPrefs.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;

                ToolbarUtilities.PlaceInScene( go.transform );
                ToolbarUtilities.Select( go );
            };
        }
    }

    internal class DirectionalLightButton : EditorToolbarButton {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(DirectionalLightButton);

        public DirectionalLightButton() {
            name    = ID;
            icon    = EditorGUIUtility.TrIconContent( "DirectionalLight Gizmo" ).image as Texture2D;
            text    = "Directional";
            tooltip = "Creates a new directional light in the scene.";

            clicked += () => {
                GameObject rootObj = new( "Directional Light" );

                Undo.RegisterCreatedObjectUndo( rootObj, "Create new point light" );

                GameObject lightObj = new( "Light" );

                lightObj.transform.SetParent( rootObj.transform );
                lightObj.transform.localPosition = new Vector3( 0.0f, 0.0f, -2500.0f );

                rootObj.transform.localRotation = Quaternion.Euler( 50.0f, -30.0f, 0.0f );

                DynamicLight light = lightObj.AddComponent<DynamicLight>();
                light.lightType         = DynamicLightType.Point;
                light.lightIntensity    = 2.0f;
                light.lightRadius       = 10000.0f;
                light.lightIllumination = DynamicLightingPrefs.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;

                ToolbarUtilities.Select( rootObj );
            };
        }
    }

#endregion Basic Light Types

#region Other Lights

    internal class OtherLightsButton : EditorToolbarDropdown {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(OtherLightsButton);

        private readonly GenericMenu _Menu = new();

        public OtherLightsButton() {
            CreateMenu( _Menu );

            name    = ID;
            tooltip = "Other light types.";

            style.minWidth = 14f;

            clicked += () => {
                _Menu.ShowAsContext();
            };
        }

        private void CreateMenu( GenericMenu menu ) {
            addItem( "New Disco Ball Light", createDiscoBall );
            addItem( "New Flicker Light", createFlicker );
            addItem( "New Pulse Light", createPulse );
            addItem( "New Random Noise Light", createRand );
            addItem( "New Strobe Light", createStrobe );

            menu.AddSeparator( "" );

            addItem(
                "Placed Lights Snap To Grid", () => {
                    DynamicLightingPrefs.SnapToGridWhenPlaced = !DynamicLightingPrefs.SnapToGridWhenPlaced;
                }, DynamicLightingPrefs.SnapToGridWhenPlaced
            );

            return; // local methods

            void addItem( string label, GenericMenu.MenuFunction func, bool enabled = false ) => menu.AddItem( new GUIContent( label ), enabled, func );

            void createDiscoBall() => createLight( "Disco Ball Light", DynamicLightType.Discoball );
            void createFlicker()   => createLight( "Flicker Light", DynamicLightType.Point, DynamicLightEffect.Flicker );
            void createPulse()     => createLight( "Pulse Light", DynamicLightType.Point, DynamicLightEffect.Pulse );
            void createRand()      => createLight( "Random Noise Light", DynamicLightType.Point, DynamicLightEffect.Random );
            void createStrobe()    => createLight( "Strobe Light", DynamicLightType.Point, DynamicLightEffect.Strobe );

            void createLight( string lightName, DynamicLightType lightType = DynamicLightType.Point, DynamicLightEffect lightEffect = DynamicLightEffect.Steady ) {
                GameObject go = new( lightName );

                Undo.RegisterCreatedObjectUndo( go, $"Create new {lightType} light (effect {lightEffect})." );

                DynamicLight light = go.AddComponent<DynamicLight>();

                light.lightType         = lightType;
                light.lightEffect       = lightEffect;
                light.lightIllumination = DynamicLightingPrefs.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;

                if( DynamicLightingPrefs.SnapToGridWhenPlaced ) {
                    ToolbarUtilities.PlaceInScene( go.transform );
                }

                ToolbarUtilities.Select( go );
            }
        }
    }

#endregion Other Lights
}
