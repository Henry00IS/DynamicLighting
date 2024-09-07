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
        private SerializedObject settingsObject;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (DynamicLightManager)serializedObject.targetObject;
            var settings = manager.GetSettingsOrDefaults();

            if (settingsObject == null || settingsObject.targetObject != settings)
            {
                settingsObject?.Dispose();
                settingsObject = new SerializedObject(settings);
            }

            EditorGUI.BeginChangeCheck();
            settingsObject.UpdateIfRequiredOrScript();
            var iterator = settingsObject.GetIterator();

            using (new EditorGUI.DisabledScope(settings == DynamicLightingSettings.defaultSettings))
            {
                if (iterator.NextVisible(enterChildren: true))
                {
                    do
                    {
                        if (iterator.propertyPath == "m_Script")
                            continue;

                        EditorGUILayout.PropertyField(iterator, includeChildren: true);
                    } while (iterator.NextVisible(enterChildren: false));
                }
            }

            settingsObject.ApplyModifiedProperties();
            EditorGUI.EndChangeCheck();

            if (settings == DynamicLightingSettings.defaultSettings)
            {
                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("New Lighting Settings"), GUILayout.Width(170)))
                {
                    var scene = SceneManager.GetActiveScene();

                    if (scene.IsSavedToDisk())
                    {
                        settings = Instantiate(settings);
                        Directory.CreateDirectory(Path.Combine(Path.ChangeExtension(scene.path, null)));
                        AssetDatabase.CreateAsset(settings, Path.Combine(Path.ChangeExtension(scene.path, null), "DynamicLightingSettings.asset"));
                        manager.lightingSettings = settings;
                        EditorUtility.SetDirty(manager);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }

        private void OnDestroy()
        {
            settingsObject?.Dispose();
        }
    }
}
