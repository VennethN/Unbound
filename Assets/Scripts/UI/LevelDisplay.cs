using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbound.Player;

namespace Unbound.UI
{
    /// <summary>
    /// UI component that displays the player's current level and experience progress.
    /// Automatically updates when exp is gained or level changes.
    /// </summary>
    public class LevelDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text displaying the current level number")]
        [SerializeField] private TextMeshProUGUI levelText;
        
        [Tooltip("Text displaying current/needed exp (optional)")]
        [SerializeField] private TextMeshProUGUI expText;
        
        [Tooltip("Progress bar fill image (uses Image.fillAmount)")]
        [SerializeField] private Image expBarFill;
        
        [Tooltip("Alternative: Slider for exp bar")]
        [SerializeField] private Slider expBarSlider;
        
        [Header("Text Formatting")]
        [Tooltip("Format for level text. {0} = level number")]
        [SerializeField] private string levelFormat = "Lv. {0}";
        
        [Tooltip("Format for exp text. {0} = current, {1} = needed")]
        [SerializeField] private string expFormat = "{0} / {1}";
        
        [Tooltip("Show 'MAX' when at max level")]
        [SerializeField] private bool showMaxAtMaxLevel = true;
        
        [Header("Animation")]
        [Tooltip("Animate the exp bar smoothly")]
        [SerializeField] private bool animateExpBar = true;
        
        [Tooltip("Speed of exp bar animation")]
        [SerializeField] private float animationSpeed = 5f;
        
        [Header("Level Up Effect")]
        [Tooltip("Flash/pulse effect on level up")]
        [SerializeField] private bool enableLevelUpEffect = true;
        
        [Tooltip("Color to flash on level up")]
        [SerializeField] private Color levelUpFlashColor = new Color(1f, 0.85f, 0.2f, 1f);
        
        [Tooltip("Duration of level up flash")]
        [SerializeField] private float flashDuration = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color normalExpBarColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color fullExpBarColor = new Color(0.4f, 1f, 0.4f, 1f);
        
        // Runtime
        private LevelingSystem levelingSystem;
        private float targetFill = 0f;
        private float currentFill = 0f;
        private Color originalLevelTextColor;
        private float flashTimer = 0f;
        private bool isFlashing = false;

        private void Start()
        {
            // Cache original color
            if (levelText != null)
            {
                originalLevelTextColor = levelText.color;
            }
            
            // Subscribe to leveling events
            SubscribeToEvents();
            
            // Initial update
            UpdateDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void Update()
        {
            // Animate exp bar
            if (animateExpBar && Mathf.Abs(currentFill - targetFill) > 0.001f)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * animationSpeed);
                ApplyFillAmount(currentFill);
            }
            
            // Level up flash effect
            if (isFlashing)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    isFlashing = false;
                    if (levelText != null)
                    {
                        levelText.color = originalLevelTextColor;
                    }
                }
                else
                {
                    // Pulse effect
                    float t = flashTimer / flashDuration;
                    float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;
                    if (levelText != null)
                    {
                        levelText.color = Color.Lerp(originalLevelTextColor, levelUpFlashColor, pulse);
                    }
                }
            }
        }

        private void SubscribeToEvents()
        {
            levelingSystem = LevelingSystem.Instance;
            if (levelingSystem != null)
            {
                levelingSystem.OnExpChanged.AddListener(OnExpChanged);
                levelingSystem.OnLevelUp.AddListener(OnLevelUp);
            }
            else
            {
                // Retry after a short delay
                Invoke(nameof(RetrySubscription), 0.1f);
            }
        }

        private void RetrySubscription()
        {
            if (levelingSystem == null)
            {
                levelingSystem = LevelingSystem.Instance;
                if (levelingSystem != null)
                {
                    levelingSystem.OnExpChanged.AddListener(OnExpChanged);
                    levelingSystem.OnLevelUp.AddListener(OnLevelUp);
                    UpdateDisplay();
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (levelingSystem != null)
            {
                levelingSystem.OnExpChanged.RemoveListener(OnExpChanged);
                levelingSystem.OnLevelUp.RemoveListener(OnLevelUp);
            }
        }

        private void OnExpChanged(int currentExp, int expToNext, int level)
        {
            UpdateDisplay();
        }

        private void OnLevelUp(int newLevel)
        {
            UpdateDisplay();
            
            if (enableLevelUpEffect)
            {
                TriggerLevelUpEffect();
            }
        }

        /// <summary>
        /// Updates all display elements
        /// </summary>
        public void UpdateDisplay()
        {
            if (levelingSystem == null)
            {
                levelingSystem = LevelingSystem.Instance;
                if (levelingSystem == null) return;
            }
            
            int level = levelingSystem.CurrentLevel;
            int currentExp = levelingSystem.CurrentExp;
            int expToNext = levelingSystem.ExpToNextLevel;
            float progress = levelingSystem.LevelProgress;
            bool isMaxLevel = level >= levelingSystem.MaxLevel;
            
            // Update level text
            if (levelText != null)
            {
                levelText.text = string.Format(levelFormat, level);
            }
            
            // Update exp text
            if (expText != null)
            {
                if (isMaxLevel && showMaxAtMaxLevel)
                {
                    expText.text = "MAX";
                }
                else
                {
                    // Calculate exp within current level
                    int expForCurrentLevel = levelingSystem.GetExpForLevel(level);
                    int expIntoLevel = currentExp - expForCurrentLevel;
                    int expNeededForLevel = expToNext - expForCurrentLevel;
                    expText.text = string.Format(expFormat, expIntoLevel, expNeededForLevel);
                }
            }
            
            // Update exp bar
            targetFill = isMaxLevel ? 1f : progress;
            
            if (!animateExpBar)
            {
                currentFill = targetFill;
                ApplyFillAmount(currentFill);
            }
            
            // Update bar color
            UpdateBarColor(isMaxLevel);
        }

        private void ApplyFillAmount(float fill)
        {
            if (expBarFill != null)
            {
                expBarFill.fillAmount = fill;
            }
            
            if (expBarSlider != null)
            {
                expBarSlider.value = fill;
            }
        }

        private void UpdateBarColor(bool isMaxLevel)
        {
            Color targetColor = isMaxLevel ? fullExpBarColor : normalExpBarColor;
            
            if (expBarFill != null)
            {
                expBarFill.color = targetColor;
            }
        }

        private void TriggerLevelUpEffect()
        {
            isFlashing = true;
            flashTimer = flashDuration;
            
            // Scale punch effect
            if (levelText != null)
            {
                StartCoroutine(ScalePunchCoroutine(levelText.transform));
            }
        }

        private System.Collections.IEnumerator ScalePunchCoroutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Overshoot then settle
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
                target.localScale = originalScale * scale;
                
                yield return null;
            }
            
            target.localScale = originalScale;
        }

        #region Public API

        /// <summary>
        /// Forces a display refresh
        /// </summary>
        public void Refresh()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// Sets the level text format
        /// </summary>
        public void SetLevelFormat(string format)
        {
            levelFormat = format;
            UpdateDisplay();
        }

        /// <summary>
        /// Sets the exp text format
        /// </summary>
        public void SetExpFormat(string format)
        {
            expFormat = format;
            UpdateDisplay();
        }

        /// <summary>
        /// Triggers the level up effect manually
        /// </summary>
        public void PlayLevelUpEffect()
        {
            TriggerLevelUpEffect();
        }

        #endregion
    }
}

