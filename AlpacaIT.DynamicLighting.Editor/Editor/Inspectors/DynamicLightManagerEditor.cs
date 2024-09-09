using AlpacaIT.DynamicLighting.Internal;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlpacaIT.DynamicLighting.Editor
{
    using Editor = UnityEditor.Editor;

    /// <summary>Customizes the inspector for <see cref="DynamicLightManager"/> instances to display the lighting settings.</summary>
    [CustomEditor(typeof(DynamicLightManager))]
    public sealed class DynamicLightManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var manager = (DynamicLightManager)serializedObject.targetObject;
            bool hasSettingsTemplate = manager.settingsTemplate;

            EditorGUI.BeginChangeCheck();

            var iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(enterChildren: true))
            {
                do
                {
                    if (iterator.propertyPath == "m_Script")
                        continue;

                    bool disabled = hasSettingsTemplate;
                    bool isSettingsTemplate = iterator.propertyPath == nameof(DynamicLightManager.settingsTemplate);

                    if (isSettingsTemplate)
                        disabled = false;

                    using (new EditorGUI.DisabledScope(disabled))
                    {
                        EditorGUILayout.PropertyField(iterator, includeChildren: true);
                    }

                    if (isSettingsTemplate)
                    {
                        if (hasSettingsTemplate)
                        {
                            GUIStyle TextFieldStyles = new GUIStyle(EditorStyles.label);
                            TextFieldStyles.normal.textColor = Color.white;
                            TextFieldStyles.alignment = TextAnchor.MiddleCenter;
                            EditorGUILayout.LabelField("Please edit the settings template directly.", TextFieldStyles);
                        }
                        EditorGUILayout.Space();
                    }
                } while (iterator.NextVisible(enterChildren: false));
            }

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                if (manager.settingsTemplate)
                {
                    manager.settingsTemplate.Apply();
                }
            }

            if (!hasSettingsTemplate)
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.IsSavedToDisk())
                {
                    EditorGUILayout.Space();
                    GUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(EditorGUIUtility.TrTextContent("New Settings Template"), GUILayout.Width(150)))
                    {
                        // create a settings template containing the current values.
                        var settings = CreateInstance<DynamicLightingSettings>();
                        settings.ImportFromScene();

                        // assign it to the current dynamic light manager and write it to disk.
                        var scenePath = Utilities.CreateScenePath(scene);
                        if (scenePath != null)
                        {
                            var assetPath = AssetDatabase.GenerateUniqueAssetPath(scenePath + Path.DirectorySeparatorChar + "DynamicLightingSettings.asset");
                            AssetDatabase.CreateAsset(settings, assetPath);
                            manager.settingsTemplate = settings;
                            settings.Apply();
                            EditorUtility.SetDirty(manager);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }
        }
    }
}