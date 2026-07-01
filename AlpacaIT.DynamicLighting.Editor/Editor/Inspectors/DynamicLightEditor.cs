using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>Customizes the inspector for <see cref="DynamicLight"/> instances to display the light settings.</summary>
    [CustomEditor(typeof(DynamicLight))]
    [CanEditMultipleObjects]
    public sealed class DynamicLightEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var iterator = serializedObject.GetIterator();

            bool showLightSettingsSection = false;

            if (iterator.NextVisible(enterChildren: true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                        continue;

                    if (noLightTypeUsesCutoffSettings)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightOuterCutoff))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightCutoff))
                            continue;
                    }
                    else
                        showLightSettingsSection = true;

                    if (noLightTypeIsSpot)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightCookieTexture))
                            continue;
                    }
                    else
                        showLightSettingsSection = true;

                    if (noLightTypeUsesWaveSettings)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightWaveSpeed))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightWaveFrequency))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightWaveOffset))
                            continue;
                    }
                    else
                        showLightSettingsSection = true;

                    if (noLightTypeIsRotor)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightRotorCenter))
                            continue;
                    }
                    else
                        showLightSettingsSection = true;

                    if (noLightTypeIsDisco)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightDiscoVerticalSpeed))
                            continue;
                    }
                    else
                        showLightSettingsSection = true;

                    if (noLightIlluminationIsSingleBounce)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightBounceColor))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightBounceModifier))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightBounceIntensity))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightBounceSamples))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightBounceCompression))
                            continue;
                    }

                    if (noLightIsShimmering)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightShimmerScale))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightShimmerModifier))
                            continue;
                    }

                    if (noLightEffectUsesSpeed)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightEffectPulseSpeed))
                            continue;
                    }

                    if (noLightEffectUsesModifier)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightEffectPulseModifier))
                            continue;
                    }

                    if (noLightEffectUsesOffset)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightEffectPulseOffset))
                            continue;
                    }

                    if (noLightEffectUsesTimestepFrequency)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightEffectTimestepFrequency))
                            continue;
                    }

                    if (noLightUsesVolumetrics)
                    {
                        if (iterator.propertyPath == nameof(DynamicLight.lightVolumetricRadius))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightVolumetricThickness))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightVolumetricIntensity))
                            continue;

                        if (iterator.propertyPath == nameof(DynamicLight.lightVolumetricVisibility))
                            continue;
                    }

                    EditorGUILayout.PropertyField(iterator, new GUIContent(GetCleanDisplayName(iterator), iterator.tooltip), includeChildren: true);

                    if (showLightSettingsSection && iterator.propertyPath == nameof(DynamicLight.lightTransparency))
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField(new GUIContent("Light Settings:"), EditorStyles.boldLabel);
                    }
                } while (iterator.NextVisible(enterChildren: false));
            }

            serializedObject.ApplyModifiedProperties();
            return;
        }

        private string GetCleanDisplayName(SerializedProperty iterator)
        {
            if (iterator.propertyPath == nameof(DynamicLight.lightShimmerScale) || iterator.propertyPath == nameof(DynamicLight.lightShimmerModifier))
                return iterator.displayName.Replace("Light Shimmer ", "");

            if (
                iterator.propertyPath == nameof(DynamicLight.lightEffectPulseSpeed)
                || iterator.propertyPath == nameof(DynamicLight.lightEffectPulseModifier)
                || iterator.propertyPath == nameof(DynamicLight.lightEffectPulseOffset)
                || iterator.propertyPath == nameof(DynamicLight.lightEffectTimestepFrequency)
            )
                return iterator.displayName.Replace("Light Effect ", "");

            if (
                iterator.propertyPath == nameof(DynamicLight.lightVolumetricRadius)
                || iterator.propertyPath == nameof(DynamicLight.lightVolumetricThickness)
                || iterator.propertyPath == nameof(DynamicLight.lightVolumetricIntensity)
                || iterator.propertyPath == nameof(DynamicLight.lightVolumetricVisibility)
            )
                return iterator.displayName.Replace("Light Volumetric ", "Fog ");

            return iterator.displayName.Replace("Light ", "");
        }

        private bool AnyLightIs(Func<DynamicLight, bool> func) => serializedObject.targetObjects.Any(o => o is DynamicLight dynamicLight && func(dynamicLight));

        private bool NoLightIs(Func<DynamicLight, bool> func) => !AnyLightIs(func);

        private bool noLightTypeIsSpot => NoLightIs(l => l.lightType == DynamicLightType.Spot);
        private bool noLightTypeIsRotor => NoLightIs(l => l.lightType == DynamicLightType.Rotor);
        private bool noLightTypeIsDisco => NoLightIs(l => l.lightType == DynamicLightType.Disco);
        private bool noLightTypeUsesCutoffSettings => NoLightIs(l => l.lightType == DynamicLightType.Spot || l.lightType == DynamicLightType.Discoball);

        private bool noLightTypeUsesWaveSettings =>
            NoLightIs(l =>
                l.lightType == DynamicLightType.Wave
                || l.lightType == DynamicLightType.Interference
                || l.lightType == DynamicLightType.Disco
                || l.lightType == DynamicLightType.Rotor
                || l.lightType == DynamicLightType.Shock
            );

        private bool noLightIlluminationIsSingleBounce => NoLightIs(l => l.lightIllumination == DynamicLightIlluminationMode.SingleBounce);

        private bool noLightIsShimmering => NoLightIs(l => l.lightShimmer != DynamicLightShimmer.None);

        private bool noLightEffectUsesSpeed =>
            NoLightIs(l =>
                l.lightEffect == DynamicLightEffect.Cloudy
                || l.lightEffect == DynamicLightEffect.Generator
                || l.lightEffect == DynamicLightEffect.Lightning
                || l.lightEffect == DynamicLightEffect.Overcast
                || l.lightEffect == DynamicLightEffect.Pulsar
                || l.lightEffect == DynamicLightEffect.Pulse
            );

        private bool noLightEffectUsesModifier =>
            NoLightIs(l =>
                l.lightEffect == DynamicLightEffect.Candle
                || l.lightEffect == DynamicLightEffect.Cloudy
                || l.lightEffect == DynamicLightEffect.Fire
                || l.lightEffect == DynamicLightEffect.Flicker
                || l.lightEffect == DynamicLightEffect.FluorescentRandom
                || l.lightEffect == DynamicLightEffect.FluorescentStarter
                || l.lightEffect == DynamicLightEffect.Generator
                || l.lightEffect == DynamicLightEffect.Lightning
                || l.lightEffect == DynamicLightEffect.Overcast
                || l.lightEffect == DynamicLightEffect.Pulsar
                || l.lightEffect == DynamicLightEffect.Pulse
                || l.lightEffect == DynamicLightEffect.Random
                || l.lightEffect == DynamicLightEffect.Strobe
            );

        private bool noLightEffectUsesOffset =>
            NoLightIs(l =>
                l.lightEffect == DynamicLightEffect.Candle
                || l.lightEffect == DynamicLightEffect.Cloudy
                || l.lightEffect == DynamicLightEffect.Fire
                || l.lightEffect == DynamicLightEffect.FluorescentStarter
                || l.lightEffect == DynamicLightEffect.Generator
                || l.lightEffect == DynamicLightEffect.Lightning
                || l.lightEffect == DynamicLightEffect.Overcast
                || l.lightEffect == DynamicLightEffect.Pulsar
                || l.lightEffect == DynamicLightEffect.Pulse
            );

        private bool noLightEffectUsesTimestepFrequency =>
            NoLightIs(l => l.lightEffect == DynamicLightEffect.Flicker || l.lightEffect == DynamicLightEffect.Random || l.lightEffect == DynamicLightEffect.Strobe);

        private bool noLightUsesVolumetrics => NoLightIs(l => l.lightVolumetricType != DynamicLightVolumetricType.None);
    }
}
