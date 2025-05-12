using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Examples.Unity.Managers;
using UnityEngine;
using UnityEngine.Tilemaps;
using SimArena.Core;
using SimArena.Core.Configuration;
using SimArena.Core.Entities;
using SimArena.Core.Serialization.Configuration;
using SimArena.Core.Serialization.Results;
using SimArena.Core.SimulationElements.Map;
using SimArena.Core.SimulationElements.Scene;
using TMPro;
using Random = UnityEngine.Random;
using SimulationMode = SimArena.Core.SimulationMode;

namespace Examples
{
    public class SimulationLoader  : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TextAsset configFile;
        [SerializeField] private bool loadOnStart = true;
        
        [Header("Simulation")]
        [SerializeField] private bool autoStart = true;
        
        [Header("Visualization")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private float movementLerpSpeed = 5f;
        
        [Header("UI")]
        [SerializeField] private GameObject simulationEndPanel;
        [SerializeField] private TextMeshProUGUI simulationResultText;
        [SerializeField] private GameObject killFeedPrefab;
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private float killFeedDisplayTime = 3f;
        
        // Simulation components
        private Simulation simulation;
        private bool _realTime = false;

        
        private IMap simulationMap => mapManager.Map;
        private Scene simulationScene;
        
        // Entity tracking
        private Dictionary<Guid, GameObject> entityGameObjects = new();
        private Dictionary<Guid, Vector3> targetPositions = new();
        
        /// <summary>
        /// Whether the simulation is running
        /// </summary>
        public bool IsRunning => simulation != null && simulation.IsRunning;
        
        private void Start()
        {
            if (configFile == null)
            {
                Debug.LogError("No configuration file assigned!");
                return;
            }
            else if (loadOnStart && configFile != null && !string.IsNullOrEmpty(configFile.text))
            {
                // Initialize UI
                if (simulationEndPanel != null)
                    simulationEndPanel.SetActive(false);
            
                // Load and start simulation
                LoadSimulation();
            }
        }
        
        private void Update()
        {
            // Log simulation status every 5 seconds for debugging
            if (Time.frameCount % 300 == 0) // Approximately every 5 seconds at 60 FPS
            {
                Debug.Log($"Simulation status - Exists: {simulation != null}, Running: {IsRunning}, RealTime: {_realTime}");
            }
            
            if (simulation != null && IsRunning && _realTime)
            {
                // Update the simulation
                simulation.Update(Time.deltaTime);
                
                // Make sure we have entities before trying to update camera
                if (entityGameObjects.Count > 0)
                {
                    // Look at the first entity
                    cameraController.UpdateCameraPosition(entityGameObjects.First().Value.transform.position);
                }
                else
                {
                    Debug.LogWarning("No entities to focus camera on");
                }
                
                // Update visual positions with lerping
                UpdateEntityPositions();
            }
        }
        
        private void LoadSimulation()
        {
            try
            {
                // Check if config file is assigned
                if (configFile == null)
                {
                    Debug.LogError("Failed to load simulation: Config file is not assigned");
                    return;
                }
                
                // Parse the configuration file
                Debug.Log("Parsing configuration file...");
                GameConfig config;
                
                try {
                    // Try using the GameConfig's built-in method
                    config = GameConfig.LoadFromTextJson(configFile.text);
                    
                    if (config == null)
                    {
                        Debug.LogError("Failed to parse configuration file with LoadFromTextJson. Check JSON format.");
                        return;
                    }
                    
                    Debug.Log($"Config parsed successfully. " +
                              $"Game name: {config.Name}, " +
                              $"Agent count: {config.Agents?.Count ?? 0}");
                              
                    // Set realtime mode flag
                    _realTime = config.RealtimeMode;
                    
                    if (config.Agents.Exists(a => a.BrainType == BrainType.Human))
                    {
                        _realTime = true;
                    }
                }
                catch (Exception parseEx)
                {
                    Debug.LogError($"Error parsing configuration with LoadFromTextJson: {parseEx.Message}");
                    
                    // Fallback to JsonUtility
                    Debug.Log("Falling back to JsonUtility...");
                    try
                    {
                        config = JsonUtility.FromJson<GameConfig>(configFile.text);
                        
                        if (config == null)
                        {
                            Debug.LogError("Failed to parse configuration with JsonUtility. Check JSON format.");
                            Debug.LogError($"Config content: {configFile.text}");
                            
                            // Create a default config as a last resort
                            Debug.LogWarning("Creating default configuration as fallback...");
                            config = CreateDefaultConfig();
                        }
                        else
                        {
                            Debug.Log($"Config parsed successfully with JsonUtility. Game name: {config.Name}, Agent count: {config.Agents?.Count ?? 0}");
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Debug.LogError($"Error parsing configuration with JsonUtility: {jsonEx.Message}");
                        Debug.LogWarning("Creating default configuration as fallback...");
                        config = CreateDefaultConfig();
                    }
                    
                    // Set realtime mode flag
                    _realTime = config.RealtimeMode;

                    if (config.Agents.Exists(a => a.BrainType == BrainType.Human))
                    {
                        _realTime = true;
                    }
                }
                
                // Create the simulation
                try
                {
                    string fullMapPath = Path.Combine(Application.streamingAssetsPath, config.MapPath);
                    
                    // Check if the map path exists and modify if needed
                    if (!string.IsNullOrEmpty(fullMapPath) && !File.Exists(fullMapPath))
                    {
                        Debug.LogWarning($"Map file not found at path: {config.MapPath}. Using procedural map instead.");
                        config.MapPath = null; 
                    }
                    else
                    {
                        config.MapPath = fullMapPath;
                    }
                    
                    if (!config.Validate(false, out string err))
                    {
                        Debug.LogError($"Invalid configuration: {err}");
                        return;
                    }
                    
                    simulation = new Simulation(config, SimulationMode.Realtime);
                    Debug.Log("Simulation created successfully");
                }
                catch (Exception simEx)
                {
                    Debug.LogError($"Error creating simulation: {simEx.Message}");
                    Debug.LogError($"Stack trace: {simEx.StackTrace}");
                    return;
                }
                
                // Create the map
                Debug.Log("Creating map...");
                try
                {
                    CreateMap(config);
                    Debug.Log("Map created successfully");
                }
                catch (Exception mapEx)
                {
                    Debug.LogError($"Error creating map: {mapEx.Message}");
                    return;
                }
                
                // Create the scene
                Debug.Log("Creating scene...");
                try
                {
                    simulationScene = new MinimalScene(simulationMap);
                    Debug.Log("Scene created successfully");
                }
                catch (Exception sceneEx)
                {
                    Debug.LogError($"Error creating scene: {sceneEx.Message}");
                    return;
                }
                
                // Subscribe to simulation events
                Debug.Log("Subscribing to events...");
                try
                {
                    SubscribeToEvents();
                    Debug.Log("Events subscribed successfully");
                }
                catch (Exception eventEx)
                {
                    Debug.LogError($"Error subscribing to events: {eventEx.Message}");
                    return;
                }
                
                // Initialize the simulation
                Debug.Log("Initializing simulation...");
                try
                {
                    simulation.Initialize(simulationMap, simulationScene);
                    Debug.Log("Simulation initialized successfully");
                }
                catch (Exception initEx)
                {
                    Debug.LogError($"Error initializing simulation: {initEx.Message}");
                    return;
                }
                
                // Start the simulation
                if (autoStart)
                {
                    Debug.Log("Starting simulation...");
                    try
                    {
                        simulation.Start();
                        Debug.Log("Simulation started successfully");
                    }
                    catch (Exception startEx)
                    {
                        Debug.LogError($"Error starting simulation: {startEx.Message}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load simulation: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        private void CreateMap(GameConfig config)
        {
            // Check if we should load the map from a file
            if (!string.IsNullOrEmpty(config.MapPath) && File.Exists(config.MapPath))
            {
                Debug.Log($"Loading map from file: {config.MapPath}");
                try
                {
                    mapManager.Initialize(config.MapPath); ;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error loading map from file: {ex.Message}");
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            simulation.Events.OnCreate += OnEntityCreated;
            simulation.Events.OnDestroy += OnEntityDestroyed;
            simulation.Events.OnMove += OnEntityMoved;
            simulation.Events.OnDamage += OnEntityDamaged;
            simulation.Events.OnHeal += OnEntityHealed;
            simulation.Events.OnKill += OnEntityKilled;
            simulation.Events.Stopped += OnSimulationStopped;
            simulation.Events.Paused += OnSimulationPaused;
            simulation.Events.Resumed += OnSimulationResumed;
        }
        
        private void OnEntityCreated(object sender, Entity entity)
        {
            if (agentPrefab == null)
            {
                Debug.LogError("Agent prefab not assigned!");
                return;
            }
            
            // Create a game object for the entity
            GameObject entityObject = Instantiate(agentPrefab, new Vector3(entity.X, entity.Y, 0),
                Quaternion.identity);
            entityObject.name = $"{entity.Name} ({entity.Id})";
            
            // Set up the entity representation
            EntityVisualizer visualizer = entityObject.GetComponent<EntityVisualizer>();
            if (visualizer != null)
            {
                visualizer.Initialize(entity);
            }
            
            // Store the game object reference
            entityGameObjects[entity.Id] = entityObject;
            targetPositions[entity.Id] = new Vector3(entity.X, entity.Y, 0);
        }
        
        private void OnEntityDestroyed(object sender, Entity entity)
        {
            if (entityGameObjects.TryGetValue(entity.Id, out GameObject entityObject))
            {
                // Destroy the game object
                Destroy(entityObject);
                entityGameObjects.Remove(entity.Id);
                targetPositions.Remove(entity.Id);
            }
        }
        
        private void OnEntityMoved(object sender, Entity entity)
        {
            if (entityGameObjects.ContainsKey(entity.Id))
            {
                // Update the target position for lerping
                Vector3 newPosition = new Vector3(entity.X, entity.Y, 0);
                targetPositions[entity.Id] = newPosition;
                
                // Debug log to confirm movement events are being received
                Debug.Log($"Entity {entity.Name} moved to position: {newPosition}");
            }
        }
        
        private void OnEntityDamaged(object sender, (Entity attacker, Entity victim) damageInfo)
        {
            if (entityGameObjects.TryGetValue(damageInfo.victim.Id, out GameObject victimObject))
            {
                // Update health bar
                EntityVisualizer visualizer = victimObject.GetComponent<EntityVisualizer>();
                if (visualizer != null && damageInfo.victim is Character character)
                {
                    visualizer.UpdateHealth(character.Health, character.MaxHealth);
                }
            }
        }
        
        private void OnEntityHealed(object sender, (Entity healer, Entity healed) healInfo)
        {
            if (entityGameObjects.TryGetValue(healInfo.healed.Id, out GameObject healedObject))
            {
                // Update health bar
                EntityVisualizer visualizer = healedObject.GetComponent<EntityVisualizer>();
                if (visualizer != null && healInfo.healed is Character character)
                {
                    visualizer.UpdateHealth(character.Health, character.MaxHealth);
                }
            }
        }
        
        private void OnEntityKilled(object sender, (Entity killer, Entity killed) killInfo)
        {
            // Show kill feed notification
            ShowKillFeedNotification(killInfo.killer.Name, killInfo.killed.Name);
        }
        
        private void OnSimulationStopped(object sender, ISimulationResult result)
        {
            // Show simulation end panel
            if (simulationEndPanel != null)
            {
                simulationEndPanel.SetActive(true);
                
                if (simulationResultText != null)
                {
                    simulationResultText.text = $"Simulation Ended: {result.Read()}";
                }
            }
            
            // Clean up all entities
            foreach (var entityObject in entityGameObjects.Values)
            {
                Destroy(entityObject);
            }
            
            entityGameObjects.Clear();
            targetPositions.Clear();
        }
        
        private void OnSimulationPaused(object sender, EventArgs e)
        {
            Debug.Log("Simulation paused");
        }
        
        private void OnSimulationResumed(object sender, EventArgs e)
        {
            Debug.Log("Simulation resumed");
        }
        
        private void UpdateEntityPositions()
        {
            if (entityGameObjects.Count == 0)
            {
                Debug.LogWarning("No entities to update positions for");
                return;
            }
            
            Debug.Log($"Updating positions for {entityGameObjects.Count} entities. Realtime mode: {_realTime}");
            
            foreach (var entityId in entityGameObjects.Keys)
            {
                if (entityGameObjects.TryGetValue(entityId, out GameObject entityObject) && 
                    targetPositions.TryGetValue(entityId, out Vector3 targetPosition))
                {
                    // Smoothly move the entity to its target position
                    Vector3 oldPosition = entityObject.transform.position;
                    entityObject.transform.position = Vector3.Lerp(
                        oldPosition, 
                        targetPosition, 
                        Time.deltaTime * movementLerpSpeed
                    );
                    
                    // Log significant movements to help with debugging
                    if (Vector3.Distance(oldPosition, entityObject.transform.position) > 0.01f)
                    {
                        Debug.Log($"Entity {entityObject.name} moved from {oldPosition} to {entityObject.transform.position}, target: {targetPosition}");
                    }
                }
            }
        }
        
        private void ShowKillFeedNotification(string killerName, string killedName)
        {
            if (killFeedPrefab == null || killFeedContainer == null)
                return;
                
            GameObject killFeedItem = Instantiate(killFeedPrefab, killFeedContainer);
            KillFeedItem feedItem = killFeedItem.GetComponent<KillFeedItem>();
            
            if (feedItem != null)
            {
                feedItem.SetKillInfo(killerName, killedName);
                StartCoroutine(DestroyAfterDelay(killFeedItem, killFeedDisplayTime));
            }
        }
        
        private IEnumerator DestroyAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(obj);
        }
        
        /// <summary>
        /// Creates a default configuration for the simulation when parsing fails
        /// </summary>
        private GameConfig CreateDefaultConfig()
        {
            Debug.Log("Creating default configuration...");
            
            // Create a simple configuration with two agents
            GameConfig config = new GameConfig
            {
                Name = "Default Simulation",
                RealtimeMode = true,
                Objective = new StepsObjective
                {
                    TypeEnum = SimulationObjective.Steps,
                    MaxSteps = 1000
                },
                Agents = new List<AgentConfig>
                {
                    new()
                    {
                        Name = "Agent 1",
                        BrainType = BrainType.AI,
                        RandomStart = true,
                        Awareness = 10,
                        MaxHealth = 100,
                        AttackPower = 10,
                        Defense = 5,
                        Speed = 1.0f,
                        ThinkInterval = 0.5f,
                        OwnedWeaponIds = Array.Empty<string>()
                    },
                    new()
                    {
                        Name = "Agent 2",
                        BrainType = BrainType.AI,
                        RandomStart = true,
                        Awareness = 10,
                        MaxHealth = 100,
                        AttackPower = 10,
                        Defense = 5,
                        Speed = 1.0f,
                        ThinkInterval = 0.5f,
                        OwnedWeaponIds = Array.Empty<string>()
                    }
                },
                Weapons = new List<WeaponConfig>()
            };
            
            Debug.Log("Default configuration created successfully");
            return config;
        }
        
        // Public methods for UI buttons
        public void PauseSimulation()
        {
            if (simulation != null && simulation.IsRunning)
            {
                simulation.Pause();
            }
        }
        
        public void ResumeSimulation()
        {
            if (simulation != null && !simulation.IsRunning)
            {
                simulation.Resume();
            }
        }
        
        public void StopSimulation()
        {
            if (simulation != null)
            {
                simulation.Stop();
            }
        }
        
        public void RestartSimulation()
        {
            // Clean up existing simulation
            if (simulation != null)
            {
                simulation.Stop();
            }
            
            // Clear all entities
            foreach (var entityObject in entityGameObjects.Values)
            {
                Destroy(entityObject);
            }
            
            entityGameObjects.Clear();
            targetPositions.Clear();
            
            // Hide UI
            if (simulationEndPanel != null)
                simulationEndPanel.SetActive(false);
                
            // Load new simulation
            LoadSimulation();
        }
    }
}