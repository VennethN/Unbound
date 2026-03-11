using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbound.Player;

namespace Unbound.UI
{
    /// <summary>
    /// UI component that displays the player's current health and maximum health.
    /// Automatically updates when health changes or damage is taken.
    /// </summary>
    public class HPDisplay : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text displaying current/max HP")]
        [SerializeField] private TextMeshProUGUI hpText;
        
        [Tooltip("Text displaying only current HP (optional)")]
        [SerializeField] private TextMeshProUGUI currentHpText;
        
        [Tooltip("Text displaying only max HP (optional)")]
        [SerializeField] private TextMeshProUGUI maxHpText;
        
        [Tooltip("Progress bar fill image (uses Image.fillAmount)")]
        [SerializeField] private Image hpBarFill;
        
        [Tooltip("Alternative: Slider for HP bar")]
        [SerializeField] private Slider hpBarSlider;
        
        [Header("Text Formatting")]
        [Tooltip("Format for HP text. {0} = current, {1} = max")]
        [SerializeField] private string hpFormat = "{0} / {1}";
        
        [Tooltip("Format for current HP only. {0} = current")]
        [SerializeField] private string currentHpFormat = "{0}";
        
        [Tooltip("Format for max HP only. {0} = max")]
        [SerializeField] private string maxHpFormat = "Max: {0}";
        
        [Tooltip("Show HP as percentage instead of numbers")]
        [SerializeField] private bool showAsPercentage = false;
        
        [Header("Animation")]
        [Tooltip("Animate the HP bar smoothly")]
        [SerializeField] private bool animateHpBar = true;
        
        [Tooltip("Speed of HP bar animation")]
        [SerializeField] private float animationSpeed = 5f;
        
        [Header("Damage Effect")]
        [Tooltip("Flash/pulse effect on damage taken")]
        [SerializeField] private bool enableDamageEffect = true;
        
        [Tooltip("Color to flash on damage")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
        
        [Tooltip("Duration of damage flash")]
        [SerializeField] private float flashDuration = 0.3f;
        
        [Header("Low Health Warning")]
        [Tooltip("Enable low health warning effect")]
        [SerializeField] private bool enableLowHealthWarning = true;
        
        [Tooltip("Health percentage threshold for low health warning (0-1)")]
        [SerializeField] private float lowHealthThreshold = 0.3f;
        
        [Tooltip("Color for low health warning")]
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        [Tooltip("Pulse speed for low health warning")]
        [SerializeField] private float lowHealthPulseSpeed = 2f;
        
        [Header("Colors")]
        [SerializeField] private Color normalHpBarColor = new Color(0.2f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color midHpBarColor = new Color(1f, 0.8f, 0.2f, 1f);
        [SerializeField] private Color lowHpBarColor = new Color(0.8f, 0.2f, 0.2f, 1f);
        
        [Tooltip("Health percentage threshold for mid health color (0-1)")]
        [SerializeField] private float midHealthThreshold = 0.5f;
        
        // Runtime
        private PlayerCombat playerCombat;
        private float targetFill = 1f;
        private float currentFill = 1f;
        private Color originalHpTextColor;
        private float flashTimer = 0f;
        private bool isFlashing = false;
        private float currentHealth = 100f;
        private float maxHealth = 100f;

        private void Start()
        {
            // Cache original color
            if (hpText != null)
            {
                originalHpTextColor = hpText.color;
            }
            
            // Find player combat component
            FindPlayerCombat();
            
            // Subscribe to events
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
            // Animate HP bar
            if (animateHpBar && Mathf.Abs(currentFill - targetFill) > 0.001f)
            {
                currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * animationSpeed);
                ApplyFillAmount(currentFill);
            }
            
            // Damage flash effect
            if (isFlashing)
            {
                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0f)
                {
                    isFlashing = false;
                    if (hpText != null)
                    {
                        hpText.color = originalHpTextColor;
                    }
                }
                else
                {
                    // Pulse effect
                    float t = flashTimer / flashDuration;
                    float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.5f + 0.5f;
                    if (hpText != null)
                    {
                        hpText.color = Color.Lerp(originalHpTextColor, damageFlashColor, pulse);
                    }
                }
            }
            
            // Low health warning effect
            if (enableLowHealthWarning && playerCombat != null)
            {
                float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
                if (healthPercent <= lowHealthThreshold)
                {
                    // Pulse effect for low health
                    float pulse = Mathf.Sin(Time.time * lowHealthPulseSpeed) * 0.3f + 0.7f;
                    Color warningColor = Color.Lerp(originalHpTextColor, lowHealthColor, pulse);
                    
                    if (hpText != null)
                    {
                        hpText.color = isFlashing ? hpText.color : warningColor;
                    }
                }
                else if (!isFlashing && hpText != null)
                {
                    hpText.color = originalHpTextColor;
                }
            }
        }

        private void FindPlayerCombat()
        {
            if (playerCombat == null)
            {
                playerCombat = FindFirstObjectByType<PlayerCombat>();
            }
        }

        private void SubscribeToEvents()
        {
            FindPlayerCombat();
            
            if (playerCombat != null)
            {
                playerCombat.OnHealthChanged += OnHealthChanged;
                playerCombat.OnDamageTaken += OnDamageTaken;
                
                // Get initial health values
                currentHealth = playerCombat.health;
                maxHealth = playerCombat.maxHealth;
            }
            else
            {
                // Retry after a short delay
                Invoke(nameof(RetrySubscription), 0.1f);
            }
        }

        private void RetrySubscription()
        {
            if (playerCombat == null)
            {
                FindPlayerCombat();
                if (playerCombat != null)
                {
                    playerCombat.OnHealthChanged += OnHealthChanged;
                    playerCombat.OnDamageTaken += OnDamageTaken;
                    
                    // Get initial health values
                    currentHealth = playerCombat.health;
                    maxHealth = playerCombat.maxHealth;
                    
                    UpdateDisplay();
                }
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (playerCombat != null)
            {
                playerCombat.OnHealthChanged -= OnHealthChanged;
                playerCombat.OnDamageTaken -= OnDamageTaken;
            }
        }

        private void OnHealthChanged(PlayerCombat player, float health)
        {
            currentHealth = health;
            maxHealth = player.maxHealth;
            UpdateDisplay();
        }

        private void OnDamageTaken(PlayerCombat player, float damage)
        {
            currentHealth = player.health;
            maxHealth = player.maxHealth;
            UpdateDisplay();
            
            if (enableDamageEffect)
            {
                TriggerDamageEffect();
            }
        }

        /// <summary>
        /// Updates all display elements
        /// </summary>
        public void UpdateDisplay()
        {
            if (playerCombat == null)
            {
                FindPlayerCombat();
                if (playerCombat == null)
                {
                    // Use cached values if player not found
                    UpdateDisplayWithValues(currentHealth, maxHealth);
                    return;
                }
                
                currentHealth = playerCombat.health;
                maxHealth = playerCombat.maxHealth;
            }
            else
            {
                currentHealth = playerCombat.health;
                maxHealth = playerCombat.maxHealth;
            }
            
            UpdateDisplayWithValues(currentHealth, maxHealth);
        }

        private void UpdateDisplayWithValues(float current, float max)
        {
            float healthPercent = max > 0 ? current / max : 0f;
            
            // Update HP text
            if (hpText != null)
            {
                if (showAsPercentage)
                {
                    hpText.text = string.Format("{0:P0}", healthPercent);
                }
                else
                {
                    hpText.text = string.Format(hpFormat, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
                }
            }
            
            // Update current HP text
            if (currentHpText != null)
            {
                if (showAsPercentage)
                {
                    currentHpText.text = string.Format("{0:P0}", healthPercent);
                }
                else
                {
                    currentHpText.text = string.Format(currentHpFormat, Mathf.CeilToInt(current));
                }
            }
            
            // Update max HP text
            if (maxHpText != null)
            {
                maxHpText.text = string.Format(maxHpFormat, Mathf.CeilToInt(max));
            }
            
            // Update HP bar
            targetFill = healthPercent;
            
            if (!animateHpBar)
            {
                currentFill = targetFill;
                ApplyFillAmount(currentFill);
            }
            
            // Update bar color
            UpdateBarColor(healthPercent);
        }

        private void ApplyFillAmount(float fill)
        {
            if (hpBarFill != null)
            {
                hpBarFill.fillAmount = fill;
            }
            
            if (hpBarSlider != null)
            {
                hpBarSlider.value = fill;
            }
        }

        private void UpdateBarColor(float healthPercent)
        {
            Color targetColor;
            
            if (healthPercent <= lowHealthThreshold)
            {
                targetColor = lowHpBarColor;
            }
            else if (healthPercent <= midHealthThreshold)
            {
                targetColor = midHpBarColor;
            }
            else
            {
                targetColor = normalHpBarColor;
            }
            
            if (hpBarFill != null)
            {
                hpBarFill.color = targetColor;
            }
        }

        private void TriggerDamageEffect()
        {
            isFlashing = true;
            flashTimer = flashDuration;
            
            // Scale punch effect
            if (hpText != null)
            {
                StartCoroutine(ScalePunchCoroutine(hpText.transform));
            }
        }

        private System.Collections.IEnumerator ScalePunchCoroutine(Transform target)
        {
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;
            float duration = 0.2f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Overshoot then settle
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
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
        /// Sets the HP text format
        /// </summary>
        public void SetHpFormat(string format)
        {
            hpFormat = format;
            UpdateDisplay();
        }

        /// <summary>
        /// Sets whether to show HP as percentage
        /// </summary>
        public void SetShowAsPercentage(bool show)
        {
            showAsPercentage = show;
            UpdateDisplay();
        }

        /// <summary>
        /// Triggers the damage effect manually
        /// </summary>
        public void PlayDamageEffect()
        {
            TriggerDamageEffect();
        }

        /// <summary>
        /// Manually sets the health values (useful for testing or non-player entities)
        /// </summary>
        public void SetHealth(float current, float max)
        {
            currentHealth = current;
            maxHealth = max;
            UpdateDisplayWithValues(currentHealth, maxHealth);
        }

        #endregion
    }
}
