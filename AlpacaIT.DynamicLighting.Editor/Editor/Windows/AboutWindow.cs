// * * * * * * * * * * * * * * * * * * * * * *
//  Author:  Lindsey Keene (nukeandbeans)
//  Contact: Twitter @nukeandbeans, Discord @nukeandbeans
//
//  Description:
//      About window! Support! Do it <3!
//  * * * * * * * * * * * * * * * * * * * * * *

using System;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT.DynamicLighting.Editor {
    public class AboutWindow : EditorWindow {
        private class LinkButton : EditorToolbarButton {
            public LinkButton( Texture2D icon, string tooltip, Action clicked ) {
                this.icon    =  icon;
                this.tooltip =  tooltip;
                this.clicked += clicked;

                style.width  = 24.0f;
                style.height = 24.0f;

                style.borderLeftWidth  = 0.0f;
                style.borderRightWidth = 0.0f;

                style.justifyContent = Justify.Center;

                style.paddingTop    = 4.0f;
                style.paddingBottom = 0.0f;
                style.marginLeft    = 2.0f;
                style.marginRight   = 2.0f;
            }
        }

        public static void Init() {
            AboutWindow window = GetWindow<AboutWindow>( true, "About" );
            window.maxSize = new Vector2( 250, 150 );
            window.minSize = window.maxSize;

            window.ShowUtility();
        }

        private void OnEnable() {
            bool isProSkin = EditorGUIUtility.isProSkin;

            Box   titleBox     = new();
            Label titleLabel   = new( "Dynamic Lighting" );
            Label creatorLabel = new( "Created by: Henry de Jongh" );
            Label versionLabel = new( $"v{ToolbarUtilities.GetPackageVersion( "de.alpacait.dynamiclighting" )}" );

            Box   contributorGroup       = new();
            Label contributorHeaderLabel = new( "Contributors" );
            Label contributorLabel       = new( "Lindsey Keene (nukeandbeans), Gawidev (Gawi)" );

            VisualElement linksBox = new();

            const string paypalURL  = "https://paypal.me/henrydejongh";
            const string kofiURL    = "https://ko-fi.com/henry00";
            const string patreonURL = "https://patreon.com/henrydejongh";
            const string discordURL = "https://discord.gg/sKEvrBwHtq";
            const string githubURL  = "https://github.com/Henry00IS/DynamicLighting";

            Color creatorLabelColor     = new( isProSkin ? 0.5f : 0.3f, isProSkin ? 0.5f : 0.3f, isProSkin ? 0.5f : 0.3f, 1.0f );
            Color neutralStyleColor     = new( isProSkin ? 0.35f : 0.5f, isProSkin ? 0.35f : 0.5f, isProSkin ? 0.35f : 0.5f, 1.0f );
            Color contributorStyleColor = new( isProSkin ? 0.6f : 0.5f, isProSkin ? 0.6f : 0.5f, isProSkin ? 0.6f : 0.5f, 1.0f );

            const float smallFont       = 8.0f;
            const float bigFont         = 9.0f;
            const float fontLeftPadding = 8.0f;
            const float fontTopPadding  = 1.0f;

            const TextAnchor textAnchor    = TextAnchor.MiddleCenter;
            const FontStyle  boldFontStyle = FontStyle.Bold;

            float rootElementWidth = rootVisualElement.layout.width;

            DynamicLightingEditorResources editorResources = DynamicLightingEditorResources.Instance;

            LinkButton paypalButton = new(
                editorResources.paypalIcon, paypalURL, () => {
                    Application.OpenURL( paypalURL );
                }
            );

            LinkButton kofiButton = new(
                editorResources.kofiIcon, kofiURL, () => {
                    Application.OpenURL( kofiURL );
                }
            );

            Texture2D patreonIcon = isProSkin ? editorResources.patreonIconWhite : editorResources.patreonIcon;

            LinkButton patreonButton = new(
                patreonIcon, patreonURL, () => {
                    Application.OpenURL( patreonURL );
                }
            );

            LinkButton discordButton = new(
                DynamicLightingEditorResources.Instance.discordIcon, discordURL, () => {
                    Application.OpenURL( discordURL );
                }
            );

            Texture2D githubIcon = isProSkin ? editorResources.gitHubIconWhite : editorResources.gitHubIcon;

            LinkButton githubButton = new(
                githubIcon, githubURL, () => {
                    Application.OpenURL( githubURL );
                }
            );

            titleBox.style.height            = 50.0f;
            titleBox.style.borderBottomColor = neutralStyleColor;
            titleBox.style.borderBottomWidth = 1.0f;

            titleLabel.style.paddingLeft             = smallFont;
            titleLabel.style.paddingTop              = 6.0f;
            titleLabel.style.width                   = rootElementWidth;
            titleLabel.style.unityTextAlign          = textAnchor;
            titleLabel.style.unityFontStyleAndWeight = boldFontStyle;

            versionLabel.style.fontSize                = smallFont;
            versionLabel.style.paddingLeft             = fontLeftPadding;
            versionLabel.style.paddingTop              = fontTopPadding;
            versionLabel.style.width                   = rootElementWidth;
            versionLabel.style.unityTextAlign          = textAnchor;
            versionLabel.style.unityFontStyleAndWeight = boldFontStyle;
            versionLabel.style.color                   = creatorLabelColor;

            creatorLabel.style.fontSize                = bigFont;
            creatorLabel.style.paddingLeft             = fontLeftPadding;
            creatorLabel.style.paddingTop              = fontTopPadding;
            creatorLabel.style.width                   = rootElementWidth;
            creatorLabel.style.unityTextAlign          = textAnchor;
            creatorLabel.style.unityFontStyleAndWeight = boldFontStyle;
            creatorLabel.style.color                   = creatorLabelColor;

            contributorGroup.style.flexGrow = 1.0f;

            contributorHeaderLabel.style.fontSize                = smallFont;
            contributorHeaderLabel.style.paddingLeft             = fontLeftPadding;
            contributorHeaderLabel.style.paddingTop              = fontTopPadding;
            contributorHeaderLabel.style.width                   = rootElementWidth;
            contributorHeaderLabel.style.unityTextAlign          = textAnchor;
            contributorHeaderLabel.style.unityFontStyleAndWeight = boldFontStyle;
            contributorHeaderLabel.style.color                   = contributorStyleColor;

            contributorLabel.style.fontSize                = smallFont;
            contributorLabel.style.paddingLeft             = fontLeftPadding;
            contributorLabel.style.paddingTop              = fontTopPadding;
            contributorLabel.style.width                   = rootElementWidth;
            contributorLabel.style.unityTextAlign          = textAnchor;
            contributorLabel.style.unityFontStyleAndWeight = boldFontStyle;
            contributorLabel.style.color                   = creatorLabelColor;


            titleBox.Add( titleLabel );
            titleBox.Add( creatorLabel );
            titleBox.Add( versionLabel );

            contributorGroup.Add( contributorHeaderLabel );
            contributorGroup.Add( contributorLabel );

            linksBox.style.flexDirection  = FlexDirection.Row;
            linksBox.style.justifyContent = Justify.Center;

            linksBox.Add( paypalButton );
            linksBox.Add( kofiButton );
            linksBox.Add( patreonButton );
            linksBox.Add( discordButton );
            linksBox.Add( githubButton );

            rootVisualElement.Add( titleBox );
            rootVisualElement.Add( contributorGroup );
            rootVisualElement.Add( linksBox );
        }
    }
}
