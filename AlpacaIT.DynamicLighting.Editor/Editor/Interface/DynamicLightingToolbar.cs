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
            Add(new SpecialLightButton());
            Add(new SpecialLightsButton());

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

    internal class SpecialLightButton : EditorToolbarButton
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(SpecialLightButton);

        public SpecialLightButton()
        {
            name = ID;
            icon = EditorGUIUtility.TrIconContent("DirectionalLight Gizmo").image as Texture2D;
            text = "Special";
            tooltip = "Creates the last-used special light in the scene.";

            clicked += () =>
            {
                SpecialLight.Create(SpecialLight.LastSpecialLight);
            };
        }
    }

    #endregion Basic Light Types

    #region Special Lights

    internal class SpecialLightsButton : EditorToolbarDropdown
    {
        public const string ID = nameof(DynamicLightingToolbar) + "_" + nameof(SpecialLightsButton);

        private readonly GenericMenu menu = new GenericMenu();

        public SpecialLightsButton()
        {
            CreateMenu(menu);

            name = ID;
            tooltip = "Special light types...";

            style.minWidth = 14f;

            clicked += () =>
            {
                menu.ShowAsContext();
            };
        }

        private void CreateMenu(GenericMenu genericMenu)
        {
            addItem(SpecialLight.SpecialDirectionalLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialIndirectBounceLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialDiscoballLight);
            addItem(SpecialLight.SpecialWaveLight);
            addItem(SpecialLight.SpecialInterferenceLight);
            addItem(SpecialLight.SpecialRotorLight);
            addItem(SpecialLight.SpecialShockLight);
            addItem(SpecialLight.SpecialDiscoLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialPulsatingLight);
            addItem(SpecialLight.SpecialPulsarLight);
            addItem(SpecialLight.SpecialRandomLight);
            addItem(SpecialLight.SpecialStrobeLight);
            addItem(SpecialLight.SpecialFlickeringLight);
            addItem(SpecialLight.SpecialCandleLight);
            addItem(SpecialLight.SpecialFireLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialFluorescentStarterLight);
            addItem(SpecialLight.SpecialFluorescentClickerLight);
            addItem(SpecialLight.SpecialFluorescentRandomLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialWaterShimmerLight);
            addItem(SpecialLight.SpecialFireShimmerLight);

            genericMenu.AddSeparator("");

            addItem(SpecialLight.SpecialRotaryWarningLight);

            return; // local methods

            void addItem(SpecialLight specialLight) => genericMenu.AddItem(new GUIContent(specialLight.Name), false, () => SpecialLight.Create(specialLight));
        }
    }

    #endregion Special Lights
}

#endif