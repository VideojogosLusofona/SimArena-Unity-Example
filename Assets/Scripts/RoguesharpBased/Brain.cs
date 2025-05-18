using System;
using RogueSharp;

namespace RoguesharpBased
{
    public abstract class Brain
    {
        protected readonly Agent _agent;
        protected readonly IMap _map;
        protected readonly int _tickIntervalMs;
        protected readonly Random _random = new();
        protected DateTime _lastDecisionTime;
        
        public int Team { get; }
        public bool IsPassive { get; set; }
        
        public event Action<Agent, int, int>? OnMove;
        
        protected Brain(Agent agent, IMap map, int team, bool isPassive = false, int tickIntervalMs = 500)
        {
            _agent = agent;
            _map = map;
            _tickIntervalMs = tickIntervalMs;
            Team = team;
            IsPassive = isPassive;
        }
        
        public virtual void Think()
        {
            // Check if tick interval has passed since last time we made a decision
            if ((DateTime.UtcNow - _lastDecisionTime).TotalMilliseconds < _tickIntervalMs)
                return;
            
            // If so then continue to this
            _lastDecisionTime = DateTime.UtcNow;
            
            // Execute brain-specific logic
            ExecuteThink();
        }
        
        protected abstract void ExecuteThink();
        
        protected void MoveTo(int newX, int newY)
        {
            if (_map.IsWalkable(newX, newY))
            {
                var current = _map.GetCell(_agent.X, _agent.Y);
                _map.SetCellProperties(_agent.X, _agent.Y, current.IsTransparent, true);
               
                _agent.X = newX;
                _agent.Y = newY;
                
                var newer = _map.GetCell(newX, newY);
                _map.SetCellProperties(newX, newY, newer.IsTransparent, false);
                
                OnMove?.Invoke(_agent, newX, newY);
            }
        }
        
        public static (int x, int y) GetRandomWalkableLocation(IMap map, Agent agent = null)
        {
            var minX = 0;
            var maxX = map.Width - 1;
            var minY = 0;
            var maxY = map.Height - 1;
            
            // Ensure bounds are within map limits
            minX = Math.Clamp(minX, 0, map.Width - 1);
            maxX = Math.Clamp(maxX, 0, map.Width - 1);
            minY = Math.Clamp(minY, 0, map.Height - 1);
            maxY = Math.Clamp(maxY, 0, map.Height - 1);

            // Check if there's any walkable space in the area
            bool hasWalkableSpace = false;
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (agent != null)
                    {
                        if (x == agent.X && y == agent.Y)
                        {
                            continue;
                        }
                    }
                    
                    if (map.IsWalkable(x, y))
                    {
                        hasWalkableSpace = true;
                        break;
                    }
                }
                
                if (hasWalkableSpace) 
                    break;
            }

            if (!hasWalkableSpace)
            {
                if (agent != null)
                {
                    return (agent.X, agent.Y);
                }
                else
                {
                    throw new Exception("No place available to move to.");
                }
            }
        
            Random rand = new();
            
            // Try to find a random walkable location
            for (int i = 0; i < 100; i++)
            {
                int x = rand.Next(minX, maxX + 1);
                int y = rand.Next(minY, maxY + 1);

                if (map.IsWalkable(x, y))
                    return (x, y);
            }

            throw new InvalidOperationException("Could not find a walkable location after 100 attempts.");
        }
    }
}