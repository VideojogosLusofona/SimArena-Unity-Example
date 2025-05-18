using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;

namespace RoguesharpBased
{
    public class HunterBrain : Brain
    {
        private readonly GameEngine _gameEngine;
        private const int DetectionRange = 10; // How far the agent can "see" other agents
        
        public HunterBrain(Agent agent, IMap map, GameEngine gameEngine, int team, bool isPassive = false, int tickIntervalMs = 500) 
            : base(agent, map, team, isPassive, tickIntervalMs)
        {
            _gameEngine = gameEngine;
        }

        protected override void ExecuteThink()
        {
            // Find the nearest enemy agent
            var nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy == null)
            {
                // No enemies found, move randomly
                MoveRandomly();
                return;
            }
            
            // Calculate distance to the nearest enemy
            int distanceToEnemy = CalculateManhattanDistance(_agent.X, _agent.Y, 
                nearestEnemy.X, nearestEnemy.Y);
            
            // Check if we're adjacent to the enemy
            if (distanceToEnemy <= 1)
            {
                if (!IsPassive)
                {
                    // We're adjacent to the enemy and we're aggressive, so kill it
                    _gameEngine.KillAgent(nearestEnemy);
                    return;
                }
            }
            
            // We're not adjacent to the enemy
            if (!IsPassive)
            {
                // We're aggressive, so chase the enemy
                MoveTowardsAgent(nearestEnemy);
            }
            else if (distanceToEnemy <= DetectionRange)
            {
                // We're passive and the enemy is within detection range, so flee
                FleeFromAgent(nearestEnemy);
            }
            else
            {
                // We're passive and the enemy is far away, so move randomly
                MoveRandomly();
            }
        }
        
        private Agent FindNearestEnemy()
        {
            Agent nearestEnemy = null;
            int shortestDistance = int.MaxValue;
            
            foreach (var agent in _gameEngine.Agents)
            {
                // Skip self and agents on the same team
                if (agent == _agent || agent.Brain.Team == Team || !agent.IsAlive)
                    continue;
                
                int distance = CalculateManhattanDistance(_agent.X, _agent.Y, agent.X, agent.Y);
                
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    nearestEnemy = agent;
                }
            }
            
            return nearestEnemy;
        }
        
        private int CalculateManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2);
        }
        
        private void MoveTowardsAgent(Agent target)
        {
            var pathFinder = new IgnorantPathfinder(_map, new []{_map.GetCell(target.X, target.Y), 
                _map.GetCell(_agent.X, _agent.Y)});
            
            var path = pathFinder.ShortestPath(
                _map.GetCell(_agent.X, _agent.Y), 
                _map.GetCell(target.X, target.Y));
            
            if (path is { Length: > 1 })
            {
                var nextStep = path.StepForward();
                MoveTo(nextStep.X, nextStep.Y);
            }
            else
            {
                // If no path is found, move randomly
                MoveRandomly();
            }
        }
        
        private void FleeFromAgent(Agent threat)
        {
            // Get all walkable neighbors
            var neighbors = _map.GetBorderCellsInSquare(_agent.X, _agent.Y, 1)
                .Where(c => c.IsWalkable)
                .ToList();
            
            if (neighbors.Count == 0)
            {
                // No walkable neighbors, try to find a path to a random location
                TryFindNewWalkableCell();
                return;
            }
            
            // Calculate the direction away from the threat
            int dirX = _agent.X - threat.X;
            int dirY = _agent.Y - threat.Y;
            
            // Find the neighbor that maximizes distance from the threat
            ICell bestCell = null;
            int maxDistance = -1;
            
            foreach (var cell in neighbors)
            {
                // Calculate the potential new distance if we move to this cell
                int newDistance = CalculateManhattanDistance(cell.X, cell.Y, threat.X, threat.Y);
                
                // Prefer cells that are in the direction away from the threat
                int directionBonus = 0;
                if ((dirX > 0 && cell.X > _agent.X) || (dirX < 0 && cell.X < _agent.X))
                    directionBonus += 2;
                if ((dirY > 0 && cell.Y > _agent.Y) || (dirY < 0 && cell.Y < _agent.Y))
                    directionBonus += 2;
                
                int score = newDistance + directionBonus;
                
                if (score > maxDistance)
                {
                    maxDistance = score;
                    bestCell = cell;
                }
            }
            
            if (bestCell != null)
            {
                MoveTo(bestCell.X, bestCell.Y);
            }
            else
            {
                // Fallback to random movement
                MoveRandomly();
            }
        }
        
        private void MoveRandomly()
        {
            var (x, y) = (_agent.X, _agent.Y);
            
            var neighbors = _map.GetBorderCellsInSquare(x, y, 1).Where(c => c.IsWalkable).ToArray();

            if (neighbors.Length > 0)
            {
                var choice = neighbors[_random.Next(neighbors.Length)];
                MoveTo(choice.X, choice.Y);
            }
            else
            {
                TryFindNewWalkableCell();
            }
        }
        
        private void TryFindNewWalkableCell()
        {
            var goal = GetRandomWalkableLocation(_map, _agent);
            
            var pathFinder = new IgnorantPathfinder(_map, new []{_map.GetCell(goal.x, goal.y), 
                _map.GetCell(_agent.X, _agent.Y)});
            
            var path = pathFinder.ShortestPath(_map.GetCell(_agent.X, _agent.Y), 
                _map.GetCell(goal.x, goal.y));
            
            if (path is { Length: > 1 })
            {
                var nextStep = path.StepForward();
                MoveTo(nextStep.X, nextStep.Y);
            }
        }
    }
}