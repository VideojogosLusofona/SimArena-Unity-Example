using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguesharpBased
{
    public class GameController : MonoBehaviour
    {
        [Header("TEST - Params")]
        public int agentsPerTeam = 5;
        public int numberOfTeams = 2;
        public int mapWidth;
        public int mapHeight;
        
        [Header("Agent Settings")]
        [Tooltip("Percentage of agents that will use hunter brain (0-100)")]
        [Range(0, 100)]
        public int hunterPercentage = 70;
        
        [Tooltip("Percentage of hunter agents that will be passive (flee instead of chase) (0-100)")]
        [Range(0, 100)]
        public int passivePercentage = 30;
        
        [Header("Elements")]
        public TilemapView mapView;
        public GameObject agentPrefab;
        
        [Header("UI")]
        public GameObject victoryPanel;
        public TMP_Text victoryText;
        public Button restartButton;
        
        private Dictionary<Agent, AgentView> _views = new();
        private bool _isGameRunning = false;
        
        public GameEngine Engine { get; set; }

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
        
        private void StartGame()
        {
            // Create new engine
            Engine = new GameEngine(mapWidth, mapHeight);
            
            // Subscribe to events
            Engine.OnAgentKilled += HandleAgentKilled;
            Engine.OnTeamWon += HandleTeamWon;
            
            // Initialize map view
            mapView.Init(Engine);
            
            // Clear any existing views
            foreach (var view in _views.Values)
            {
                if (view != null && view.gameObject != null)
                    Destroy(view.gameObject);
            }
            _views.Clear();
            
            // Create agents for each team
            for (int team = 0; team < numberOfTeams; team++)
            {
                for (int i = 0; i < agentsPerTeam; i++)
                {
                    (int x, int y) whereToSpawn = Brain.GetRandomWalkableLocation(Engine.Map);
                    
                    // Determine if this agent will use hunter brain
                    bool useHunterBrain = Random.Range(0, 100) < hunterPercentage;
                    
                    // Determine if this agent will be passive
                    bool isPassive = Random.Range(0, 100) < passivePercentage;
                    
                    var agent = new Agent(whereToSpawn.x, whereToSpawn.y, Engine.Map, team, Engine, useHunterBrain, isPassive);
                    Engine.AddAgent(agent);
                    
                    // Create visual representation
                    var go = Instantiate(agentPrefab);
                    var view = go.GetComponent<AgentView>();
                    view.Initialize(agent);
                    _views[agent] = view;
                }
            }
            
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