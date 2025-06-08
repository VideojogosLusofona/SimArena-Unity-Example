using System;
using System.Collections.Generic;
using System.Text.Json;
using SimArena.Core;
using SimArena.Entities;
using SimArena.Serialization.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UnitySide
{
    public class GameController: MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Game configuration file (JSON format)")]
        public TextAsset configurationFile;
        
        [Header("Map Settings")]
        public int mapWidth = 50;
        public int mapHeight = 50;
        
        [Header("Elements")]
        public MapView mapView;
        public GameObject agentPrefab;
        
        [Header("UI")]
        public GameObject victoryPanel;
        public TMP_Text victoryText;
        public Button restartButton;
        
        private Dictionary<Agent, AgentView> _views = new();
        private bool _isGameRunning = false;
        
        public Simulation Engine { get; set; }
        
        void Start()
        {
            // Hide victory panel at start
            if (victoryPanel != null)
                victoryPanel.SetActive(false);
                
            // Add listener to restart button
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
            
            StartGame();
        }
        
        /// <summary>
        /// Loads a GameConfiguration from a TextAsset (JSON)
        /// </summary>
        private GameConfiguration LoadConfiguration(TextAsset configAsset)
        {
            if (configAsset == null)
            {
                Debug.LogWarning("No configuration file provided. Using default settings.");
                return CreateDefaultConfiguration();
            }

            try
            {
                string jsonText = configAsset.text;
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };
                
                return JsonSerializer.Deserialize<GameConfiguration>(jsonText, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }
        
        /// <summary>
        /// Creates a default configuration when none is provided
        /// </summary>
        private GameConfiguration CreateDefaultConfiguration()
        {
            var config = new GameConfiguration
            {
                Name = "Default Game",
                MapPath = "", 
                Agents = new List<AgentConfiguration>()
            };
            
            // Add some default agents
            for (int team = 0; team < 2; team++)
            {
                // Add hunters (70% of agents)
                for (int i = 0; i < 3; i++)
                {
                    config.Agents.Add(new AgentConfiguration
                    {
                        Name = $"Hunter-Team{team+1}-{i+1}",
                        Brain = new SimArena.Serialization.Configuration.Brains.ChaserBrainConfiguration
                        {
                            Team = team,
                            IsPassive = i % 3 == 0 // 33% passive
                        },
                        RandomStart = true
                    });
                }
                
                // Add random agents (30% of agents)
                for (int i = 0; i < 2; i++)
                {
                    config.Agents.Add(new AgentConfiguration
                    {
                        Name = $"Random-Team{team+1}-{i+1}",
                        Brain = new SimArena.Serialization.Configuration.Brains.RandomBrainConfiguration
                        {
                            Team = team
                        },
                        RandomStart = true
                    });
                }
            }
            
            return config;
        }
        
        private void StartGame()
        {
            // Load configuration
            GameConfiguration config = LoadConfiguration(configurationFile);
            
            // Create new engine
            Engine = new Simulation(mapWidth, mapHeight);
            
            // Subscribe to events
            Engine.Events.OnAgentKilled += (_, agent) => HandleAgentKilled(agent);
            Engine.Events.OnTeamWon += (_, i) => HandleTeamWon(i);
            
            // Initialize map view
            mapView.Init(Engine);
            
            // Clear any existing views
            foreach (var view in _views.Values)
            {
                if (view != null && view.gameObject != null)
                    Destroy(view.gameObject);
            }
            _views.Clear();
            
            // Dictionary to track teams and count agents per team
            Dictionary<int, int> teamCounts = new Dictionary<int, int>();
            
            // Create agents from configuration
            foreach (var agentConfig in config.Agents)
            {
                int team = agentConfig.Brain.Team;
                
                // Track teams
                if (!teamCounts.ContainsKey(team))
                    teamCounts[team] = 0;
                teamCounts[team]++;
                
                // Determine spawn location
                (int x, int y) whereToSpawn;
                if (agentConfig.RandomStart)
                {
                    whereToSpawn = Brain.GetRandomWalkableLocation(Engine.Map);
                }
                else
                {
                    whereToSpawn = (agentConfig.StartX, agentConfig.StartY);
                }
                
                // Create brain for agent
                Brain agentBrain = agentConfig.Brain.CreateBrain(null, Engine.Map, Engine);
                
                // Create agent
                var agent = new Agent(whereToSpawn.x, whereToSpawn.y, agentBrain, team, agentConfig.Name);
                agentBrain.SetAgent(agent);
                Engine.AddAgent(agent);
                
                // Create visual representation
                var go = Instantiate(agentPrefab);
                var view = go.GetComponent<AgentView>();
                view.Initialize(agent);
                _views[agent] = view;
            }
            
            Debug.Log($"Loaded {config.Agents.Count} agents across {teamCounts.Count} teams.");
            _isGameRunning = true;
        }

        private void Update()
        {
            if (_isGameRunning)
            {
                Engine.Update();
            }
        }
        
        private void HandleAgentKilled(Agent agent)
        {
            if (_views.TryGetValue(agent, out var view) && view != null)
            {
                // Destroy the agent's visual representation
                Destroy(view.gameObject);
                _views.Remove(agent);
            }
        }
        
        private void HandleTeamWon(int team)
        {
            _isGameRunning = false;
            
            // Show victory panel
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
                
                if (victoryText != null)
                {
                    victoryText.text = $"Team {team + 1} Won!";
                    
                    // Set text color to match team color
                    if (team < AgentView.PastelColors.Count)
                    {
                        victoryText.color = AgentView.PastelColors[team];
                    }
                }
            }
        }
        
        public void RestartGame()
        {
            // Hide victory panel
            if (victoryPanel != null)
                victoryPanel.SetActive(false);
                
            // Reset engine
            Engine.Reset();
            
            // Start a new game
            StartGame();
        }
    }
}