using System;
using RogueSharp;

namespace RoguesharpBased
{
    public class Agent
    {
        private int _x;
        private int _y;
    
        public int X 
        { 
            get => _x;
            set => _x = value;
        }
    
        public int Y 
        { 
            get => _y;
            set => _y = value;
        }

        public Brain Brain { get; private set; }
        public Guid Id { get; }
        public int Team { get; }
        public bool IsAlive { get; private set; } = true;

        public Agent(int x, int y, IMap map, int team, GameEngine gameEngine = null, bool useHunterBrain = false, bool isPassive = false, int tickMs = 500)
        {
            _x = x;
            _y = y;
            Team = team;
            Id = Guid.NewGuid();
        
            if (useHunterBrain && gameEngine != null)
            {
                Brain = new HunterBrain(this, map, gameEngine, team, isPassive, tickMs);
            }
            else
            {
                Brain = new RandomBrain(this, map, team, isPassive, tickMs);
            }
        }
    
        public void SetBrain(Brain brain)
        {
            Brain = brain;
        }
    
        public void Kill()
        {
            IsAlive = false;
        }
    }
}