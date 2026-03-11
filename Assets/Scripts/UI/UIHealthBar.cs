using UnityEngine;
using UnityEngine.UI;

namespace Unbound.UI
{
    /// <summary>
    /// Canvas-based health bar (HUD).
    /// Supports Slider OR Image fill.
    /// Does NOT follow world entities.
    /// </summary>
    public class UIHealthBar : MonoBehaviour
    {
        public enum DisplayMode
        {
            ImageFill,
            Slider
        }

        [Header("Display Mode")]
        [SerializeField] private DisplayMode displayMode = DisplayMode.ImageFill;

        [Header("UI References")]
        [SerializeField] private GameObject root;          // Entire HUD container
        [SerializeField] private Image fillImage;          // For ImageFill mode
        [SerializeField] private Slider healthSlider;      // For Slider mode

        [Header("Colors")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;

        [Header("Animation")]
        [SerializeField] private float fillLerpSpeed = 8f;

        private float _currentHealth;
        private float _maxHealth;
        private float _currentVisualValue;

        #region Public API

        public void SetMaxHealth(float maxHealth)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);

            if (displayMode == DisplayMode.Slider && healthSlider != null)
            {
                healthSlider.maxValue = _maxHealth;
            }
        }

        public void SetHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
        }

        #endregion

        private void Awake()
        {
            InitializeMode();
        }

        private void Update()
        {
            UpdateVisual();
            UpdateColor();
        }

        private void InitializeMode()
        {
            if (root != null)
                root.SetActive(true);

            if (displayMode == DisplayMode.ImageFill)
            {
                if (fillImage != null)
                    fillImage.type = Image.Type.Filled;

                if (healthSlider != null)
                    healthSlider.gameObject.SetActive(false);
            }
            else
            {
                if (healthSlider != null)
                    healthSlider.gameObject.SetActive(true);

                if (fillImage != null)
                    fillImage.gameObject.SetActive(false);
            }
        }

        private void UpdateVisual()
        {
            float targetValue = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
            _currentVisualValue = Mathf.Lerp(_currentVisualValue, targetValue, Time.deltaTime * fillLerpSpeed);

            if (displayMode == DisplayMode.ImageFill && fillImage != null)
            {
                fillImage.fillAmount = _currentVisualValue;
            }
            else if (displayMode == DisplayMode.Slider && healthSlider != null)
            {
                healthSlider.value = Mathf.Lerp(
                    healthSlider.value,
                    _currentHealth,
                    Time.deltaTime * fillLerpSpeed
                );
            }
        }

        private void UpdateColor()
        {
            float percent = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
            Color target = Color.Lerp(lowHealthColor, fullHealthColor, percent);

            if (displayMode == DisplayMode.ImageFill && fillImage != null)
            {
                fillImage.color = target;
            }
        }

        #region Entity Binding

        public void BindEnemy(Unbound.Enemy.Enemy enemy)
        {
            if (enemy == null) return;

            SetMaxHealth(enemy.MaxHealth);
            SetHealth(enemy.CurrentHealth);

            enemy.OnHealthChanged += (_, h) => SetHealth(h);
            enemy.OnDamageTaken += (_, _) => SetHealth(enemy.CurrentHealth);
            enemy.OnDeath += _ => root.SetActive(false);
        }

        public void BindPlayer(Unbound.Player.PlayerCombat player)
        {
            if (player == null) return;

            SetMaxHealth(player.maxHealth);
            SetHealth(player.health);

            player.OnHealthChanged += (_, h) => SetHealth(h);
            player.OnDamageTaken += (_, _) => SetHealth(player.health);
        }

        #endregion
    }
}
