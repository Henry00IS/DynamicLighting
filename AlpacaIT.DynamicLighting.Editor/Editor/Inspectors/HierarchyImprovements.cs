#if UNITY_6000_7_OR_NEWER
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Visualizes extra icons in the <see cref="HierarchyWindow"/>.</summary>
    public static class HierarchyImprovements
    {
        private static DynamicLightingEditorResources editorResources;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            editorResources = DynamicLightingEditorResources.Instance;

            HierarchyWindow.BindViewItem -= HierarchyWindow_BindViewItem;
            HierarchyWindow.BindViewItem += HierarchyWindow_BindViewItem;

            HierarchyWindow.UnbindViewItem -= HierarchyWindow_UnbindViewItem;
            HierarchyWindow.UnbindViewItem += HierarchyWindow_UnbindViewItem;
        }

        /// <summary>
        /// Called when the <see cref="HierarchyWindow"/> creates or recycles a <see cref="HierarchyViewItem"/>.
        /// </summary>
        private static void HierarchyWindow_BindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            var entityId = item.View.Source.GetEntityIdFromNode(item.Node);
            var obj = EditorUtility.EntityIdToObject(entityId);
            if (obj is not GameObject gameObject)
                return;

            if (gameObject.TryGetComponent<DynamicLight>(out var light))
            {
                // set the primary icon to a point or spot light.
                var icon = item.Icon;
                icon.style.backgroundImage = light.lightType switch
                {
                    DynamicLightType.Spot => (StyleBackground)editorResources.dynamicLightingSpotLightIcon,
                    _ => (StyleBackground)editorResources.dynamicLightingPointLightIcon,
                };

                // set the icon color to the light color.
                icon.style.unityBackgroundImageTintColor = light.lightColor;

                // set the light effect icons.
                item.RightCustomContainer.Add(new DynamicLightingHierarchyEffectRow(light));
            }
        }

        /// <summary>
        /// Called when the <see cref="HierarchyWindow"/> removes or is about to recycle the
        /// <paramref name="item"/>.
        /// </summary>
        private static void HierarchyWindow_UnbindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            // remove the dynamic lighting effect row and if there was one:
            if (item.RightCustomContainer.RemoveByType<DynamicLightingHierarchyEffectRow>())
            {
                // restore the icon color to clean up after ourselves.
                item.Icon.style.unityBackgroundImageTintColor = Color.white;
            }
        }

        /// <summary>Small icon using scaled and clipped textures to recycle gizmo textures.</summary>
        private class DynamicLightingHierarchyEffectIcon : VisualElement
        {
            public DynamicLightingHierarchyEffectIcon(Texture2D icon, float offset)
            {
                style.width = 16f;
                style.height = 16f;
                style.backgroundImage = icon;
                style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, -offset);
                style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Bottom);
                style.backgroundSize = new BackgroundSize(124f, 124f);
            }
        }

        /// <summary>
        /// Horizontal row containing icons that represent certain <see cref="DynamicLight"/> effects.
        /// </summary>
        private class DynamicLightingHierarchyEffectRow : VisualElement
        {
            public DynamicLightingHierarchyEffectRow(DynamicLight light)
            {
                style.flexDirection = FlexDirection.Row;

                // transparency:
                if (light.lightTransparency != DynamicLightTransparencyMode.Disabled)
                {
                    var iconTransparency = new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingTransparentIcon, 0f);
                    if (light.lightTransparency == DynamicLightTransparencyMode.EnabledMax)
                    {
                        Color orange;
                        orange.r = 1f;
                        orange.g = 0.5f;
                        orange.b = 0f;
                        orange.a = 1f;
                        iconTransparency.style.unityBackgroundImageTintColor = orange;
                    }
                    Add(iconTransparency);
                }

                // single bounce:
                if (light.lightIllumination == DynamicLightIlluminationMode.SingleBounce)
                {
                    var iconBounce = new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingBounceIcon, 16f);
                    Color bounce = light.lightBounceColor;
                    bounce.a = 1f;
                    iconBounce.style.unityBackgroundImageTintColor = bounce;
                    Add(iconBounce);
                }

                // realtime shadows:
                if (light.lightShadows == DynamicLightShadowMode.RealtimeShadows)
                    Add(new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingRealtimeShadowsIcon, 32f));

                // volumetric:
                if (light.lightVolumetricType != DynamicLightVolumetricType.None)
                    Add(new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingVolumetricIcon, 76.5f));

                // intensity effects:
                if (light.lightEffect != DynamicLightEffect.Steady)
                    Add(new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingEffectsIcon, 92.75f));

                // shimmering:
                if (light.lightShimmer != DynamicLightShimmer.None)
                {
                    var iconShimmer = new DynamicLightingHierarchyEffectIcon(editorResources.dynamicLightingShimmerFireIcon, 108.5f);
                    iconShimmer.style.backgroundImage =
                        light.lightShimmer == DynamicLightShimmer.Water ? editorResources.dynamicLightingShimmerWaterIcon : editorResources.dynamicLightingShimmerFireIcon;
                    Add(iconShimmer);
                }
            }
        }
    }
}
#endif
