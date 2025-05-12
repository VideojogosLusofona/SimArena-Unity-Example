/*using Examples.Unity.Cosmetic;
using UnityEngine;

namespace Examples.Unity.Managers
{
    /// <summary>
    /// Manages player creation and movement
    /// </summary>
    public class PlayerManager : MonoBehaviour
    {
        [Header("Player Settings")]
        [SerializeField] private RenderableBridge playerPrefab;
        [SerializeField] private float moveSpeed = 5f;

        /// <summary>
        /// The player GameObject
        /// </summary>
        public GameObject PlayerObject { get; private set; }

        /// <summary>
        /// The player entity
        /// </summary>
        public Player Player { get; private set; }

        /// <summary>
        /// The player animations component
        /// </summary>
        public PlayerAnimations PlayerAnimations { get; private set; }

        /// <summary>
        /// The target position for the player
        /// </summary>
        private Vector3 _targetPos = Vector3.zero;

        /// <summary>
        /// Creates the player at a random walkable location
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="grid">The grid</param>
        /// <param name="simulation">The simulation</param>
        public void CreatePlayer(ISimMap map, Grid grid, Simulation simulation)
        {
            var startPos = map.GetRandomWalkableLocation() ?? (5, 5);

            RangedWeapon pistol = new RangedWeapon("Pistol", startPos.Item1, startPos.Item2, simulation);
            
            // Create a human-controlled player
            Player = new Player("Player", startPos.Item1, startPos.Item2, 1, 2, 
                10, simulation, new Weapon[] { pistol })
            {
                Health = 100,
                MaxHealth = 100,
                AttackPower = 10,
                Defense = 5,
                Speed = moveSpeed,
                FacingDirection = DirectionVector.Right
            };
            
            pistol.SetOwner(Player);

            // Initialize the target position to the player's starting position
            _targetPos = grid.GetCellCenterWorld(new Vector3Int(Player.X, Player.Y));

            RenderableBridge obj = Instantiate(playerPrefab);
            PlayerObject = obj.gameObject;
            Player.Avatar = obj.GetRenderable(simulation, Player);
            PlayerObject.transform.position = _targetPos;
            PlayerAnimations = PlayerObject.GetComponent<PlayerAnimations>();
            Player.InitializeCollider(map);

            // Data renderData = new Data();
            // renderData.Set("transform", PlayerObject.transform);
            // renderData.Set("grid", grid);
            // renderData.Set("entity", Player);
            //
            // UnityEntityRenderable playerRenderable = new UnityEntityRenderable(renderData);
            // Player.Avatar = playerRenderable;

            // The player is already added to the scene in the Simulation.CreateAgents method
            // We don't need to add it again, but we do need to ensure field of view is enabled
            map.ToggleFieldOfView(Player);
        }

        /// <summary>
        /// Sends movement input to the simulation
        /// </summary>
        /// <param name="moveDirection">The direction to move</param>
        /// <returns>True if input was processed, false otherwise</returns>
        public bool SendMovementInput(System.Numerics.Vector3 moveDirection)
        {
            if (Player != null && Player.IsHumanControlled)
            {
                // Just send the input to the player's brain
                // The actual movement will be handled by the simulation
                return Player.ProcessInput(moveDirection, false);
            }
            return false;
        }
        
        /// <summary>
        /// Updates the visual position of the player based on its logical position
        /// </summary>
        /// <param name="grid">The grid</param>
        public void UpdateVisualPosition(Grid grid)
        {
            if (Player != null && PlayerObject != null)
            {
                // Update the target position for visualization
                _targetPos = grid.GetCellCenterWorld(new Vector3Int(Player.X, Player.Y));
            }
        }

        /// <summary>
        /// Gets the current target position
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            return _targetPos;
        }

        /// <summary>
        /// Checks if the player has reached the target position
        /// </summary>
        /// <param name="grid">The grid</param>
        /// <returns>True if the player has reached the target position, false otherwise</returns>
        public bool HasReachedTargetPosition(Grid grid)
        {
            if (Player == null || PlayerObject == null)
                return false;

            Vector3 targetPosition = grid.GetCellCenterWorld(new Vector3Int(Player.X, Player.Y));
            float distanceToTarget = Vector3.Distance(PlayerObject.transform.position, targetPosition);

            return distanceToTarget < 0.1f;
        }
    }
}*/