#if UNITY_6000_7_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor
{
    public static class HierarchyImprovements
    {
        private static DynamicLightingEditorResources editorResources;

        private class DynamicLightingHierarchyEffectTransparency { }

        private class DynamicLightingHierarchyEffectBounce { }

        private class DynamicLightingHierarchyEffectRealtimeShadows { }

        private class DynamicLightingHierarchyEffectVolumetric { }

        private class DynamicLightingHierarchyEffectEffects { }

        private class DynamicLightingHierarchyEffectShimmer { }

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            editorResources = DynamicLightingEditorResources.Instance;

            HierarchyWindow.BindViewItem -= HierarchyWindow_BindViewItem;
            HierarchyWindow.BindViewItem += HierarchyWindow_BindViewItem;
        }

        private static void HierarchyWindow_BindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item)
        {
            var entityId = item.View.Source.GetEntityIdFromNode(item.Node);
            var obj = EditorUtility.EntityIdToObject(entityId);
            if (obj is not GameObject gameObject)
                return;
            if (!gameObject.TryGetComponent<DynamicLight>(out var light))
                return;

            // set the primary icon to a point or spot light.
            item.Icon.style.backgroundImage = light.lightType switch
            {
                DynamicLightType.Spot => (StyleBackground)editorResources.dynamicLightingSpotLightIcon,
                _ => (StyleBackground)editorResources.dynamicLightingPointLightIcon,
            };

            // set the icon color to the light color.
            item.Icon.style.unityBackgroundImageTintColor = light.lightColor;

            // display the light effect icons.
            UpdateRightIcons(item, light);
        }

        private static void UpdateRightIcons(HierarchyViewItem item, DynamicLight light)
        {
            var existing = new Dictionary<Type, VisualElement>();
            foreach (var child in item.RightCustomContainer.Children())
            {
                if (child.userData != null)
                    existing[child.userData.GetType()] = child;
            }

            // csharpier-ignore
            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectTransparency),
                light.lightTransparency != DynamicLightTransparencyMode.Disabled,
                editorResources.dynamicLightingTransparentIcon, 1f);

            // csharpier-ignore
            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectBounce),
                light.lightIllumination == DynamicLightIlluminationMode.SingleBounce,
                editorResources.dynamicLightingBounceIcon, 16f);

            // csharpier-ignore
            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectRealtimeShadows),
                light.lightShadows == DynamicLightShadowMode.RealtimeShadows,
                editorResources.dynamicLightingRealtimeShadowsIcon, 32f);

            // csharpier-ignore
            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectVolumetric),
                light.lightVolumetricType != DynamicLightVolumetricType.None,
                editorResources.dynamicLightingVolumetricIcon, 76f);

            // csharpier-ignore
            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectEffects),
                light.lightEffect != DynamicLightEffect.Steady,
                editorResources.dynamicLightingEffectsIcon, 92f);

            bool wantShimmer = light.lightShimmer != DynamicLightShimmer.None;
            var shimmerIcon = wantShimmer ? (light.lightShimmer == DynamicLightShimmer.Water ? editorResources.dynamicLightingShimmerWaterIcon : editorResources.dynamicLightingShimmerFireIcon) : null;

            ProcessIcon(item, existing, typeof(DynamicLightingHierarchyEffectShimmer), wantShimmer, shimmerIcon, 108f);
        }

        private static void ProcessIcon(HierarchyViewItem item, Dictionary<Type, VisualElement> existing, Type markerType, bool want, Texture2D icon, float offset)
        {
            if (existing.TryGetValue(markerType, out var elem))
            {
                if (!want)
                {
                    item.RightCustomContainer.Remove(elem);
                }
                else if (icon != null)
                {
                    elem.style.backgroundImage = icon;
                }
            }
            else if (want && icon != null)
            {
                item.RightCustomContainer.Add(BuildIcon(Activator.CreateInstance(markerType), icon, offset));
            }
        }

        private static VisualElement BuildIcon(object userData, Texture2D icon, float offset)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList("hierarchy-item__icon");
            element.style.backgroundImage = icon;
            element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, -offset);
            element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Bottom);
            element.style.backgroundSize = new BackgroundSize(124f, 124f);
            element.userData = userData;
            return element;
        }
    }
}
#endif
