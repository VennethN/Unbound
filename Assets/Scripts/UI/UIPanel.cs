using UnityEngine;
using UnityEngine.Events;

namespace Unbound.UI
{
    /// <summary>
    /// Optional component for UI panels that adds additional functionality and events.
    /// Attach this to your panel GameObjects for enhanced control.
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [Header("Panel Settings")]
        [Tooltip("Unique identifier for this panel")]
        [SerializeField] private string panelId;
        
        [Tooltip("Should this panel be active on start?")]
        [SerializeField] private bool activeOnStart = false;
        
        [Header("Animation Settings")]
        [Tooltip("Animate this panel when showing/hiding")]
        [SerializeField] private bool useAnimation = false;
        
        [Tooltip("Animation duration in seconds")]
        [SerializeField] private float animationDuration = 0.3f;
        
        [SerializeField] private AnimationType showAnimation = AnimationType.FadeIn;
        [SerializeField] private AnimationType hideAnimation = AnimationType.FadeOut;
        
        [Header("Events")]
        public UnityEvent onPanelShown;
        public UnityEvent onPanelHidden;
        
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private bool isAnimating = false;
        private bool isInitialized = false;
        
        public enum AnimationType
        {
            None,
            FadeIn,
            FadeOut,
            SlideInLeft,
            SlideInRight,
            SlideInTop,
            SlideInBottom,
            Scale
        }
        
        public string PanelId => panelId;
        public bool IsActive => gameObject.activeSelf;
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            if (activeOnStart)
            {
                Show();
            }
            else if (gameObject.activeSelf)
            {
                // Only call Hide() if the panel is currently active
                // If it's already inactive, just leave it
                Hide();
            }
        }
        
        private void InitializeComponents()
        {
            if (isInitialized) return;
            
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && useAnimation)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            rectTransform = GetComponent<RectTransform>();
            isInitialized = true;
        }
        
        /// <summary>
        /// Show the panel with optional animation
        /// </summary>
        public void Show()
        {
            if (isAnimating) return;
            
            // Ensure components are initialized (important if panel starts inactive)
            if (!isInitialized)
            {
                InitializeComponents();
            }
            
            gameObject.SetActive(true);
            
            if (useAnimation)
            {
                PlayAnimation(showAnimation, true);
            }
            
            onPanelShown?.Invoke();
        }
        
        /// <summary>
        /// Hide the panel with optional animation
        /// </summary>
        public void Hide()
        {
            if (isAnimating) return;
            
            // Ensure components are initialized (important if panel starts inactive)
            if (!isInitialized)
            {
                InitializeComponents();
            }
            
            if (useAnimation)
            {
                PlayAnimation(hideAnimation, false);
            }
            else
            {
                gameObject.SetActive(false);
            }
            
            onPanelHidden?.Invoke();
        }
        
        /// <summary>
        /// Toggle the panel visibility
        /// </summary>
        public void Toggle()
        {
            if (IsActive)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
        
        private void PlayAnimation(AnimationType animType, bool showing)
        {
            if (animType == AnimationType.None)
            {
                if (!showing) gameObject.SetActive(false);
                return;
            }

            // Safety check: if GameObject is inactive, can't start coroutines
            // Just set the final state immediately
            if (!gameObject.activeInHierarchy)
            {
                if (showing)
                {
                    // Set final shown state
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                    transform.localScale = Vector3.one;
                    rectTransform.anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Set final hidden state
                    if (canvasGroup != null) canvasGroup.alpha = 0f;
                    transform.localScale = Vector3.zero;
                    // Don't set position for hide when inactive (would cause issues)
                }
                return;
            }

            isAnimating = true;

            switch (animType)
            {
                case AnimationType.FadeIn:
                case AnimationType.FadeOut:
                    StartCoroutine(FadeAnimation(showing));
                    break;

                case AnimationType.SlideInLeft:
                case AnimationType.SlideInRight:
                case AnimationType.SlideInTop:
                case AnimationType.SlideInBottom:
                    StartCoroutine(SlideAnimation(animType, showing));
                    break;

                case AnimationType.Scale:
                    StartCoroutine(ScaleAnimation(showing));
                    break;
            }
        }
        
        private System.Collections.IEnumerator FadeAnimation(bool fadeIn)
        {
            if (canvasGroup == null) yield break;
            
            float elapsed = 0f;
            float startAlpha = fadeIn ? 0f : 1f;
            float endAlpha = fadeIn ? 1f : 0f;
            
            canvasGroup.alpha = startAlpha;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                yield return null;
            }
            
            canvasGroup.alpha = endAlpha;
            isAnimating = false;
            
            if (!fadeIn)
            {
                gameObject.SetActive(false);
            }
        }
        
        private System.Collections.IEnumerator SlideAnimation(AnimationType direction, bool slideIn)
        {
            if (rectTransform == null) yield break;
            
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector2 endPos = Vector2.zero;
            
            if (!slideIn)
            {
                // Slide out - determine direction from slide in type
                switch (direction)
                {
                    case AnimationType.SlideInLeft:
                        endPos = new Vector2(-rectTransform.rect.width, startPos.y);
                        break;
                    case AnimationType.SlideInRight:
                        endPos = new Vector2(rectTransform.rect.width, startPos.y);
                        break;
                    case AnimationType.SlideInTop:
                        endPos = new Vector2(startPos.x, rectTransform.rect.height);
                        break;
                    case AnimationType.SlideInBottom:
                        endPos = new Vector2(startPos.x, -rectTransform.rect.height);
                        break;
                }
            }
            else
            {
                // Slide in
                switch (direction)
                {
                    case AnimationType.SlideInLeft:
                        startPos = new Vector2(-rectTransform.rect.width, endPos.y);
                        break;
                    case AnimationType.SlideInRight:
                        startPos = new Vector2(rectTransform.rect.width, endPos.y);
                        break;
                    case AnimationType.SlideInTop:
                        startPos = new Vector2(endPos.x, rectTransform.rect.height);
                        break;
                    case AnimationType.SlideInBottom:
                        startPos = new Vector2(endPos.x, -rectTransform.rect.height);
                        break;
                }
                rectTransform.anchoredPosition = startPos;
            }
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = EaseOutCubic(t);
                rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }
            
            rectTransform.anchoredPosition = endPos;
            isAnimating = false;
            
            if (!slideIn)
            {
                gameObject.SetActive(false);
            }
        }
        
        private System.Collections.IEnumerator ScaleAnimation(bool scaleIn)
        {
            Vector3 startScale = scaleIn ? Vector3.zero : Vector3.one;
            Vector3 endScale = scaleIn ? Vector3.one : Vector3.zero;
            
            transform.localScale = startScale;
            
            float elapsed = 0f;
            
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animationDuration;
                t = EaseOutBack(t);
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            
            transform.localScale = endScale;
            isAnimating = false;
            
            if (!scaleIn)
            {
                gameObject.SetActive(false);
            }
        }
        
        // Easing functions
        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }
        
        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}

