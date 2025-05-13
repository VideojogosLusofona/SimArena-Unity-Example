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

            if (loadOnStart && configFile != null && !string.IsNullOrEmpty(configFile.text))
            {
                // Initialize UI
                if (simulationEndPanel != null)
                    simulationEndPanel.SetActive(false);
            
                // Load and start simulation
                StartCoroutine(LoadSimulationCoroutine());
            }
        }

        private IEnumerator LoadSimulationCoroutine()
        {
            GameConfig config = ValidateConfig();
            yield return StartCoroutine(InitializeMapCoroutine(config));
            InitializeScene();
            CreateSimulation(config);
            SubscribeToEvents();
    
            try
            {
                simulation.Initialize(simulationMap, simulationScene);
                Debug.Log("Simulation initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Simulation initialization failed: {ex.Message}");
            }
            
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
        
        private void InitializeScene()
        {
            try
            {
                simulationScene = new MinimalScene(simulationMap);
                Debug.Log("Scene created successfully");
            }
            catch (Exception sceneEx)
            {
                Debug.LogError($"Error creating scene: {sceneEx.Message}");
            }
        }
        
        private void CreateSimulation(GameConfig config)
        {
            // Create the simulation
            try
            {
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
            }
        }
        
        private IEnumerator InitializeMapCoroutine(GameConfig config)
        {
            if (!string.IsNullOrEmpty(config.MapPath))
            {
                string fullMapPath = Path.Combine(Application.streamingAssetsPath, config.MapPath);

                if (!string.IsNullOrEmpty(fullMapPath) && File.Exists(fullMapPath))
                {
                    config.MapPath = fullMapPath;
                    Debug.Log($"Loading map from file: {config.MapPath}");
                    try
                    {
                        mapManager.Initialize(config.MapPath);

                        // Wait until mapManager.Map is no longer null
                        float timeout = 5f;
                        float timer = 0f;
                        while (mapManager.Map == null && timer < timeout)
                        {
                            timer += Time.deltaTime;
                        }

                        if (mapManager.Map == null)
                        {
                            Debug.LogError("Map initialization timed out or failed.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error loading map from file: {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No valid map path provided, or file doesn't exist.");
                yield break;
            }
        }

        private GameConfig ValidateConfig()
        {
            GameConfig config;
                
            try {
                // Try using the GameConfig's built-in method
                config = GameConfig.LoadFromTextJson(configFile.text);
                
                if (config == null)
                {
                    Debug.LogError("Failed to parse configuration file with LoadFromTextJson. Check JSON format.");
                    return null;
                }
                
                Debug.Log($"Config parsed successfully. " +
                          $"Game name: {config.Name}, " +
                          $"Agent count: {config.Agents?.Count ?? 0}");
                          
               
                _realTime = config.RealtimeMode;
                
                if (config.Agents.Exists(a => a.BrainType == BrainType.Human))
                {
                    _realTime = true;
                }
            }
            catch (Exception parseEx)
            {
                Debug.LogError($"Error parsing configuration with LoadFromTextJson: {parseEx.Message}");
                

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
                        Debug.Log($"Config parsed successfully with JsonUtility. " +
                                  $"Game name: {config.Name}, Agent count: {config.Agents?.Count ?? 0}");
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

            return config;
        }
        
        private void Update()
        {
            // Log simulation status every 5 seconds for debugging
            if (Time.frameCount % 300 == 0) // Approximately every 5 seconds at 60 FPS
            {
                Debug.Log($"Simulation status - Exists: {simulation != null}, Running: {IsRunning}, RealTime: {_realTime}");
                
                // Log entity positions
                if (entityGameObjects.Count > 0)
                {
                    foreach (var kvp in entityGameObjects)
                    {
                        Entity entity = simulation.GetEntities().FirstOrDefault(e => e.Id == kvp.Key);
                        if (entity != null)
                        {
                            Debug.Log($"Entity {entity.Name} - Sim Position: ({entity.X}, {entity.Y}), Unity Position: {kvp.Value.transform.position}");
                        }
                    }
                }
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
            UnityEngine.Vector3 initialPosition = new UnityEngine.Vector3(entity.X, entity.Y, 0);
            GameObject entityObject = Instantiate(agentPrefab, initialPosition, Quaternion.identity);
            entityObject.name = $"{entity.Name} ({entity.Id})";
            
            // Set up the entity representation
            EntityVisualizer visualizer = entityObject.GetComponent<EntityVisualizer>();
            if (visualizer != null)
            {
                visualizer.Initialize(entity);
            }
            
            // Store the game object reference
            entityGameObjects[entity.Id] = entityObject;
            targetPositions[entity.Id] = initialPosition;
            
            // Log the creation for debugging
            Debug.Log($"Created entity {entity.Name} at position: X={entity.X}, Y={entity.Y} (Unity Vector3: {initialPosition})");
            Debug.Log($"GameObject position after creation: {entityObject.transform.position}");
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
                // Convert grid coordinates to world position using the grid
                Vector3 newPosition = mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y, 0));
                targetPositions[entity.Id] = newPosition;
                
                // Debug log to confirm movement events are being received
                Debug.Log($"Entity {entity.Name} moved to position: X={entity.X}, Y={entity.Y} (Unity Vector3: {newPosition})");
                
                // Force immediate position update for debugging
                if (entityGameObjects.TryGetValue(entity.Id, out GameObject entityObject))
                {
                    // Log the current position before updating
                    Debug.Log($"Entity {entity.Name} current GameObject position: {entityObject.transform.position}");
                }
            }
            else
            {
                Debug.LogWarning($"Entity {entity.Name} moved but no GameObject found for ID: {entity.Id}");
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
            // Check for null references before accessing properties
            if (killInfo.killer != null && killInfo.killed != null)
            {
                // Show kill feed notification
                ShowKillFeedNotification(killInfo.killer.Name, killInfo.killed.Name);
            }
            else
            {
                Debug.LogWarning("OnEntityKilled received null entity references");
            }
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
            
            // Only log this message occasionally to reduce spam
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"Updating positions for {entityGameObjects.Count} entities. Realtime mode: {_realTime}");
            }
            
            // Create a temporary list to avoid collection modification issues
            var entityIds = new List<Guid>(entityGameObjects.Keys);
            
            foreach (var entityId in entityIds)
            {
                if (entityGameObjects.TryGetValue(entityId, out GameObject entityObject) && 
                    targetPositions.TryGetValue(entityId, out UnityEngine.Vector3 targetPosition))
                {
                    // Smoothly move the entity to its target position
                    Vector3 oldPosition = entityObject.transform.position;
                    
                    // Check if we need to move at all
                    float distance = Vector3.Distance(oldPosition, targetPosition);
                    if (distance > 0.001f)
                    {
                        // Apply movement with lerp
                        entityObject.transform.position = Vector3.Lerp(
                            oldPosition, 
                            targetPosition, 
                            Time.deltaTime * movementLerpSpeed
                        );
                        
                        // Log significant movements to help with debugging (only occasionally)
                        if (distance > 0.1f && Time.frameCount % 10 == 0)
                        {
                            Debug.Log($"Entity {entityObject.name} moving from {oldPosition} to {entityObject.transform.position}, target: {targetPosition}, distance: {distance}");
                        }
                    }
                }
                else
                {
                    if (!entityGameObjects.ContainsKey(entityId))
                    {
                        Debug.LogWarning($"Entity with ID {entityId} not found in entityGameObjects");
                    }
                    else if (!targetPositions.ContainsKey(entityId))
                    {
                        Debug.LogWarning($"Entity with ID {entityId} not found in targetPositions");
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
            StartCoroutine(LoadSimulationCoroutine());
        }
    }
}