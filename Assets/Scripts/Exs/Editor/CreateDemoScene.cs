using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Examples.Unity.Managers;
using Unity.VisualScripting;
using UnityEngine.Tilemaps;

namespace Examples.Editor
{
    public class CreateDemoScene
    {
        [MenuItem("SimArena/Create Demo Scene")]
        public static void CreateScene()
        {
            // Create a new scene
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Create simulation loader
            GameObject simulationLoaderObj = new GameObject("SimulationLoader");
            SimulationLoader loader = simulationLoaderObj.AddComponent<SimulationLoader>();
            MapManager mapManager = simulationLoaderObj.AddComponent<MapManager>();

            if (mapManager != null)
            {
                SerializedObject so = new SerializedObject(loader);
                so.FindProperty("mapManager").objectReferenceValue = mapManager;
                so.ApplyModifiedProperties();
            }
            
            // Create grid and tilemaps
            GameObject grid = new GameObject("Grid");
            grid.AddComponent<Grid>();
            
            GameObject floorTilemap = new GameObject("Tilemap");
            floorTilemap.transform.SetParent(grid.transform);
            floorTilemap.AddComponent<Tilemap>();
            floorTilemap.AddComponent<TilemapRenderer>();
            
            // Set up camera
            Camera mainCamera = Camera.main;
            
            if (mainCamera != null)
            {
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 10;
                mainCamera.transform.position = new Vector3(10, 10, -10);
            }
            
            CameraController cameraController = mainCamera.AddComponent<CameraController>();

            if (cameraController != null)
            {
                SerializedObject so = new SerializedObject(loader);
                so.FindProperty("cameraController").objectReferenceValue = cameraController;
                so.ApplyModifiedProperties();
            }
            
            // Try to find and assign the config file
            TextAsset configAsset = Resources.Load<TextAsset>("DefaultSimConfig");
            
            if (configAsset != null)
            {
                SerializedObject so = new SerializedObject(loader);
                so.FindProperty("configFile").objectReferenceValue = configAsset;
                so.ApplyModifiedProperties();
                
                SerializedObject mp = new SerializedObject(mapManager);
                mp.FindProperty("tilemap").objectReferenceValue = floorTilemap;
                mp.FindProperty("grid").objectReferenceValue = grid;
                mp.ApplyModifiedProperties();
            }
            
            // Try to find and assign tiles
            TileBase wallTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/RuleTile.asset");
            if (wallTile != null)
            {
                SerializedObject so = new SerializedObject(mapManager);
                so.FindProperty("wallTile").objectReferenceValue = wallTile;
                so.ApplyModifiedProperties();
            }
            
            TileBase floorTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/FloorTile.asset");
            if (floorTile != null)
            {
                SerializedObject so = new SerializedObject(mapManager);
                so.FindProperty("floorTile").objectReferenceValue = floorTile;
                so.ApplyModifiedProperties();
            }
            
            // Save the scene
            string scenePath = "Assets/Scenes/SimulationDemo.unity";
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(scenePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);
            
            Debug.Log($"Demo scene created at {scenePath}");
            Debug.Log("Note: You still need to assign the agent prefab and UI elements in the inspector.");
        }
    }
}