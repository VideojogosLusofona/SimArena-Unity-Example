/*using SimArena.Core.Entities;
using UnityEngine;

namespace Examples.Unity.Collision
{
    /// <summary>
    /// Bridge between Unity GameObjects and SimToolAI entities
    /// </summary>
    public class EntityBridge : MonoBehaviour
    {
        /// <summary>
        /// The entity this GameObject represents
        /// </summary>
        public Entity Entity { get; private set; }
        
        /// <summary>
        /// Sets the entity for this bridge
        /// </summary>
        /// <param name="entity">The entity to set</param>
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
        
        /// <summary>
        /// Draws the collider in the scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            if (Entity?.Collider == null)
                return;
                
            // Draw the collider
            this.DrawCollider(FindAnyObjectByType<Grid>());
        }
    }
}*/