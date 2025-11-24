using UnityEngine;

namespace Unbound.UI
{
    /// <summary>
    /// Health bar component that displays above an entity and shows when damage is taken
    /// Uses GameObjects with SpriteRenderers instead of Canvas/UI
    /// </summary>
    public class HealthBar : MonoBehaviour
    {
        [Header("Health Bar Settings")]
        [SerializeField] private float showDuration = 3f; // How long to show health bar after taking damage
        [SerializeField] private float hideDelay = 2f; // Delay before hiding when health is full
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f); // Offset from entity position
        [SerializeField] private Vector2 healthBarSize = new Vector2(1f, 0.1f); // Size of health bar in world units
        
        [Header("GameObject References")]
        [SerializeField] private GameObject healthBarContainer;
        [SerializeField] private SpriteRenderer healthBarBackground;
        [SerializeField] private SpriteRenderer healthBarFill;
        
        [Header("Colors")]
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        
        [Header("Animation")]
        [SerializeField] private float colorTransitionSpeed = 2f;
        [SerializeField] private float fillAnimationSpeed = 5f;
        
        private float _currentHealth;
        private float _maxHealth;
        private float _targetFillAmount;
        private float _currentFillAmount;
        private float _lastDamageTime;
        private bool _isVisible = false;
        private UnityEngine.Camera _mainCamera;
        
        public float CurrentHealth
        {
            get => _currentHealth;
            set
            {
                if (value < _currentHealth)
                {
                    // Damage taken - show health bar
                    ShowHealthBar();
                    _lastDamageTime = Time.time;
                }
                _currentHealth = Mathf.Clamp(value, 0f, _maxHealth);
                _targetFillAmount = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
            }
        }
        
        public float MaxHealth
        {
            get => _maxHealth;
            set
            {
                _maxHealth = Mathf.Max(1f, value);
                _targetFillAmount = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
            }
        }
        
        private void Awake()
        {
            InitializeHealthBar();
        }
        
        private void Start()
        {
            _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<UnityEngine.Camera>();
            }
            
            // Auto-setup if component exists
            var enemy = GetComponent<Unbound.Enemy.Enemy>();
            if (enemy != null)
            {
                SetupForEnemy(enemy);
            }
            
            var playerCombat = GetComponent<Unbound.Player.PlayerCombat>();
            if (playerCombat != null)
            {
                SetupForPlayer(playerCombat);
            }
            
            // Initially hide health bar
            SetHealthBarVisible(false);
        }
        
        private void Update()
        {
            // Update position to follow entity
            UpdatePosition();
            
            // Update health bar fill
            UpdateHealthBarFill();
            
            // Update health bar visibility
            UpdateVisibility();
            
            // Update colors based on health
            UpdateColors();
            
            // Refresh health from player/enemy in real-time (for inspector editing)
            RefreshHealthFromSource();
        }
        
        /// <summary>
        /// Refreshes health values from the source entity (for inspector editing)
        /// </summary>
        private void RefreshHealthFromSource()
        {
            if (!Application.isPlaying)
                return;
            
            // Check for enemy
            var enemy = GetComponent<Unbound.Enemy.Enemy>();
            if (enemy != null)
            {
                if (MaxHealth != enemy.MaxHealth || CurrentHealth != enemy.CurrentHealth)
                {
                    MaxHealth = enemy.MaxHealth;
                    CurrentHealth = enemy.CurrentHealth;
                }
                return;
            }
            
            // Check for player combat
            var playerCombat = GetComponent<Unbound.Player.PlayerCombat>();
            if (playerCombat != null)
            {
                if (MaxHealth != playerCombat.maxHealth || CurrentHealth != playerCombat.health)
                {
                    MaxHealth = playerCombat.maxHealth;
                    CurrentHealth = playerCombat.health;
                }
            }
        }
        
        /// <summary>
        /// Creates a simple white sprite for the health bar
        /// </summary>
        private Sprite CreateSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        
        /// <summary>
        /// Initializes the health bar GameObjects if not already set up
        /// </summary>
        private void InitializeHealthBar()
        {
            if (healthBarContainer == null)
            {
                // Create container GameObject (parented to player, rotation/scale will be countered)
                healthBarContainer = new GameObject("HealthBarContainer");
                healthBarContainer.transform.SetParent(transform);
                healthBarContainer.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
                healthBarContainer.transform.localRotation = Quaternion.identity;
                healthBarContainer.transform.localScale = Vector3.one;
                
                // Create background sprite
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(healthBarContainer.transform);
                bgObj.transform.localPosition = Vector3.zero;
                bgObj.transform.localRotation = Quaternion.identity;
                bgObj.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y, 1f);
                
                healthBarBackground = bgObj.AddComponent<SpriteRenderer>();
                healthBarBackground.sprite = CreateSprite();
                healthBarBackground.color = backgroundColor;
                healthBarBackground.sortingOrder = 100; // Render above most things
                
                // Create fill sprite
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(healthBarContainer.transform);
                fillObj.transform.localPosition = new Vector3(-healthBarSize.x / 2f, 0f, -0.01f); // Slightly in front
                fillObj.transform.localRotation = Quaternion.identity;
                fillObj.transform.localScale = new Vector3(healthBarSize.x, healthBarSize.y, 1f);
                
                healthBarFill = fillObj.AddComponent<SpriteRenderer>();
                healthBarFill.sprite = CreateSprite();
                healthBarFill.color = fullHealthColor;
                healthBarFill.sortingOrder = 101; // Render above background
                
                // Set pivot to left for fill scaling
                Sprite fillSprite = healthBarFill.sprite;
                if (fillSprite != null)
                {
                    // Create new sprite with left pivot
                    Texture2D fillTexture = fillSprite.texture;
                    Sprite newFillSprite = Sprite.Create(fillTexture, fillSprite.rect, new Vector2(0f, 0.5f), fillSprite.pixelsPerUnit);
                    healthBarFill.sprite = newFillSprite;
                }
            }
        }
        
        /// <summary>
        /// Updates the health bar position to follow the entity
        /// </summary>
        private void UpdatePosition()
        {
            if (healthBarContainer == null || _mainCamera == null)
                return;
            
            // Counter negative X scale - if player scale X is negative, negate it back
            Vector3 playerScale = transform.localScale;
            Vector3 counterScale = Vector3.one;
            if (playerScale.x < 0)
            {
                counterScale.x = -1f;
            }
            if (playerScale.y < 0)
            {
                counterScale.y = -1f;
            }
            healthBarContainer.transform.localScale = counterScale;
            
            // Make health bar face camera (billboard) - counter player rotation and apply billboard
            Vector3 worldPos = healthBarContainer.transform.position;
            Vector3 directionToCamera = (_mainCamera.transform.position - worldPos).normalized;
            Quaternion billboardRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
            
            // Calculate local rotation that cancels player rotation and applies billboard
            // worldRotation = parentRotation * localRotation
            // So: localRotation = Inverse(parentRotation) * worldRotation
            healthBarContainer.transform.localRotation = Quaternion.Inverse(transform.rotation) * billboardRotation;
        }
        
        /// <summary>
        /// Updates the health bar fill amount with smooth animation
        /// </summary>
        private void UpdateHealthBarFill()
        {
            if (healthBarFill == null)
                return;
            
            // Smoothly animate fill amount
            _currentFillAmount = Mathf.Lerp(_currentFillAmount, _targetFillAmount, Time.deltaTime * fillAnimationSpeed);
            
            // Update fill scale based on health percentage
            // Since the sprite pivot is at the left edge (0, 0.5), scaling in X will grow/shrink from the left
            float fillWidth = healthBarSize.x * _currentFillAmount;
            healthBarFill.transform.localScale = new Vector3(fillWidth, healthBarSize.y, 1f);
            
            // Position stays fixed at left edge - no need to recalculate
            // (Position is already set in InitializeHealthBar and doesn't need to change)
            healthBarFill.transform.localPosition = new Vector3(-healthBarSize.x / 2f, 0f, -0.01f);
        }
        
        /// <summary>
        /// Updates health bar visibility based on damage and health state
        /// </summary>
        private void UpdateVisibility()
        {
            bool shouldShow = false;
            
            // Hide everything when health is full
            if (_currentHealth >= _maxHealth)
            {
                shouldShow = false;
            }
            // Show when health is not full
            else if (_currentHealth < _maxHealth)
            {
                shouldShow = true;
            }
            
            if (shouldShow != _isVisible)
            {
                SetHealthBarVisible(shouldShow);
            }
        }
        
        /// <summary>
        /// Updates health bar colors based on health percentage
        /// </summary>
        private void UpdateColors()
        {
            if (healthBarFill == null)
                return;
            
            float healthPercent = _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;
            Color targetColor = Color.Lerp(lowHealthColor, fullHealthColor, healthPercent);
            
            healthBarFill.color = Color.Lerp(healthBarFill.color, targetColor, Time.deltaTime * colorTransitionSpeed);
        }
        
        /// <summary>
        /// Shows the health bar
        /// </summary>
        public void ShowHealthBar()
        {
            _isVisible = true;
            _lastDamageTime = Time.time;
            SetHealthBarVisible(true);
        }
        
        /// <summary>
        /// Hides the health bar
        /// </summary>
        public void HideHealthBar()
        {
            _isVisible = false;
            SetHealthBarVisible(false);
        }
        
        /// <summary>
        /// Sets the visibility of the health bar
        /// </summary>
        private void SetHealthBarVisible(bool visible)
        {
            if (healthBarContainer != null)
            {
                healthBarContainer.SetActive(visible);
            }
        }
        
        /// <summary>
        /// Sets up the health bar for an enemy
        /// </summary>
        public void SetupForEnemy(Unbound.Enemy.Enemy enemy)
        {
            if (enemy == null)
                return;
            
            MaxHealth = enemy.MaxHealth;
            CurrentHealth = enemy.CurrentHealth;
            
            // Subscribe to enemy events
            enemy.OnDamageTaken += OnEnemyDamageTaken;
            enemy.OnHealthChanged += OnEnemyHealthChanged;
            enemy.OnDeath += OnEnemyDeath;
        }
        
        /// <summary>
        /// Sets up the health bar for a player
        /// </summary>
        public void SetupForPlayer(Unbound.Player.PlayerCombat player)
        {
            if (player == null)
                return;
            
            MaxHealth = player.maxHealth;
            CurrentHealth = player.health;
            
            // Subscribe to player events
            player.OnDamageTaken += OnPlayerCombatDamageTaken;
            player.OnHealthChanged += OnPlayerCombatHealthChanged;
        }
        
        private void OnPlayerCombatDamageTaken(Unbound.Player.PlayerCombat player, float damage)
        {
            CurrentHealth = player.health;
        }
        
        private void OnPlayerCombatHealthChanged(Unbound.Player.PlayerCombat player, float health)
        {
            CurrentHealth = health;
        }
        
        private void OnEnemyDamageTaken(Unbound.Enemy.Enemy enemy, float damage)
        {
            CurrentHealth = enemy.CurrentHealth;
        }
        
        private void OnEnemyHealthChanged(Unbound.Enemy.Enemy enemy, float health)
        {
            CurrentHealth = health;
        }
        
        private void OnEnemyDeath(Unbound.Enemy.Enemy enemy)
        {
            HideHealthBar();
        }
        
        private void OnDestroy()
        {
            // Clean up event subscriptions
            var enemy = GetComponent<Unbound.Enemy.Enemy>();
            if (enemy != null)
            {
                enemy.OnDamageTaken -= OnEnemyDamageTaken;
                enemy.OnHealthChanged -= OnEnemyHealthChanged;
                enemy.OnDeath -= OnEnemyDeath;
            }
            
            var playerCombat = GetComponent<Unbound.Player.PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.OnDamageTaken -= OnPlayerCombatDamageTaken;
                playerCombat.OnHealthChanged -= OnPlayerCombatHealthChanged;
            }
            
            // Destroy health bar container if it exists
            if (healthBarContainer != null)
            {
                Destroy(healthBarContainer);
            }
        }
    }
}
