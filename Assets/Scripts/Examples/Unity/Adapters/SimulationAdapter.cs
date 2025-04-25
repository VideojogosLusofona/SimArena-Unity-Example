using System;
using System.Numerics;
using SimToolAI.Core;
using SimToolAI.Core.Configuration;
using SimToolAI.Core.Entities;
using SimToolAI.Core.Map;
using SimToolAI.Core.Rendering;
using SimToolAI.Utilities;
using SimulationMode = SimToolAI.Core.Configuration.SimulationMode;

namespace Examples.Unity.Adapters
{
    /// <summary>
    /// Adapter class that bridges between the core simulation and Unity visualization
    /// This follows the Adapter pattern to decouple the simulation from the visualization
    /// </summary>
    public class SimulationAdapter
    {
        #region Properties
        
        /// <summary>
        /// The underlying simulation
        /// </summary>
        public Simulation Simulation { get; private set; }
        
        /// <summary>
        /// The Unity scene
        /// </summary>
        public UnityScene UnityScene { get; private set; }
        
        /// <summary>
        /// The map
        /// </summary>
        public ISimMap Map => Simulation?.Map;
        
        /// <summary>
        /// Whether the simulation is initialized
        /// </summary>
        public bool IsInitialized => Simulation != null;
        
        /// <summary>
        /// Whether the simulation is running
        /// </summary>
        public bool IsRunning => Simulation != null && Simulation.IsRunning;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Event raised when the simulation is initialized
        /// </summary>
        public event EventHandler Initialized;
        
        /// <summary>
        /// Event raised when the simulation is started
        /// </summary>
        public event EventHandler Started;
        
        /// <summary>
        /// Event raised when the simulation is paused
        /// </summary>
        public event EventHandler Paused;
        
        /// <summary>
        /// Event raised when the simulation is resumed
        /// </summary>
        public event EventHandler Resumed;
        
        /// <summary>
        /// Event raised when the simulation is stopped
        /// </summary>
        public event EventHandler<SimulationResult> Stopped;
        
        /// <summary>
        /// Event raised when a step is completed
        /// </summary>
        public event EventHandler<int> StepCompleted;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Initializes the simulation with an existing map and scene
        /// </summary>
        /// <param name="config">Match configuration</param>
        /// <param name="map">The map</param>
        /// <param name="scene">The scene</param>
        public void Initialize(MatchConfig config, ISimMap map, UnityScene scene)
        {
            // Force realtime mode if there are human agents
            SimulationMode mode = SimulationMode.Offline;
            if (config.RealtimeMode || config.Agents.Exists(a => a.BrainType == BrainType.Human))
            {
                mode = SimulationMode.Realtime;
                
                // Ensure MaxSteps is high enough for human play
                if (config.MaxSteps < 10000)
                {
                    UnityEngine.Debug.Log("Increasing MaxSteps for human play");
                    config.MaxSteps = 10000;
                }
            }
            
            // Create the simulation
            Simulation = new Simulation(config, mode);
            UnityScene = scene;
            
            // Subscribe to simulation events
            SubscribeToEvents();
            
            // Initialize the simulation
            Simulation.Initialize(map, scene);
            
            UnityEngine.Debug.Log($"Simulation initialized in {mode} mode with {Simulation.Agents.Count} agents");
        }
        
        /// <summary>
        /// Starts the simulation
        /// </summary>
        public void Start()
        {
            if (!IsInitialized)
                return;
                
            Simulation.Start();
        }
        
        /// <summary>
        /// Pauses the simulation
        /// </summary>
        public void Pause()
        {
            if (!IsRunning)
                return;
                
            Simulation.Pause();
        }
        
        /// <summary>
        /// Resumes the simulation
        /// </summary>
        public void Resume()
        {
            if (IsRunning)
                return;
                
            Simulation.Resume();
        }
        
        /// <summary>
        /// Stops the simulation
        /// </summary>
        public void Stop()
        {
            if (!IsInitialized)
                return;
                
            Simulation.Stop();
        }
        
        /// <summary>
        /// Updates the simulation
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update in seconds</param>
        public void Update(float deltaTime)
        {
            if (!IsRunning)
                return;
                
            Simulation.Update(deltaTime);
        }

        /// <summary>
        /// Processes input for a human-controlled player
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="direction">Direction to move, or null for no movement</param>
        public void ProcessPlayerInput(Player entity, Vector3 direction)
        {
            if (!IsRunning)
                return;
                
            entity.ProcessInput(direction, false);
        }
        
        /// <summary>
        /// Cleans up the simulation
        /// </summary>
        public void Cleanup()
        {
            if (Simulation != null)
            {
                // Unsubscribe from simulation events
                UnsubscribeFromEvents();
                
                // Stop the simulation
                Simulation.Stop();
                Simulation = null;
            }
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Subscribes to simulation events
        /// </summary>
        private void SubscribeToEvents()
        {
            Simulation.Initialized += OnSimulationInitialized;
            Simulation.Started += OnSimulationStarted;
            Simulation.Paused += OnSimulationPaused;
            Simulation.Resumed += OnSimulationResumed;
            Simulation.Stopped += OnSimulationStopped;
            Simulation.StepCompleted += OnSimulationStepCompleted;
        }
        
        /// <summary>
        /// Unsubscribes from simulation events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            Simulation.Initialized -= OnSimulationInitialized;
            Simulation.Started -= OnSimulationStarted;
            Simulation.Paused -= OnSimulationPaused;
            Simulation.Resumed -= OnSimulationResumed;
            Simulation.Stopped -= OnSimulationStopped;
            Simulation.StepCompleted -= OnSimulationStepCompleted;
        }
        
        #endregion
        
        #region Event Handlers
        
        /// <summary>
        /// Called when the simulation is initialized
        /// </summary>
        private void OnSimulationInitialized(object sender, EventArgs e)
        {
            Initialized?.Invoke(this, e);
        }
        
        /// <summary>
        /// Called when the simulation is started
        /// </summary>
        private void OnSimulationStarted(object sender, EventArgs e)
        {
            Started?.Invoke(this, e);
        }
        
        /// <summary>
        /// Called when the simulation is paused
        /// </summary>
        private void OnSimulationPaused(object sender, EventArgs e)
        {
            Paused?.Invoke(this, e);
        }
        
        /// <summary>
        /// Called when the simulation is resumed
        /// </summary>
        private void OnSimulationResumed(object sender, EventArgs e)
        {
            Resumed?.Invoke(this, e);
        }
        
        /// <summary>
        /// Called when the simulation is stopped
        /// </summary>
        private void OnSimulationStopped(object sender, SimulationResult e)
        {
            Stopped?.Invoke(this, e);
        }
        
        /// <summary>
        /// Called when a step is completed
        /// </summary>
        private void OnSimulationStepCompleted(object sender, int e)
        {
            StepCompleted?.Invoke(this, e);
        }
        
        #endregion
    }
}