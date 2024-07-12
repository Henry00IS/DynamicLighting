#if UNITY_2021_2_OR_NEWER

using AlpacaIT.DynamicLighting.Internal;
using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor
{
    // shoutouts to Lindsey Keene for making these awesome windows and toolbars!

    /// <summary>The about window displays credits and the package version.</summary>
    public class AboutWindow : EditorWindow
    {
        /// <summary>Represents a small button with an icon such as PayPal that can be clicked.</summary>
        internal class LinkButton : EditorToolbarButton
        {
            /// <summary>Creates a new instance of <see cref="LinkButton"/>.</summary>
            /// <param name="icon">The icon texture to be displayed on the button.</param>
            /// <param name="tooltip">
            /// Text to display inside an information box after the user hovers over the button for a
            /// small amount of time.
            /// </param>
            /// <param name="clicked">The action to take when the user clicks on the button.</param>
            public LinkButton(Texture2D icon, string tooltip, Action clicked)
            {
                this.icon = icon;
                this.tooltip = tooltip;
                this.clicked += clicked;

                style.width = 32.0f;
                style.height = 32.0f;

                style.borderLeftWidth = 0.0f;
                style.borderRightWidth = 0.0f;

                style.justifyContent = Justify.Center;

                style.paddingTop = 4.0f;
                style.paddingBottom = 0.0f;
                style.marginLeft = 2.0f;
                style.marginRight = 2.0f;
            }
        }

        private const string paypalUrl = "https://paypal.me/henrydejongh";
        private const string kofiUrl = "https://ko-fi.com/henry00";
        private const string patreonUrl = "https://patreon.com/henrydejongh";
        private const string discordUrl = "https://discord.gg/sKEvrBwHtq";
        private const string githubUrl = "https://github.com/Henry00IS/DynamicLighting";

        private const float smallFont = 12.0f;
        private const float bigFont = 14.0f;
        private const float fontLeftPadding = 8.0f;
        private const float fontTopPadding = 1.0f;

        /// <summary>Displays the about window or selects it when already open.</summary>
        public static void Init()
        {
            AboutWindow window = GetWindow<AboutWindow>(true, "About");
            window.maxSize = new Vector2(350, 200) * Screen.dpi / 100.0f;
            window.minSize = window.maxSize;

            window.ShowUtility();
        }

        private void OnEnable()
        {
            var editorResources = DynamicLightingEditorResources.Instance;
            var rootElementWidth = rootVisualElement.layout.width;
            var isProSkin = EditorGUIUtility.isProSkin;
            var creatorLabelColor = new Color(isProSkin ? 0.5f : 0.3f, isProSkin ? 0.5f : 0.3f, isProSkin ? 0.5f : 0.3f, 1.0f);
            var neutralStyleColor = new Color(isProSkin ? 0.35f : 0.5f, isProSkin ? 0.35f : 0.5f, isProSkin ? 0.35f : 0.5f, 1.0f);
            var contributorStyleColor = new Color(isProSkin ? 0.6f : 0.5f, isProSkin ? 0.6f : 0.5f, isProSkin ? 0.6f : 0.5f, 1.0f);

            var titleBox = new Box();
            {
                titleBox.style.borderBottomColor = neutralStyleColor;
                titleBox.style.borderBottomWidth = 1.0f;
                titleBox.style.paddingBottom = 4.0f;

                var titleLabel = new Label("Dynamic Lighting");
                titleLabel.style.fontSize = bigFont + 4.0f;
                titleLabel.style.paddingLeft = 0.0f;
                titleLabel.style.paddingTop = 6.0f;
                titleLabel.style.width = rootElementWidth;
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                var creatorLabel = new Label("Created by: Henry de Jongh");
                creatorLabel.style.fontSize = bigFont;
                creatorLabel.style.paddingLeft = fontLeftPadding;
                creatorLabel.style.paddingTop = fontTopPadding;
                creatorLabel.style.width = rootElementWidth;
                creatorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                creatorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                creatorLabel.style.color = creatorLabelColor;

                var versionLabel = new Label($"v{Utilities.GetPackageVersion()}");
                versionLabel.style.fontSize = smallFont;
                versionLabel.style.paddingLeft = fontLeftPadding;
                versionLabel.style.paddingTop = fontTopPadding;
                versionLabel.style.width = rootElementWidth;
                versionLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                versionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                versionLabel.style.color = creatorLabelColor;

                titleBox.Add(titleLabel);
                titleBox.Add(creatorLabel);
                titleBox.Add(versionLabel);
            }

            var contributorGroup = new Box();
            {
                contributorGroup.style.flexGrow = 1.0f;

                var contributorHeaderLabel = new Label("Contributors");
                contributorHeaderLabel.style.fontSize = smallFont;
                contributorHeaderLabel.style.paddingLeft = fontLeftPadding;
                contributorHeaderLabel.style.paddingTop = fontTopPadding;
                contributorHeaderLabel.style.width = rootElementWidth;
                contributorHeaderLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                contributorHeaderLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                contributorHeaderLabel.style.color = contributorStyleColor;

                var contributorLabel = new Label("Lindsey Keene (nukeandbeans), Gawidev (Gawi)");
                contributorLabel.style.fontSize = smallFont;
                contributorLabel.style.paddingLeft = fontLeftPadding;
                contributorLabel.style.paddingTop = fontTopPadding;
                contributorLabel.style.width = rootElementWidth;
                contributorLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                contributorLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                contributorLabel.style.color = creatorLabelColor;

                contributorGroup.Add(contributorHeaderLabel);
                contributorGroup.Add(contributorLabel);
            }

            var linksBox = new VisualElement();
            {
                linksBox.style.flexDirection = FlexDirection.Row;
                linksBox.style.justifyContent = Justify.Center;

                var patreonIcon = isProSkin ? editorResources.patreonIconWhite : editorResources.patreonIcon;
                var githubIcon = isProSkin ? editorResources.gitHubIconWhite : editorResources.gitHubIcon;

                var paypalButton = new LinkButton(editorResources.paypalIcon, paypalUrl, () => Application.OpenURL(paypalUrl));
                var kofiButton = new LinkButton(editorResources.kofiIcon, kofiUrl, () => Application.OpenURL(kofiUrl));
                var patreonButton = new LinkButton(patreonIcon, patreonUrl, () => Application.OpenURL(patreonUrl));
                var discordButton = new LinkButton(editorResources.discordIcon, discordUrl, () => Application.OpenURL(discordUrl));
                var githubButton = new LinkButton(githubIcon, githubUrl, () => Application.OpenURL(githubUrl));

                linksBox.Add(paypalButton);
                linksBox.Add(kofiButton);
                linksBox.Add(patreonButton);
                linksBox.Add(discordButton);
                linksBox.Add(githubButton);
            }

            rootVisualElement.Add(titleBox);
            rootVisualElement.Add(contributorGroup);
            rootVisualElement.Add(linksBox);
        }
    }
}

#endif