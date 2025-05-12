using DG.Tweening;
using UnityEngine;
using TMPro;

namespace Examples
{
    public class KillFeedItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI killFeedText;
        [SerializeField] private Color killerColor = Color.green;
        [SerializeField] private Color killedColor = Color.red;
        
        [Header("Animation Settings")]
        [SerializeField] private float animationDuration = 0.5f;
        [SerializeField] private float slideDistance = 100f;
        [SerializeField] private Ease easeTypeIn = Ease.OutBack;
        [SerializeField] private Ease easeTypeOut = Ease.InBack;
        
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            
            // Add CanvasGroup if it doesn't exist
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        public void SetKillInfo(string killerName, string killedName)
        {
            if (killFeedText != null)
            {
                string killerColorHex = ColorUtility.ToHtmlStringRGB(killerColor);
                string killedColorHex = ColorUtility.ToHtmlStringRGB(killedColor);
                
                killFeedText.text = $"<color=#{killerColorHex}>{killerName}</color> killed <color=#{killedColorHex}>{killedName}</color>";
            }
        }
        
        /// <summary>
        /// Animates the kill feed item into view using DOTween
        /// </summary>
        public void AnimateIn()
        {
            // Kill any existing animations
            DOTween.Kill(this.transform);
            
            // Reset position and opacity
            canvasGroup.alpha = 0f;
            Vector2 startPosition = rectTransform.anchoredPosition;
            rectTransform.anchoredPosition = new Vector2(startPosition.x + slideDistance, startPosition.y);
            
            // Create animation sequence
            Sequence sequence = DOTween.Sequence();
            
            // Slide in from right
            sequence.Append(rectTransform.DOAnchorPosX(startPosition.x, animationDuration).SetEase(easeTypeIn));
            
            // Fade in
            sequence.Join(canvasGroup.DOFade(1f, animationDuration * 0.8f));
            
            // Play the sequence
            sequence.Play();
        }
        
        /// <summary>
        /// Animates the kill feed item out of view using DOTween
        /// </summary>
        /// <param name="destroyAfter">Whether to destroy the GameObject after animation</param>
        public void AnimateOut(bool destroyAfter = true)
        {
            // Kill any existing animations
            DOTween.Kill(this.transform);
            
            // Create animation sequence
            Sequence sequence = DOTween.Sequence();
            
            // Slide out to left
            sequence.Append(rectTransform.DOAnchorPosX(rectTransform.anchoredPosition.x - slideDistance, animationDuration).SetEase(easeTypeOut));
            
            // Fade out
            sequence.Join(canvasGroup.DOFade(0f, animationDuration * 0.8f));
            
            // Destroy after animation if requested
            if (destroyAfter)
            {
                sequence.OnComplete(() => Destroy(gameObject));
            }
            
            // Play the sequence
            sequence.Play();
        }
    }
}