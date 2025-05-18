using System.Linq;
using RogueSharp;

namespace RoguesharpBased
{
    public class RandomBrain : Brain
    {
        public RandomBrain(Agent agent, IMap map, int team, bool isPassive = false, int tickIntervalMs = 500) 
            : base(agent, map, team, isPassive, tickIntervalMs)
        {
        }

        protected override void ExecuteThink()
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
            
            var pathFinder = new PathFinder(_map);
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