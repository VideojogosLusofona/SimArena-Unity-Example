/*using Examples.Unity.Managers;
using SimArena.Core;
using SimArena.Core.Entities;
using SimArena.Core.SimulationElements.Map;
using UnityEditor;
using UnityEngine;

namespace Examples.Unity.Collision
{
    /// <summary>
    /// Example class demonstrating the use of the collision system
    /// </summary>
    public class CollisionExample : MonoBehaviour
    {
        [Header("Entity Settings")] [SerializeField]
        private int entityWidth = 2;

        [SerializeField] private int entityHeight = 2;

        [Header("Simulation Settings")] [SerializeField]
        private SimulationLoader simulation;

        [SerializeField] private Grid grid;
        [SerializeField] private EntityBridge entityPrefab;

        [Header("Debug")] [SerializeField] private bool showColliders = true;

        private void Start()
        {
            if (simulation != null)
            {
                simulation.EntityCreatedEvent += OnEntityCreated;
            }
        }

        private void OnEntityCreated(GameObject go, Entity ent)
        {
            if (go.TryGetComponent<EntityBridge>(out var bridge))
            {
                bridge.SetEntity(ent);
            }
            else
            {
                EntityBridge ebridge = go.AddComponent<EntityBridge>();
                ebridge.SetEntity(ent);
            }
        }

        /// <summary>
        /// Creates an entity with a custom size
        /// </summary>
        /// <param name="simulation">The simulation instance</param>
        /// <param name="map">The map instance</param>
        /// <param name="x">X-coordinate</param>
        /// <param name="y">Y-coordinate</param>
        /// <param name="width">Width in grid cells</param>
        /// <param name="height">Height in grid cells</param>
        /// <returns>The created entity</returns>
        public static Entity CreateLargeEntity(Simulation simulation, IMap map, int x, int y, int width, int height)
        {
            // Create a character with the specified size
            var entity = new Character($"LargeEntity_{width}x{height}", x, y, simulation, width, height);

            // Add the entity to the simulation
            simulation.ProcessNewCreation(entity);

            return entity;
        }
    }

    /// <summary>
    /// Extension class for EntityBridge to support drawing colliders
    /// </summary>
    public static class EntityBridgeExtensions
    {
        /// <summary>
        /// Draws the collider for an entity
        /// </summary>
        /// <param name="entityBridge">The entity bridge</param>
        public static void DrawCollider(this EntityBridge entityBridge, Grid grid)
        {
            if (entityBridge.Entity?.Collider == null)
                return;
                
            // Get the occupied cells
            var cells = entityBridge.Entity.Collider.GetOccupiedCells();
            
            // Draw a box for each cell
            foreach (var cell in cells)
            {
                // Convert grid position to world position
                Vector3Int worldPos = new Vector3Int(cell.x, cell.y, 0);
                Vector3 pos = grid.GetCellCenterWorld(worldPos);
                
                // Draw a box at the cell position
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(pos, new Vector3(1, 1, 0.1f));
            }
        }
    }
    
    [CustomEditor(typeof(CollisionExample))]
    class CollisionExampleEditor : Editor 
    {
        public override void OnInspectorGUI() 
        {
            // Draw default inspector GUI
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Create Entity"))
            {
                CollisionExample example = target as CollisionExample;
                example?.CreateLargeEntity();
            }
        }
    }
}*/