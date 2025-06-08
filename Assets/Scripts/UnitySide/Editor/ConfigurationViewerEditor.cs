using UnityEditor;
using UnityEngine;

namespace UnitySide.Editor
{
    /// <summary>
    /// Editor window for viewing and managing configuration files
    /// </summary>
    public class ConfigurationViewerEditor : EditorWindow
    {
        private TextAsset selectedConfig;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Configuration Viewer")]
        public static void ShowWindow()
        {
            GetWindow<ConfigurationViewerEditor>("Configuration Viewer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Configuration Viewer", EditorStyles.boldLabel);
            
            EditorGUILayout.Space();
            
            // Config file field
            selectedConfig = (TextAsset)EditorGUILayout.ObjectField("Configuration File", 
                selectedConfig, typeof(TextAsset), false);
            
            if (selectedConfig != null)
            {
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Create New Config Based On This"))
                {
                    CreateNewConfig();
                }
                
                EditorGUILayout.Space();
                
                // Show the content of the config file
                GUILayout.Label("Configuration Content:", EditorStyles.boldLabel);
                
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
                    GUILayout.ExpandHeight(true));
                
                // Display the config content
                GUIStyle textStyle = new GUIStyle(EditorStyles.textArea);
                textStyle.wordWrap = true;
                EditorGUI.BeginDisabledGroup(true); // Make it read-only
                EditorGUILayout.TextArea(selectedConfig.text, textStyle, 
                    GUILayout.ExpandHeight(true));
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a configuration file to view its content.", 
                    MessageType.Info);
            }
        }
        
        private void CreateNewConfig()
        {
            if (selectedConfig == null)
                return;
            
            string defaultPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(selectedConfig));
            string defaultName = "new_config.json";
            
            string savePath = EditorUtility.SaveFilePanel(
                "Create New Configuration",
                defaultPath,
                defaultName,
                "json");
            
            if (string.IsNullOrEmpty(savePath))
                return;
            
            // Make the path relative to the Assets folder
            if (savePath.StartsWith(Application.dataPath))
            {
                savePath = "Assets" + savePath.Substring(Application.dataPath.Length);
            }
            
            // Write the content
            System.IO.File.WriteAllText(savePath, selectedConfig.text);
            
            AssetDatabase.Refresh();
            
            // Select the new asset
            TextAsset newAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(savePath);
            if (newAsset != null)
            {
                selectedConfig = newAsset;
                Selection.activeObject = newAsset;
                EditorGUIUtility.PingObject(newAsset);
            }
        }
    }
}