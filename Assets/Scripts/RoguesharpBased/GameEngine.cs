using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using RogueSharp.MapCreation;

namespace RoguesharpBased
{
    public class GameEngine
    {
        public Map Map { get; }
        public List<Agent> Agents { get; } = new();
        public bool IsGameOver { get; private set; }
        public int WinningTeam { get; private set; } = -1;
        
        public event Action<Agent> OnAgentKilled;
        public event Action<int> OnTeamWon;
        
        public GameEngine(int width, int height)
        {
            var generator = new RandomRoomsMapCreationStrategy<Map>(width, height, 10, 
                6, 6);
            Map = generator.CreateMap();
        }

        public void AddAgent(Agent agent)
        {
            Agents.Add(agent);
        }

        public void Update()
        {
            if (IsGameOver)
                return;
                
            // Only update living agents
            foreach (var agent in Agents.Where(a => a.IsAlive))
            {
                agent.Brain.Think();
            }
            
            // Check for victory conditions
            CheckVictoryConditions();
        }
        
        public void KillAgent(Agent agent)
        {
            if (!agent.IsAlive)
                return;
                
            agent.Kill();
            
            // Make the cell walkable again
            var cell = Map.GetCell(agent.X, agent.Y);
            Map.SetCellProperties(agent.X, agent.Y, cell.IsTransparent, true);
            
            OnAgentKilled?.Invoke(agent);
        }
        
        private void CheckVictoryConditions()
        {
            // Get all teams that still have living agents
            var remainingTeams = Agents
                .Where(a => a.IsAlive)
                .Select(a => a.Team)
                .Distinct()
                .ToList();
                
            // If only one team remains, they win
            if (remainingTeams.Count == 1)
            {
                WinningTeam = remainingTeams[0];
                IsGameOver = true;
                OnTeamWon?.Invoke(WinningTeam);
            }
            // If no teams remain (shouldn't happen normally), it's a draw
            else if (remainingTeams.Count == 0)
            {
                IsGameOver = true;
                WinningTeam = -1; // No winner
            }
        }
        
        public void Reset()
        {
            IsGameOver = false;
            WinningTeam = -1;
            
            // Clear all agents
            Agents.Clear();
        }
    }
}