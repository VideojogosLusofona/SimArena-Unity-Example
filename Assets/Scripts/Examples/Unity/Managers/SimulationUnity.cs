/*using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Examples.Unity.Adapters;
using Examples.Unity.Cosmetic;
using SimArena.Core.Configuration;
using SimArena.Core.Entities;
using SimArena.Core.Utilities;
using SimToolAI.Core;
using SimToolAI.Core.Configuration;
using SimToolAI.Core.Entities;
using SimToolAI.Core.Map;
using SimToolAI.Core.Rendering;
using SimToolAI.Unity;
using SimToolAI.Utilities;
using UnityEngine;

namespace Examples.Unity.Managers
{
    public class SimulationUnity : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TextAsset configFile;
        [SerializeField] private bool loadOnStart = true;
        
        [Header("Simulation")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float updateInterval = 0.05f;
        
        [Header("Visualization")]
        [SerializeField] private GameObject agentPrefab;
        [SerializeField] private PlayerManager playerManager;
        [SerializeField] private InputManager inputManager;
        [SerializeField] private MapManager mapManager;
        [SerializeField] private BulletManager bulletManager;
        [SerializeField] private CameraController cameraController;
        
        #region Properties
        
        /// <summary>
        /// The simulation adapter
        /// </summary>
        public SimulationAdapter SimulationAdapter { get; private set; }
        
        /// <summary>
        /// The match configuration
        /// </summary>
        public MatchConfig Config { get; private set; }
        
        /// <summary>
        /// Dictionary of entity GameObjects
        /// </summary>
        private readonly Dictionary<Guid, GameObject> _entityObjects = new();
        
        /// <summary>
        /// Whether the simulation is initialized
        /// </summary>
        public bool IsInitialized => SimulationAdapter != null && SimulationAdapter.IsInitialized;
        
        /// <summary>
        /// Whether the simulation is running
        /// </summary>
        public bool IsRunning => SimulationAdapter != null && SimulationAdapter.IsRunning;
        
        /// <summary>
        /// Reference to the Unity scene
        /// </summary>
        private UnityScene _unityScene;
        
        /// <summary>
        /// Reference to the map
        /// </summary>
        private ISimMap Map => mapManager.Map;
        
        private bool _realTime = false;
        
        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Called when the script instance is being loaded
        /// </summary>
        private void Awake()
        {
            inputManager.Initialize();
            inputManager.FireActionPerformed += OnFireActionPerformed;
            inputManager.FireActionCanceled += OnFireActionCanceled;
            
            // Create the simulation adapter
            SimulationAdapter = new SimulationAdapter();
            
            // Subscribe to simulation events
            SimulationAdapter.Initialized += OnSimulationInitialized;
            SimulationAdapter.Started += OnSimulationStarted;
            SimulationAdapter.Paused += OnSimulationPaused;
            SimulationAdapter.Resumed += OnSimulationResumed;
            SimulationAdapter.Stopped += OnSimulationStopped;
            SimulationAdapter.StepCompleted += OnSimulationStepCompleted;
        }
        
        /// <summary>
        /// Called when the script instance is being loaded
        /// </summary>
        private void Start()
        {
            if (loadOnStart && configFile != null && !string.IsNullOrEmpty(configFile.text))
            {
                LoadConfiguration(configFile);
                
                if (autoStart && IsInitialized)
                {
                    StartSimulation();
                }
            }
        }

        private void Update()
        {
            if (_realTime)
            {
                // Process movement input - this only sends input to the simulation
                ProcessMovementInput();

                // Update the bullet manager
                bulletManager.ManualUpdate(Time.deltaTime, playerManager.Player);

                // Update the camera position
                if (playerManager.PlayerObject != null)
                {
                    cameraController.UpdateCameraPosition(playerManager.PlayerObject.transform.position);
                }

                // Update the simulation first (this will update the scene internally)
                if (SimulationAdapter != null && SimulationAdapter.IsRunning)
                {
                    SimulationAdapter.Update(Time.deltaTime);
                }
                
                // Render the scene after simulation update
                if (_unityScene != null)
                {
                    _unityScene.Render();
                }
                
                // Smoothly move entities to their target positions
                SmoothlyUpdateEntityPositions();
                
                // Update entity visualizations (health bars, etc.)
                UpdateEntityVisualizations();
            }
        }
        
        /// <summary>
        /// Smoothly updates entity positions for visual appeal
        /// </summary>
        private void SmoothlyUpdateEntityPositions()
        {
            // Smoothly move the player to its target position
            if (playerManager.PlayerObject != null)
            {
                Vector3 targetPos = playerManager.GetTargetPosition();
                playerManager.PlayerObject.transform.position = Vector3.Lerp(
                    playerManager.PlayerObject.transform.position, 
                    targetPos, 
                    Time.deltaTime * playerManager.Player.Speed);
            }
            
            // Smoothly move other entities to their target positions
            foreach (var entity in SimulationAdapter.UnityScene.GetEntities<Entity>())
            {
                if (entity is Player) continue; // Skip player as it's handled above
                
                if (_entityObjects.TryGetValue(entity.Id, out GameObject obj))
                {
                    // Convert grid coordinates to world position
                    Vector3 targetPos = mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y, 0));
                    
                    // Ensure we're using the correct coordinate system
                    targetPos.z = 0; // Keep everything on the same Z plane
                    
                    obj.transform.position = Vector3.Lerp(
                        obj.transform.position,
                        targetPos,
                        Time.deltaTime * entity.Speed);
                        
                    // Update the object's rotation based on facing direction
                    float angle = DirectionVector.GetRotationAngle(entity.FacingDirection);
                    obj.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, angle);
                }
            }
        }

        /// <summary>
        /// Called when the script is destroyed
        /// </summary>
        private void OnDestroy()
        {
            StopSimulation();
            
            // Clean up the input manager
            inputManager.Cleanup();

            // Unsubscribe from input events
            inputManager.FireActionPerformed -= OnFireActionPerformed;
            inputManager.FireActionCanceled -= OnFireActionCanceled;
            
            // Unsubscribe from simulation events
            if (SimulationAdapter != null)
            {
                SimulationAdapter.Initialized -= OnSimulationInitialized;
                SimulationAdapter.Started -= OnSimulationStarted;
                SimulationAdapter.Paused -= OnSimulationPaused;
                SimulationAdapter.Resumed -= OnSimulationResumed;
                SimulationAdapter.Stopped -= OnSimulationStopped;
                SimulationAdapter.StepCompleted -= OnSimulationStepCompleted;
                
                SimulationAdapter.Cleanup();
                SimulationAdapter = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads a configuration file
        /// </summary>
        /// <param name="textAsset">The configuration file</param>
        public void LoadConfiguration(TextAsset textAsset)
        {
            try
            {
                // Load the configuration
                Config = MatchConfig.LoadFromTextJson(textAsset.text);
                
                // In unity editor's case, the map file needs to be in the StreamingAssets folder
                string fullPath = Path.Combine(Application.streamingAssetsPath, Config.MapPath);
                
                if (!File.Exists(fullPath))
                {
                    Debug.LogError($"Map file not found: {fullPath}");
                    return;
                }
                
                Config.MapPath = fullPath;
                
                // Validate the configuration
                if (!Config.Validate(false, out string errorMessage))
                {
                    Debug.LogError($"Invalid configuration: {errorMessage}");
                    return;
                }
                
                // Initialize the simulation
                InitializeSimulation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading configuration: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Initializes the simulation
        /// </summary>
        public void InitializeSimulation()
        {
            // Clean up any existing simulation
            CleanupSimulation();
            
            _realTime = Config.RealtimeMode;
            
            // If there are human agents, force realtime mode
            if (Config.Agents.Exists(a => a.BrainType == BrainType.Human))
            {
                _realTime = true;
            }
            
            // Load the map
            mapManager.Initialize(Config.MapPath);
            
            // Create a Unity scene with the map
            _unityScene = new UnityScene(Map);
            
            // Initialize the simulation adapter with the map and scene
            SimulationAdapter.Initialize(Config, Map, _unityScene);
            
            // Initialize the bullet manager first
            bulletManager.Initialize(_unityScene, SimulationAdapter.Simulation);
            
            // Subscribe to entity events from the simulation
            SimulationAdapter.Simulation.OnMove += OnEntityMoved;
            SimulationAdapter.Simulation.OnCreate += OnEntityCreated;
            
            // Create the player after simulation is initialized
            // This ensures the player is properly registered with the simulation
            playerManager.CreatePlayer(mapManager.Map, _unityScene, mapManager.Grid, SimulationAdapter.Simulation);
            
            // Register the player with the simulation if needed
            if (playerManager.Player != null && !SimulationAdapter.Simulation.Agents.Contains(playerManager.Player))
            {
                Debug.Log("Adding player to simulation agents list");
                SimulationAdapter.Simulation.Agents.Add(playerManager.Player);
                SimulationAdapter.Simulation.Scene.AddEntity(playerManager.Player);
            }
            
            // Create visual representations for all existing entities
            foreach (var entity in SimulationAdapter.UnityScene.GetEntities<Entity>())
            {
                CreateEntityObject(entity);
            }
        }
        
        /// <summary>
        /// Starts the simulation
        /// </summary>
        public void StartSimulation()
        {
            if (!IsInitialized)
                return;
                
            SimulationAdapter.Start();

            if (!_realTime)
            {
                // Start the update coroutine
                StartCoroutine(UpdateSimulation());
            }
        }
        
        /// <summary>
        /// Pauses the simulation
        /// </summary>
        public void PauseSimulation()
        {
            if (!IsRunning)
                return;
                
            SimulationAdapter.Pause();
        }
        
        /// <summary>
        /// Resumes the simulation
        /// </summary>
        public void ResumeSimulation()
        {
            if (IsRunning)
                return;
                
            SimulationAdapter.Resume();
        }
        
        /// <summary>
        /// Stops the simulation
        /// </summary>
        public void StopSimulation()
        {
            if (!IsInitialized)
                return;
                
            SimulationAdapter.Stop();
            CleanupSimulation();
        }
        
        /// <summary>
        /// Processes movement input from the input system and sends it to the simulation
        /// </summary>
        private void ProcessMovementInput()
        {
            if (!IsRunning || playerManager.Player == null)
                return;
            
            // Only allow new movement input if the player is close to their target position
            if (playerManager.HasReachedTargetPosition(mapManager.Grid))
            {
                // Get movement direction
                System.Numerics.Vector3 moveDirection = inputManager.GetMovementDirection();

                if (moveDirection != DirectionVector.None)
                {
                    // Send the input to the simulation through the player manager
                    playerManager.SendMovementInput(moveDirection);
                }
            }
        }
        
        /// <summary>
        /// Called when the fire action is performed (button pressed)
        /// </summary>
        private void OnFireActionPerformed()
        {
            bulletManager.StartFiring(playerManager.Player, playerManager.PlayerAnimations);
        }

        /// <summary>
        /// Called when the fire action is canceled (button released)
        /// </summary>
        private void OnFireActionCanceled()
        {
            bulletManager.StopFiring();
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        /// Cleans up the simulation
        /// </summary>
        private void CleanupSimulation()
        {
            // Unsubscribe from entity events
            if (SimulationAdapter != null && SimulationAdapter.Simulation != null)
            {
                SimulationAdapter.Simulation.OnMove -= OnEntityMoved;
                SimulationAdapter.Simulation.OnCreate -= OnEntityCreated;
            }
            
            // Destroy entity GameObjects
            foreach (var obj in _entityObjects.Values)
            {
                Destroy(obj);
            }
            
            _entityObjects.Clear();
            
            // Clean up the simulation adapter
            if (SimulationAdapter != null)
            {
                SimulationAdapter.Cleanup();
            }
            
            // Clear references
            _unityScene = null;
        }
        
        /// <summary>
        /// Called when an entity is moved in the simulation
        /// </summary>
        private void OnEntityMoved(object sender, Entity entity)
        {
            // Update the visual position of the entity
            if (entity is Player player && player.Equals(playerManager.Player))
            {
                // Update the player's visual position
                playerManager.UpdateVisualPosition(mapManager.Grid);
                
                // Update field of view
                mapManager.Map.ToggleFieldOfView(player);
            }
            else if (_entityObjects.TryGetValue(entity.Id, out GameObject obj))
            {
                // Convert grid coordinates to world position
                Vector3 targetPos = mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y, 0));
                
                // Ensure we're using the correct coordinate system
                targetPos.z = 0; // Keep everything on the same Z plane
                
                // For non-player entities, we can just set the position directly
                obj.transform.position = targetPos;
                
                // Update the object's rotation based on facing direction
                float angle = DirectionVector.GetRotationAngle(entity.FacingDirection);
                obj.transform.GetChild(0).rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        /// <summary>
        /// Called when an entity is created in the simulation
        /// </summary>
        private void OnEntityCreated(object sender, Entity entity)
        {
            // Create a visual representation for the entity
            CreateEntityObject(entity);
        }
        
        /// <summary>
        /// Updates the simulation
        /// </summary>
        private IEnumerator UpdateSimulation()
        {
            while (IsRunning)
            {
                // Update the simulation
                SimulationAdapter.Update(updateInterval);
                
                // Wait for the next update
                yield return new WaitForSeconds(updateInterval);
            }
        }
        
        /// <summary>
        /// Updates entity visualizations (health bars, etc.)
        /// </summary>
        private void UpdateEntityVisualizations()
        {
            if (!IsInitialized || SimulationAdapter.UnityScene == null)
                return;
                
            // Update existing entities (only health and status, not position)
            foreach (var entity in SimulationAdapter.UnityScene.GetEntities<Entity>())
            {
                if (_entityObjects.TryGetValue(entity.Id, out GameObject obj))
                {
                    // Update health bar if the entity is a character
                    if (entity is Character character)
                    {
                        var healthBar = obj.GetComponentInChildren<HealthBar>();
                        if (healthBar != null)
                        {
                            healthBar.SetHealth(character.Health);
                        }
                    }
                }
                else
                {
                    // Create a new object for the entity if it doesn't exist yet
                    CreateEntityObject(entity);
                }
            }
            
            // Remove objects for entities that no longer exist
            List<Guid> entitiesToRemove = new List<Guid>();
            
            foreach (var id in _entityObjects.Keys)
            {
                if (SimulationAdapter.UnityScene.GetEntity(id) == null)
                {
                    entitiesToRemove.Add(id);
                }
            }
            
            foreach (var id in entitiesToRemove)
            {
                if (_entityObjects.TryGetValue(id, out GameObject obj))
                {
                    Destroy(obj);
                    _entityObjects.Remove(id);
                }
            }
        }
        
        /// <summary>
        /// Creates a GameObject for an entity
        /// </summary>
        /// <param name="entity">Entity to create a GameObject for</param>
        private void CreateEntityObject(Entity entity)
        {
            // Skip if the entity already has a GameObject
            if (_entityObjects.ContainsKey(entity.Id))
                return;

            if (entity is Player)
            {
                _entityObjects[entity.Id] = playerManager.PlayerObject;
            }
            else
            {
                // Convert grid coordinates to world position
                Vector3 worldPos = mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y, 0));
                
                // Ensure we're using the correct coordinate system
                worldPos.z = 0; // Keep everything on the same Z plane
                
                GameObject obj = Instantiate(agentPrefab, worldPos, Quaternion.identity);
                obj.name = entity.Name;
            
                // Add the object to the dictionary
                _entityObjects[entity.Id] = obj;
            }
            
            // Set up the object's components
            if (entity is Character character)
            {
                // Set up health bar, etc.
                var healthBar = _entityObjects[entity.Id].GetComponentInChildren<HealthBar>();
                if (healthBar != null)
                {
                    healthBar.SetMaxHealth(character.MaxHealth);
                    healthBar.SetHealth(character.Health);
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            if (playerManager != null && playerManager.PlayerObject != null)
            {
                Vector3 targetPos = playerManager.GetTargetPosition();
                if (targetPos != Vector3.zero)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(playerManager.PlayerObject.transform.position, targetPos);

                    // Draw a sphere at the target position
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(targetPos, 0.1f);
                }
            }
        }

        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Called when the simulation is initialized
        /// </summary>
        private void OnSimulationInitialized(object sender, EventArgs e)
        {
            Debug.Log("Simulation initialized");
        }
        
        /// <summary>
        /// Called when the simulation is started
        /// </summary>
        private void OnSimulationStarted(object sender, EventArgs e)
        {
            Debug.Log("Simulation started");
        }
        
        /// <summary>
        /// Called when the simulation is paused
        /// </summary>
        private void OnSimulationPaused(object sender, EventArgs e)
        {
            Debug.Log("Simulation paused");
        }
        
        /// <summary>
        /// Called when the simulation is resumed
        /// </summary>
        private void OnSimulationResumed(object sender, EventArgs e)
        {
            Debug.Log("Simulation resumed");
        }
        
        /// <summary>
        /// Called when the simulation is stopped
        /// </summary>
        private void OnSimulationStopped(object sender, SimulationResult e)
        {
            Debug.Log($"Simulation stopped after {e.Steps} steps and {e.ElapsedTime} seconds");
            Debug.Log($"Surviving agents: {e.SurvivingAgents.Count}");
            Debug.Log($"Dead agents: {e.DefeatedAgents.Count}");
        }
        
        /// <summary>
        /// Called when a simulation step is completed
        /// </summary>
        private void OnSimulationStepCompleted(object sender, int e)
        {
        }
        
        #endregion
    }
}*/