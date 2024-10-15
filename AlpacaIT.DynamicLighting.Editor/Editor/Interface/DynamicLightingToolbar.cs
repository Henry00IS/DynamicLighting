#if UNITY_2021_2_OR_NEWER

using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor
{
    [Overlay(typeof(SceneView), "Dynamic Lighting")]
    internal class DynamicLightingToolbar : ToolbarOverlay
    {
        public static DynamicLightingToolbar instance;

        public DynamicLightingToolbar() : base(LightGroupElement.ID, BakeButtonGroup.ID)
        {
        }

        public override void OnCreated()
        {
            instance = this;
        }
    }

    #region Bake Buttons

    [EditorToolbarElement(ID, typeof(SceneView))]
    internal class BakeButtonGroup : VisualElement
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeButtonGroup);

        public BakeButtonGroup()
        {
            name = ID;

            Add(new PreviewBakeButton());
            Add(new BakeButton());
            Add(new BakeOptionsButton());

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
        }
    }

    internal class PreviewBakeButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeButton);

        public PreviewBakeButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("d_Profiler.LastFrame").image as Texture2D;
            text = "Preview Bake";
            tooltip = "Performs a preview bake of the scene without bounce lighting.";

            clicked += () =>
            {
                DynamicLightManager.Instance.Raytrace(DynamicLightingPreferences.BakeResolution, DynamicLightingTracerFlags.SkipAll);
            };
        }
    }

    internal class BakeButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeButton);

        public BakeButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("Refresh@2x").image as Texture2D;
            text = "Bake";
            tooltip = "Performs a full bake of the scene lighting.";

            clicked += () =>
            {
                DynamicLightManager.Instance.Raytrace(DynamicLightingPreferences.BakeResolution);
            };
        }
    }

    internal class BakeOptionsButton : EditorToolbarDropdown
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(BakeOptionsButton);

        private GenericMenu menu;

        public BakeOptionsButton()
        {
            name = ID;
            tooltip = "Scene bake options.";

            style.minWidth = 14f;

            clicked += () =>
            {
                menu = new GenericMenu();
                CreateMenu();
                menu.ShowAsContext();
            };
        }

        private void CreateMenu()
        {
            addItem("Bake Resolution: Unlimited", setUnlimited, isResolution(23170));
            menu.AddSeparator("");
            addItem("Bake Resolution: 512", set512, isResolution(512));
            addItem("Bake Resolution: 1024", set1024, isResolution(1024));
            addItem("Bake Resolution: 2048", set2048, isResolution(2048));
            addItem("Bake Resolution: 4096", set4096, isResolution(4096));

            menu.AddSeparator("");

            addItem(
                "Delete Baked Lightmaps", () =>
                {
                    typeof(DynamicLightManager).GetMethod("EditorDeleteLightmapsNow", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null);
                }
            );

            menu.AddSeparator("");

            addItem(
                "New Lights Use Bounce Pass", () =>
                {
                    DynamicLightingPreferences.DefaultToBounceLighting = !DynamicLightingPreferences.DefaultToBounceLighting;
                }, DynamicLightingPreferences.DefaultToBounceLighting
            );

            addItem(
                "New Lights Use Transparency", () =>
                {
                    DynamicLightingPreferences.DefaultToTransparency = !DynamicLightingPreferences.DefaultToTransparency;
                }, DynamicLightingPreferences.DefaultToTransparency
            );

            menu.AddSeparator("");

            addItem(
                "About...", () =>
                {
                    AboutWindow.Init();
                }
            );

            return; // local methods

            void addItem(string label, GenericMenu.MenuFunction func, bool enabled = false)
            {
                menu.AddItem(new GUIContent(label), enabled, func);
            }

            void setResolution(int res)
            {
                DynamicLightingPreferences.BakeResolution = res;
            }

            bool isResolution(int res) => DynamicLightingPreferences.BakeResolution == res;

            void set512() => setResolution(512);
            void set1024() => setResolution(1024);
            void set2048() => setResolution(2048);
            void set4096() => setResolution(4096);

            void setUnlimited() => setResolution(23170);
        }
    }

    #endregion Bake Buttons

    #region Basic Light Types

    [EditorToolbarElement(ID, typeof(SceneView))]
    internal class LightGroupElement : VisualElement
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(LightGroupElement);

        public LightGroupElement()
        {
            Add(new PointLightButton());
            Add(new SpotLightButton());
            Add(new DirectionalLightButton());
            Add(new OtherLightsButton());

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);
        }
    }

    internal class PointLightButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(PointLightButton);

        public PointLightButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("PointLight Gizmo").image as Texture2D;
            text = "Point";
            tooltip = "Creates a new point light in the scene.";

            clicked += () =>
            {
                var light = EditorMenus.EditorCreateDynamicPointLight(null);
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
            };
        }
    }

    internal class SpotLightButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(SpotLightButton);

        public SpotLightButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("SpotLight Gizmo").image as Texture2D;
            text = "Spot";
            tooltip = "Creates a new spot light in the scene.";

            clicked += () =>
            {
                var light = EditorMenus.EditorCreateDynamicSpotLight(null);
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
            };
        }
    }

    internal class DirectionalLightButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(DirectionalLightButton);

        public DirectionalLightButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("DirectionalLight Gizmo").image as Texture2D;
            text = "Directional";
            tooltip = "Creates a new directional light in the scene.";

            clicked += () =>
            {
                var light = EditorMenus.EditorCreateDynamicDirectionalLight(null);
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
            };
        }
    }

    #endregion Basic Light Types

    #region Other Lights

    internal class OtherLightsButton : EditorToolbarDropdown
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(OtherLightsButton);

        private readonly GenericMenu menu = new GenericMenu();

        public OtherLightsButton()
        {
            CreateMenu(menu);

            name = ID;
            tooltip = "Other light types.";

            style.minWidth = 14f;

            clicked += () =>
            {
                menu.ShowAsContext();
            };
        }

        private void CreateMenu(GenericMenu genericMenu)
        {
            addItem("Discoball Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicDiscoballLight(null);
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
            });

            addItem("Wave Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Wave Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightType = DynamicLightType.Wave;
            });

            addItem("Interference Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Interference Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightType = DynamicLightType.Wave;
            });

            addItem("Rotor Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Rotor Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightType = DynamicLightType.Rotor;
                light.lightWaveFrequency = 5f;
            });

            addItem("Shock Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Shock Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightType = DynamicLightType.Shock;
            });

            addItem("Disco Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Disco Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightType = DynamicLightType.Disco;
                light.lightWaveFrequency = 10f;
            });

            genericMenu.AddSeparator("");

            addItem("Pulsating Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Pulsating Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightEffect = DynamicLightEffect.Pulse;
            });

            addItem("Random Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Random Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightEffect = DynamicLightEffect.Random;
            });

            addItem("Strobe Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Strobe Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightEffect = DynamicLightEffect.Strobe;
            });

            addItem("Flickering Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Flickering Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightEffect = DynamicLightEffect.Flicker;
            });

            genericMenu.AddSeparator("");

            addItem("Water Shimmer Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Water Shimmer Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightShimmer = DynamicLightShimmer.Water;
            });

            addItem("Fire Shimmer Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Dynamic Fire Shimmer Light");
                light.lightIllumination = DynamicLightingPreferences.DefaultToBounceLighting ? DynamicLightIlluminationMode.SingleBounce : DynamicLightIlluminationMode.DirectIllumination;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightShimmer = DynamicLightShimmer.Random;
            });

            genericMenu.AddSeparator("");

            addItem("Indirect Bounce Light", () =>
            {
                var light = EditorMenus.EditorCreateDynamicLight("Indirect Bounce Light");
                light.lightIllumination = DynamicLightIlluminationMode.SingleBounce;
                light.lightTransparency = DynamicLightingPreferences.DefaultToTransparency ? DynamicLightTransparencyMode.Enabled : DynamicLightTransparencyMode.Disabled;
                light.lightColor = Color.black;
                light.lightBounceColor = Color.white;
            });

            return; // local methods

            void addItem(string label, GenericMenu.MenuFunction func, bool enabled = false) => genericMenu.AddItem(new GUIContent(label), enabled, func);
        }
    }

    #endregion Other Lights
}

#endif