using Examples.Unity.Cosmetic;
using SimToolAI.Core.Entities;
using SimToolAI.Core.Rendering;
using SimToolAI.Utilities;
using System.Collections.Generic;
using SimToolAI.Core;
using SimToolAI.Core.Rendering.RenderStrategies;
using UnityEngine;

namespace Examples.Unity.Managers
{
    /// <summary>
    /// Manages bullet pool and firing logic
    /// </summary>
    public class BulletManager : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private RenderableBridge bulletPrefab;
        [SerializeField] private Grid grid;
        [SerializeField] private float fireRate = 0.25f;
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private SimulationUnity simulationAgent;

        /// <summary>
        /// The bullet pool
        /// </summary>
        private readonly Queue<RenderableBridge> _bulletPool = new();

        /// <summary>
        /// The active bullets
        /// </summary>
        private readonly Dictionary<Bullet, GameObject> _activeBullets = new();

        /// <summary>
        /// Whether the player is firing
        /// </summary>
        private bool _isFiring;

        /// <summary>
        /// Time since the last shot
        /// </summary>
        private float _timeSinceLastShot;

        /// <summary>
        /// Initializes the bullet manager
        /// </summary>
        /// <param name="scene">The scene</param>
        /// <param name="simulation">The simulation</param>
        public void Initialize(UnityScene scene, Simulation simulation)
        {
            InitializeBulletPool();

            // Subscribe to the entity removed event to handle bullet recycling
            scene.EntityRemoved += OnEntityRemoved;

            simulation.OnCreate += (_, ent) =>
            {
                if (ent is Bullet bullet)
                {
                    bullet.MaxRange = 50;
                    CreateBulletObject(bullet);
                }
            };
        }

        /// <summary>
        /// Updates the bullet manager
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last update</param>
        /// <param name="player">The player</param>
        public void ManualUpdate(float deltaTime, Player player)
        {
            if (_isFiring && player != null)
            {
                _timeSinceLastShot += deltaTime;

                // Check if enough time has passed since the last shot
                if (_timeSinceLastShot >= fireRate)
                {
                    // Create a new bullet in the simulation
                    var bullet = new Bullet(player.X, player.Y, player.FacingDirection, player.Simulation, player)
                    {
                        FacingDirection = player.FacingDirection,
                        Speed = 10f,
                        Damage = player.AttackPower
                    };
                    
                    // Add the bullet to the simulation
                    player.Simulation.ProcessNewCreation(bullet);
                    
                    _timeSinceLastShot = 0f;
                }
            }
        }

        /// <summary>
        /// Starts firing
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="playerAnimations">The player animations</param>
        public void StartFiring(Player player, PlayerAnimations playerAnimations)
        {
            if (player == null)
                return;

            // Start continuous firing
            _isFiring = true;

            // Fire immediately on first press
            player.ProcessInput(null, true);
            _timeSinceLastShot = 0f;
            
            playerAnimations.TriggerAnimation(PlayerAnimations.SHOOT_ANIMATION);
        }

        /// <summary>
        /// Stops firing
        /// </summary>
        public void StopFiring()
        {
            // Stop continuous firing
            _isFiring = false;
        }

        /// <summary>
        /// Initializes the bullet pool
        /// </summary>
        private void InitializeBulletPool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                RenderableBridge bulletObj = Instantiate(bulletPrefab, transform);
                bulletObj.gameObject.SetActive(false);
                _bulletPool.Enqueue(bulletObj);
            }
        }

        /// <summary>
        /// Creates or reuses a Unity representation for a bullet
        /// </summary>
        /// <param name="bullet">The bullet</param>
        private void CreateBulletObject(Bullet bullet)
        {
            GameObject bulletObj;

            if (_bulletPool.Count > 0)
            {
                RenderableBridge bridge = _bulletPool.Dequeue();
                bullet.Avatar = bridge.GetRenderable(simulationAgent.SimulationAdapter.Simulation, bullet);
                bulletObj = bridge.gameObject;
            }
            else
            {
                RenderableBridge obj = Instantiate(bulletPrefab);
                bullet.Avatar = obj.GetRenderable(simulationAgent.SimulationAdapter.Simulation, bullet);
                bulletObj = obj.gameObject;
            }

            // Activate and position the bullet
            bulletObj.SetActive(true);
            bulletObj.transform.position = grid.GetCellCenterWorld(new Vector3Int(bullet.X, bullet.Y));
            bulletObj.name = $"Bullet_{bullet.Id}";

            // Set the bullet's rotation based on its direction
            float angle = DirectionVector.GetRotationAngle(bullet.FacingDirection);
            bulletObj.transform.rotation = Quaternion.Euler(0, 0, angle);

            // Track the active bullet
            _activeBullets[bullet] = bulletObj;

            // Set up the bullet's renderable
            Data bulletRenderableData = new Data();
            bulletRenderableData.Set("transform", bulletObj.transform);
            bulletRenderableData.Set("grid", grid);
            bulletRenderableData.Set("entity", bullet);

            //UnityEntityRenderable bulletRenderable = new UnityEntityRenderable(bulletRenderableData);
            //bullet.Avatar = bulletRenderable;
        }

        /// <summary>
        /// Handles entity removal events to recycle bullet objects
        /// </summary>
        private void OnEntityRemoved(object sender, EntityEventArgs e)
        {
            if (e.Entity is Bullet bullet && _activeBullets.TryGetValue(bullet, out GameObject bulletObj))
            {
                // Return the bullet to the pool
                bulletObj.SetActive(false);
                _bulletPool.Enqueue(bulletObj.GetComponent<RenderableBridge>());

                // Remove from active bullets
                _activeBullets.Remove(bullet);
            }
        }
    }
}