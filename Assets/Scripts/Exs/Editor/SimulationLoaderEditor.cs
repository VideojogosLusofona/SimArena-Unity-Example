using UnityEngine;
using UnityEditor;
using Examples;
using Examples.Unity.Cosmetic;
using Examples.Unity.Managers;

namespace Examples.Editor
{
    [CustomEditor(typeof(SimulationLoader))]
    public class SimulationLoaderEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            SimulationLoader loader = (SimulationLoader)target;
            
            // Draw the default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Default Tilemap"))
            {
                CreateDefaultTilemap();
            }
            
            if (GUILayout.Button("Create Agent Prefab"))
            {
                CreateAgentPrefab();
            }
            
            if (GUILayout.Button("Create UI Elements \n(you will still need to position them)"))
            {
                CreateUIElements();
            }
        }
        
        private void CreateDefaultTilemap()
        {
            // Check if Grid already exists
            GameObject grid = GameObject.Find("Grid");
            if (grid == null)
            {
                grid = new GameObject("Grid");
                grid.AddComponent<Grid>();
            }
            
            if (GameObject.Find("Tilemap") != null)
            {
                Debug.LogWarning("Tilemap already exists.");
                return;
            }
            
            // Create floor tilemap
            GameObject floorTilemap = new GameObject("Tilemap");
            floorTilemap.transform.SetParent(grid.transform);
            floorTilemap.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            floorTilemap.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            
            // Set up the SimulationLoader references
            MapManager map = ((GameObject)target).GetComponent<MapManager>();
            SerializedObject so = new SerializedObject(map);
            so.FindProperty("tilemap").objectReferenceValue = floorTilemap.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            so.ApplyModifiedProperties();
            
            Debug.Log("Created default tilemap.");
        }
        
        private void CreateAgentPrefab()
        {
            // Create agent game object
            GameObject agentObject = new GameObject("Agent");
            
            // Add sprite renderer
            SpriteRenderer spriteRenderer = agentObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            spriteRenderer.color = Color.blue;
            
            // Add entity visualizer
            EntityVisualizer visualizer = agentObject.AddComponent<EntityVisualizer>();
            
            // Create UI canvas for the agent
            GameObject canvas = new GameObject("Canvas");
            canvas.transform.SetParent(agentObject.transform);
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasComponent.renderMode = RenderMode.WorldSpace;
            canvasComponent.transform.localPosition = new Vector3(0, 0.5f, 0);
            canvasComponent.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Create name text
            GameObject nameTextObj = new GameObject("NameText");
            nameTextObj.transform.SetParent(canvas.transform);
            TMPro.TextMeshProUGUI nameText = nameTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            nameText.text = "Agent";
            nameText.alignment = TMPro.TextAlignmentOptions.Center;
            nameText.fontSize = 24;
            nameText.rectTransform.sizeDelta = new Vector2(200, 50);
            nameText.rectTransform.anchoredPosition = new Vector2(0, 50);
            
            // Create health bar
            GameObject healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(canvas.transform);
            UnityEngine.UI.Slider healthBar = healthBarObj.AddComponent<UnityEngine.UI.Slider>();
            HealthBar healthBarScript = healthBarObj.AddComponent<HealthBar>();
            healthBar.minValue = 0;
            healthBar.maxValue = 1;
            healthBar.value = 1;
            healthBar.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 20);
            healthBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
            
            // Create health bar background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(healthBarObj.transform);
            UnityEngine.UI.Image background = backgroundObj.AddComponent<UnityEngine.UI.Image>();
            background.color = Color.gray;
            background.rectTransform.sizeDelta = new Vector2(160, 20);
            
            // Create health bar fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(healthBarObj.transform);
            UnityEngine.UI.Image fill = fillObj.AddComponent<UnityEngine.UI.Image>();
            fill.color = Color.green;
            fill.rectTransform.sizeDelta = new Vector2(160, 20);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            
            // Set up slider references
            healthBar.targetGraphic = background;
            healthBar.fillRect = fill.rectTransform;
            
            // Set up entity visualizer references
            SerializedObject so = new SerializedObject(visualizer);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("healthBar").objectReferenceValue = healthBarScript;
            so.ApplyModifiedProperties();
            
            SerializedObject hp = new SerializedObject(healthBarScript);
            hp.FindProperty("slider").objectReferenceValue = healthBar;
            hp.FindProperty("fill").objectReferenceValue = fill;
            hp.ApplyModifiedProperties();
            
            // Create prefab
            string prefabPath = "Assets/Prefabs/AgentPrefab.prefab";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(agentObject, prefabPath);
            
            // Destroy the temporary object
            DestroyImmediate(agentObject);
            
            // Set up the SimulationLoader reference
            SimulationLoader loader = (SimulationLoader)target;
            SerializedObject loaderSO = new SerializedObject(loader);
            loaderSO.FindProperty("agentPrefab").objectReferenceValue = prefab;
            loaderSO.ApplyModifiedProperties();
            
            Debug.Log($"Created agent prefab at {prefabPath}");
        }
        
        private void CreateUIElements()
        {
            // Create UI canvas
            GameObject canvas = new GameObject("SimulationUI");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            
            // Create kill feed container
            GameObject killFeedContainer = new GameObject("KillFeedContainer");
            killFeedContainer.transform.SetParent(canvas.transform);
            UnityEngine.UI.VerticalLayoutGroup layout = killFeedContainer.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            killFeedContainer.GetComponent<RectTransform>().anchorMin = new Vector2(0.7f, 0.7f);
            killFeedContainer.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            killFeedContainer.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Create kill feed item prefab
            GameObject killFeedItem = new GameObject("KillFeedItem");
            killFeedItem.AddComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
            TMPro.TextMeshProUGUI killFeedText = killFeedItem.AddComponent<TMPro.TextMeshProUGUI>();
            killFeedText.text = "Player1 killed Player2";
            killFeedText.alignment = TMPro.TextAlignmentOptions.Right;
            killFeedText.fontSize = 16;
            KillFeedItem killFeedScript = killFeedItem.AddComponent<KillFeedItem>();
            
            // Set up kill feed script references
            SerializedObject killFeedSO = new SerializedObject(killFeedScript);
            killFeedSO.FindProperty("killFeedText").objectReferenceValue = killFeedText;
            killFeedSO.ApplyModifiedProperties();
            
            // Create kill feed prefab
            string killFeedPrefabPath = "Assets/Prefabs/KillFeedItem.prefab";
            
            // Ensure directory exists
            string directory = System.IO.Path.GetDirectoryName(killFeedPrefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab
            GameObject killFeedPrefab = PrefabUtility.SaveAsPrefabAsset(killFeedItem, killFeedPrefabPath);
            
            // Destroy the temporary object
            DestroyImmediate(killFeedItem);
            
            // Create end simulation panel
            GameObject endPanel = new GameObject("SimulationEndPanel");
            endPanel.transform.SetParent(canvas.transform);
            endPanel.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.8f);
            endPanel.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            endPanel.GetComponent<RectTransform>().anchorMax = Vector2.one;
            endPanel.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Create result text
            GameObject resultTextObj = new GameObject("ResultText");
            resultTextObj.transform.SetParent(endPanel.transform);
            TMPro.TextMeshProUGUI resultText = resultTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            resultText.text = "Simulation Ended";
            resultText.alignment = TMPro.TextAlignmentOptions.Center;
            resultText.fontSize = 36;
            resultTextObj.GetComponent<RectTransform>().anchorMin = new Vector2(0.2f, 0.6f);
            resultTextObj.GetComponent<RectTransform>().anchorMax = new Vector2(0.8f, 0.8f);
            resultTextObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Create restart button
            GameObject restartButton = new GameObject("RestartButton");
            restartButton.transform.SetParent(endPanel.transform);
            UnityEngine.UI.Button button = restartButton.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image buttonImage = restartButton.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f);
            button.targetGraphic = buttonImage;
            restartButton.GetComponent<RectTransform>().anchorMin = new Vector2(0.35f, 0.4f);
            restartButton.GetComponent<RectTransform>().anchorMax = new Vector2(0.65f, 0.5f);
            restartButton.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Create button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(restartButton.transform);
            TMPro.TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TMPro.TextMeshProUGUI>();
            buttonText.text = "Restart Simulation";
            buttonText.alignment = TMPro.TextAlignmentOptions.Center;
            buttonText.fontSize = 24;
            buttonText.color = Color.white;
            buttonTextObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            buttonTextObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            buttonTextObj.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            // Set up the SimulationLoader references
            SimulationLoader loader = (SimulationLoader)target;
            SerializedObject loaderSO = new SerializedObject(loader);
            loaderSO.FindProperty("simulationEndPanel").objectReferenceValue = endPanel;
            loaderSO.FindProperty("simulationResultText").objectReferenceValue = resultText;
            loaderSO.FindProperty("killFeedPrefab").objectReferenceValue = killFeedPrefab;
            loaderSO.FindProperty("killFeedContainer").objectReferenceValue = killFeedContainer.transform;
            loaderSO.ApplyModifiedProperties();
            
            // Set up button click event
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                button.onClick,
                loader.RestartSimulation
            );
            
            Debug.Log("Created UI elements");
        }
    }
}