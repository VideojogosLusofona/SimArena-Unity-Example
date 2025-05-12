using Examples.Unity.Cosmetic;
using UnityEngine;
using SimArena.Core.Entities;
using TMPro;

namespace Examples
{
    public class EntityVisualizer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private HealthBar healthBar;
        
        private Entity entity;
        
        public void Initialize(Entity entity)
        {
            this.entity = entity;
            
            // Set name
            if (nameText != null)
            {
                nameText.text = entity.Name;
            }
            
            // Set initial health if it's a character
            if (entity is Character character)
            {
                UpdateHealth(character.Health, character.MaxHealth);
            }
            
            // Assign random color to differentiate entities
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(
                    Random.Range(0.5f, 1.0f),
                    Random.Range(0.5f, 1.0f),
                    Random.Range(0.5f, 1.0f)
                );
            }
        }
        
        public void UpdateHealth(int currentHealth, int maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.SetMaxHealth(maxHealth);
                healthBar.SetHealth(currentHealth);
            }
        }
        
        private void Update()
        {
            // Make UI elements face the camera
            if (nameText != null && nameText.transform.parent != null)
            {
                nameText.transform.parent.rotation = Quaternion.identity;
            }
        }
    }
}