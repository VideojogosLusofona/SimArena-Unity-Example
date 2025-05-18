using UnityEngine;
using System.Collections.Generic;

namespace RoguesharpBased
{
    public class AgentView : MonoBehaviour
    {
        public Agent Agent { get; private set; }
        public float moveSpeed = 5f;
        public GameObject outline;
        
        private Vector3 targetPos;
        private bool _isMoving;
        private SpriteRenderer _spriteRenderer;
        
        // List of pastel colors - made public static so it can be accessed by GameController
        public static readonly List<Color> PastelColors = new()
        {
            new(0.8f, 0.6f, 0.8f), // Pastel Purple
            new(0.6f, 0.8f, 0.8f), // Pastel Blue
            new(0.8f, 0.8f, 0.6f), // Pastel Yellow
            new(0.6f, 0.8f, 0.6f), // Pastel Green
            new(0.8f, 0.6f, 0.6f), // Pastel Red
            new(0.9f, 0.7f, 0.5f), // Pastel Orange
            new(0.7f, 0.9f, 0.7f), // Mint Green
            new(0.9f, 0.8f, 0.9f), // Lavender
            new(0.8f, 0.9f, 0.9f), // Baby Blue
            new(0.9f, 0.9f, 0.8f) // Cream
        };

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            outline.SetActive(false);
        }

        public void Initialize(Agent agent)
        {
            Agent = agent;
            transform.position = GridToWorldPosition(agent.X, agent.Y);
            Agent.Brain.OnMove += HandleMove;
            
            if (_spriteRenderer != null)
            {
                // Use team index for color, with fallback to random if team index is out of range
                if (agent.Team < PastelColors.Count)
                {
                    _spriteRenderer.color = PastelColors[agent.Team];
                }
                else
                {
                    _spriteRenderer.color = GetRandomPastelColor();
                }
                
                // Make passive agents slightly transparent
                if (agent.Brain.IsPassive)
                {
                    outline.SetActive(true);
                }
            }
        }
        
        private Color GetRandomPastelColor()
        {
            return PastelColors[Random.Range(0, PastelColors.Count)];
        }
        
        private void HandleMove(Agent agent, int newX, int newY)
        {
            targetPos = GridToWorldPosition(newX, newY);
            _isMoving = true;
        }

        private void Update()
        {
            if (_isMoving)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, 
                    moveSpeed * Time.deltaTime);
                
                if (Vector3.Distance(transform.position, targetPos) < 0.01f)
                {
                    transform.position = targetPos;
                    _isMoving = false;
                }
            }
        }

        private Vector3 GridToWorldPosition(int x, int y)
        {
            return new Vector3(x + 0.5f, y + 0.5f, 0);
        }
    }
}