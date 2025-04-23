using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Examples.Unity.Adapters;
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
                // Process movement input
                ProcessMovementInput();

                // Update the bullet manager
                bulletManager.ManualUpdate(Time.deltaTime, playerManager.Player);

                // Update the camera position
                cameraController.UpdateCameraPosition(playerManager.PlayerObject.transform.position);

                // Update and render the scene
                _unityScene.Update(Time.deltaTime);
                _unityScene.Render();
            
                // Update the simulation
                if (SimulationAdapter != null && SimulationAdapter.IsRunning)
                {
                    SimulationAdapter.Update(Time.deltaTime);
                }
                
                UpdateEntityVisualizations();
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
            
            // Create the player
            playerManager.CreatePlayer(mapManager.Map, _unityScene, mapManager.Grid, SimulationAdapter.Simulation);

            // Initialize the bullet manager
            bulletManager.Initialize(_unityScene, SimulationAdapter.Simulation);
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
        /// Processes movement input from the input system
        /// </summary>
        private void ProcessMovementInput()
        {
            if (!IsRunning)
                return;
            
            // Only allow new movement input if the player is close to their target position
            if (playerManager.HasReachedTargetPosition(mapManager.Grid))
            {
                // Get movement direction
                System.Numerics.Vector3 moveDirection = inputManager.GetMovementDirection();

                if (moveDirection != DirectionVector.None)
                {
                    // Move the player
                    playerManager.MovePlayer(moveDirection, mapManager.Map, mapManager.Grid);
                    
                    // Log the movement action
                    SimulationAdapter.ProcessPlayerInput(playerManager.Player, moveDirection);
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
        /// Updates entity visualizations
        /// </summary>
        private void UpdateEntityVisualizations()
        {
            if (!IsInitialized || SimulationAdapter.UnityScene == null)
                return;
                
            // Update existing entities
            foreach (var entity in SimulationAdapter.UnityScene.GetEntities<Entity>())
            {
                if (_entityObjects.TryGetValue(entity.Id, out GameObject obj))
                {
                    // Update the object's position
                    obj.transform.position = mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y));
                    
                    // Update the object's rotation based on facing direction
                    float angle = DirectionVector.GetRotationAngle(entity.FacingDirection);
                    
                    obj.transform.rotation = Quaternion.Euler(0, 0, angle);
                    
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
                    // Create a new object for the entity
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
                GameObject obj = Instantiate(agentPrefab, 
                    mapManager.Grid.GetCellCenterWorld(new Vector3Int(entity.X, entity.Y)), 
                    Quaternion.identity);
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
}