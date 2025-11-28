using UnityEngine;
using Unbound.Inventory;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbound.Player
{
    /// <summary>
    /// Handles player combat and attacks using equipped weapons
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Combat Settings")]
        [SerializeField] private float attackCooldown = 0.5f;
        [SerializeField] private LayerMask enemyLayerMask = -1;
        [SerializeField] private float attackRadius = 1f;
        [SerializeField] private float attackAnimationDuration = 0.3f; // Duration of attack animation
        
        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string attackTriggerParameter = "Attack";
        [SerializeField] private string attackXParameter = "AttackX";
        [SerializeField] private string attackYParameter = "AttackY";
        
        [Header("Hitbox")]
        [SerializeField] private bool useMouseDirection = true; // If true, attack towards mouse, else use movement direction
        
        [Header("Health")]
        public float health = 100f;
        public float maxHealth = 100f;
        
        public System.Action<PlayerCombat, float> OnDamageTaken;
        public System.Action<PlayerCombat, float> OnHealthChanged;
        
        [Header("Weapon Visual")]
        [SerializeField] private Transform weaponVisualParent; // Parent transform for weapon visual (e.g., hand position)
        [SerializeField] private Sprite defaultWeaponSprite; // Default weapon sprite when nothing is equipped
        [SerializeField] private float weaponVisualOffset = 0.3f; // Offset from player center for weapon visual
        [SerializeField] private string weaponVisualObjectName = "WeaponVisual"; // Name for the weapon visual GameObject
        [SerializeField] private int weaponSortingOrder = 10; // Sorting order for weapon sprite (higher = renders on top)
        
        [Header("Weapon Animation")]
        [SerializeField] private bool enableIdleAnimation = true; // Enable idle animation
        [SerializeField] private float idleRotationSpeed = 2f; // Speed of idle rotation animation
        [SerializeField] private float idleRotationAmount = 5f; // Degrees of rotation for idle animation
        [SerializeField] private float idleBobSpeed = 3f; // Speed of idle bobbing animation
        [SerializeField] private float idleBobAmount = 0.05f; // Amount of bobbing for idle animation
        [SerializeField] private float attackSwingAngle = 90f; // Degrees to swing during attack
        [SerializeField] private float attackSwingSpeed = 15f; // Speed of attack swing animation
        [SerializeField] private AnimationCurve attackSwingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Curve for attack swing
        [SerializeField] private float weaponVisualReach = 1f; // Multiplier for weapon visual reach (1.0 = matches attack range, higher = extends further)
        
        private PlayerStats _playerStats;
        private UnityEngine.Camera _mainCamera;
        private float _lastAttackTime = 0f;
        private bool _isAttacking = false;
        private Vector2 _attackDirection = Vector2.zero;
        private System.Collections.Generic.HashSet<Collider2D> _hitEnemies = new System.Collections.Generic.HashSet<Collider2D>();
        private SpriteRenderer _weaponSpriteRenderer; // SpriteRenderer for weapon visual
        private float _weaponBaseRotation = 0f; // Base rotation angle for weapon
        private float _weaponAnimationTime = 0f; // Time for animation
        private float _attackAnimationProgress = 0f; // Progress of attack animation (0-1)
        private Vector2 _attackStartDirection = Vector2.right; // Direction at start of attack
        private Vector2 _attackTargetPosition = Vector2.zero; // Target position for weapon during attack
        private float _attackRange = 1f; // Attack range for current attack
        private bool _combatEnabled = true; // Whether combat input is allowed
        
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
        private InputAction _attackAction;
#endif
        
        public bool IsAttacking => _isAttacking;
        
        /// <summary>
        /// Gets the mouse position using the appropriate Input System
        /// </summary>
        private Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }
        
        private float _lastHealth;
        private float _lastMaxHealth;
        
        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            
            // Initialize health - use default values first, then update from PlayerStats in Start
            if (maxHealth <= 0)
            {
                maxHealth = 100f; // Default max health
            }
            if (health <= 0)
            {
                health = maxHealth; // Start at full health
            }
            
            _lastHealth = health;
            _lastMaxHealth = maxHealth;
            
            // Get animator if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Get main camera for mouse position
            _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<UnityEngine.Camera>();
            }
            
            // Initialize weapon visual
            InitializeWeaponVisual();
            
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                _playerInput = gameObject.AddComponent<PlayerInput>();
            }
            
            _attackAction = _playerInput.actions?.FindAction("Attack", false);
            if (_attackAction == null)
            {
                Debug.LogWarning("PlayerCombat: Attack action not found in Input System!");
            }
#endif
        }
        
        private void Start()
        {
            // Update health from PlayerStats after it has initialized
            if (_playerStats != null && _playerStats.MaxHealth > 0)
            {
                maxHealth = _playerStats.MaxHealth;
                health = Mathf.Min(health, maxHealth); // Ensure health doesn't exceed max
            }
            
            _lastHealth = health;
            _lastMaxHealth = maxHealth;
        }
        
        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _attackAction?.Enable();
#endif
            
            // Subscribe to equipment changes
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped += OnWeaponEquipped;
                InventoryManager.Instance.OnItemUnequipped += OnWeaponUnequipped;
            }
            
            // Initialize weapon visual
            UpdateWeaponVisual();
        }
        
        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _attackAction?.Disable();
#endif
            
            // Unsubscribe from equipment changes
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped -= OnWeaponEquipped;
                InventoryManager.Instance.OnItemUnequipped -= OnWeaponUnequipped;
            }
        }
        
        private void Update()
        {
            // Only check for attack input if combat is enabled
            if (_combatEnabled)
            {
#if ENABLE_INPUT_SYSTEM
                if (_attackAction != null && _attackAction.WasPressedThisFrame())
                {
                    TryAttack();
                }
#else
                // Fallback to old input system
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                {
                    TryAttack();
                }
#endif
            }
            
            // Update weapon visual rotation and position with procedural animation
            if (_weaponSpriteRenderer != null && _weaponSpriteRenderer.enabled)
            {
                UpdateWeaponAnimation();
            }
            
            // Update health from PlayerStats if it changed
            if (_playerStats != null)
            {
                if (_playerStats.MaxHealth != maxHealth)
                {
                    maxHealth = _playerStats.MaxHealth;
                    health = Mathf.Min(health, maxHealth);
                }
            }
            
            // Detect inspector changes and notify
            if (health != _lastHealth)
            {
                float oldHealth = _lastHealth;
                if (health < oldHealth)
                {
                    OnDamageTaken?.Invoke(this, oldHealth - health);
                }
                OnHealthChanged?.Invoke(this, health);
                _lastHealth = health;
            }
            
            if (maxHealth != _lastMaxHealth)
            {
                OnHealthChanged?.Invoke(this, health);
                _lastMaxHealth = maxHealth;
            }
        }
        
        /// <summary>
        /// Takes damage and updates health
        /// </summary>
        public void TakeDamage(float damage)
        {
            float oldHealth = health;
            health = Mathf.Max(0, health - damage);
            
            if (health < oldHealth)
            {
                OnDamageTaken?.Invoke(this, damage);
            }
            
            OnHealthChanged?.Invoke(this, health);
        }
        
        /// <summary>
        /// Heals the player
        /// </summary>
        public void Heal(float amount)
        {
            health = Mathf.Min(maxHealth, health + amount);
            OnHealthChanged?.Invoke(this, health);
        }
        
        /// <summary>
        /// Attempts to perform an attack if conditions are met
        /// Requires a weapon to be equipped
        /// </summary>
        public void TryAttack()
        {
            // Check if a weapon is equipped - REQUIRED for attacking
            string equippedWeaponID = null;
            if (InventoryManager.Instance != null)
            {
                equippedWeaponID = InventoryManager.Instance.EquippedItems.GetEquippedItem(EquipmentType.Weapon);
            }
            
            if (string.IsNullOrEmpty(equippedWeaponID))
            {
                // No weapon equipped, cannot attack
                return;
            }
            
            // Check cooldown
            if (Time.time - _lastAttackTime < attackCooldown)
                return;
            
            // Get attack speed from player stats
            float currentAttackSpeed = _playerStats != null ? _playerStats.AttackSpeed : 1f;
            float cooldownMultiplier = Mathf.Max(0.1f, currentAttackSpeed); // Lower attack speed = faster attacks
            
            if (Time.time - _lastAttackTime < attackCooldown * cooldownMultiplier)
                return;
            
            // Calculate attack direction
            CalculateAttackDirection();
            
            // Perform attack
            PerformAttack(equippedWeaponID);
            
            _lastAttackTime = Time.time;
        }
        
        /// <summary>
        /// Calculates the direction of attack based on mouse position or movement
        /// </summary>
        private void CalculateAttackDirection()
        {
            if (useMouseDirection && _mainCamera != null)
            {
                // Attack towards mouse position
                Vector2 mouseScreenPos = GetMousePosition();
                Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _mainCamera.nearClipPlane));
                mouseWorldPos.z = 0f;
                _attackDirection = (mouseWorldPos - transform.position).normalized;
            }
            else
            {
                // Attack in movement direction or last facing direction
                var playerController = GetComponent<PlayerController2D>();
                if (playerController != null)
                {
                    // Try to get movement direction from rigidbody
                    var rb = GetComponent<Rigidbody2D>();
                    if (rb != null && rb.linearVelocity.sqrMagnitude > 0.1f)
                    {
                        _attackDirection = rb.linearVelocity.normalized;
                    }
                    else
                    {
                        // Default to right direction
                        _attackDirection = Vector2.right;
                    }
                }
                else
                {
                    _attackDirection = Vector2.right;
                }
            }
        }
        
        /// <summary>
        /// Performs the actual attack with weapon stats (or default stats if no weapon)
        /// </summary>
        private void PerformAttack(string weaponItemID)
        {
            _isAttacking = true;
            _attackStartDirection = _attackDirection;
            _attackAnimationProgress = 0f;
            
            // Trigger attack animation
            if (animator != null)
            {
                // Set attack direction for animation
                if (!string.IsNullOrEmpty(attackXParameter))
                {
                    animator.SetFloat(attackXParameter, _attackDirection.x);
                }
                if (!string.IsNullOrEmpty(attackYParameter))
                {
                    animator.SetFloat(attackYParameter, _attackDirection.y);
                }
                
                // Trigger attack animation
                if (!string.IsNullOrEmpty(attackTriggerParameter))
                {
                    animator.SetTrigger(attackTriggerParameter);
                }
            }
            
            // Get attack damage from player stats (includes weapon bonuses)
            float attackDamage = _playerStats != null ? _playerStats.AttackDamage : 10f;
            
            // Get attack range from player stats
            _attackRange = _playerStats != null ? _playerStats.AttackRange : 1f;
            
            // Create hitbox position in attack direction
            Vector2 hitboxPosition = (Vector2)transform.position + _attackDirection * _attackRange;
            
            // Calculate visual reach position (can extend beyond actual attack range)
            float visualReachDistance = _attackRange * weaponVisualReach;
            _attackTargetPosition = (Vector2)transform.position + _attackDirection * visualReachDistance;
            
            // Clear previously hit enemies
            _hitEnemies.Clear();
            
            // Perform attack hitbox check immediately
            CheckHitbox(hitboxPosition, attackRadius, attackDamage);
            
            // Reset attacking state after animation delay
            Invoke(nameof(ResetAttackState), attackAnimationDuration);
        }
        
        /// <summary>
        /// Checks the hitbox for enemies and deals damage
        /// </summary>
        private void CheckHitbox(Vector2 center, float radius, float damage)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius, enemyLayerMask);
            
            bool hitSomething = false;
            foreach (Collider2D hit in hits)
            {
                // Skip if already hit in this attack
                if (_hitEnemies.Contains(hit))
                    continue;
                
                hitSomething = true;
                _hitEnemies.Add(hit);
                
                // Try to deal damage to the hit object
                // Check for Enemy component (which has TakeDamage method)
                var enemy = hit.GetComponent<Unbound.Enemy.Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Debug.Log($"Attack hit: {hit.gameObject.name} for {damage} damage");
                }
                else
                {
                    // Try to find any component with TakeDamage method via reflection
                    var takeDamageMethod = hit.GetComponent<MonoBehaviour>()?.GetType().GetMethod("TakeDamage");
                    if (takeDamageMethod != null)
                    {
                        takeDamageMethod.Invoke(hit.GetComponent<MonoBehaviour>(), new object[] { damage });
                        Debug.Log($"Attack hit: {hit.gameObject.name} for {damage} damage (via reflection)");
                    }
                    else
                    {
                        Debug.Log($"Attack hit: {hit.gameObject.name} but no damage component found");
                    }
                }
            }
            
            if (!hitSomething)
            {
                Debug.Log($"Attack performed (no targets in hitbox). Damage: {damage}, Range: {radius}");
            }
        }
        
        /// <summary>
        /// Resets the attacking state
        /// </summary>
        private void ResetAttackState()
        {
            _isAttacking = false;
            _attackAnimationProgress = 0f;
        }
        
        /// <summary>
        /// Updates weapon visual with procedural animation
        /// </summary>
        private void UpdateWeaponAnimation()
        {
            if (_weaponSpriteRenderer == null)
                return;
            
            Vector2 direction = _attackDirection;
            
            // If no attack direction set yet, calculate from mouse position
            if (direction == Vector2.zero && useMouseDirection && _mainCamera != null)
            {
                Vector2 mouseScreenPos = GetMousePosition();
                Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _mainCamera.nearClipPlane));
                mouseWorldPos.z = 0f;
                direction = (mouseWorldPos - transform.position).normalized;
            }
            
            // Default to right if still no direction
            if (direction == Vector2.zero)
            {
                direction = Vector2.right;
            }
            
            // Calculate base rotation angle (inverted to fix sprite orientation)
            float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f;
            
            // Calculate base position
            Vector3 basePosition = direction * weaponVisualOffset;
            
            // Apply attack swing animation
            float finalAngle = baseAngle;
            Vector3 finalPosition = basePosition;
            
            if (_isAttacking && _attackAnimationProgress < 1f)
            {
                _attackAnimationProgress += attackSwingSpeed * Time.deltaTime;
                _attackAnimationProgress = Mathf.Clamp01(_attackAnimationProgress);
                
                // Calculate swing angle based on curve
                float curveValue = attackSwingCurve.Evaluate(_attackAnimationProgress);
                float swingOffset = Mathf.Sin(curveValue * Mathf.PI) * attackSwingAngle;
                
                // Swing perpendicular to attack direction
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                float swingDirection = Mathf.Sign(Vector2.Dot(perpendicular, _attackStartDirection));
                finalAngle = baseAngle + swingOffset * swingDirection;
                
                // Move weapon along arc to attack target position
                Vector2 startPos = (Vector2)transform.position + (Vector2)basePosition;
                Vector2 targetPos = _attackTargetPosition;
                
                // Interpolate position along arc (using curve for smooth motion)
                float positionT = curveValue;
                Vector2 currentWorldPos = Vector2.Lerp(startPos, targetPos, positionT);
                
                // Convert to local position
                finalPosition = transform.InverseTransformPoint(new Vector3(currentWorldPos.x, currentWorldPos.y, 0f));
            }
            else
            {
                // Apply idle animation when not attacking
                if (enableIdleAnimation)
                {
                    _weaponAnimationTime += Time.deltaTime;
                    
                    // Idle rotation
                    float idleRotation = Mathf.Sin(_weaponAnimationTime * idleRotationSpeed) * idleRotationAmount;
                    finalAngle += idleRotation;
                    
                    // Idle bobbing (applied to position)
                    float idleBob = Mathf.Sin(_weaponAnimationTime * idleBobSpeed) * idleBobAmount;
                    finalPosition = basePosition + new Vector3(0f, idleBob, 0f);
                }
                else
                {
                    // No idle animation, just use base position
                    finalPosition = basePosition;
                }
            }
            
            // Apply rotation and position
            _weaponSpriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);
            _weaponSpriteRenderer.transform.localPosition = finalPosition;
        }
        
        
        /// <summary>
        /// Gets the currently equipped weapon ID
        /// </summary>
        public string GetEquippedWeaponID()
        {
            if (InventoryManager.Instance == null)
                return null;
            
            return InventoryManager.Instance.EquippedItems.GetEquippedItem(EquipmentType.Weapon);
        }
        
        /// <summary>
        /// Checks if a weapon is currently equipped
        /// </summary>
        public bool HasWeaponEquipped()
        {
            return !string.IsNullOrEmpty(GetEquippedWeaponID());
        }
        
        /// <summary>
        /// Enables or disables combat input
        /// </summary>
        public void SetCombatEnabled(bool enabled)
        {
            _combatEnabled = enabled;
        }
        
        /// <summary>
        /// Returns whether combat input is currently enabled
        /// </summary>
        public bool IsCombatEnabled()
        {
            return _combatEnabled;
        }
        
        /// <summary>
        /// Initializes the weapon visual GameObject and SpriteRenderer
        /// </summary>
        private void InitializeWeaponVisual()
        {
            // Find or create weapon visual GameObject
            Transform parent = weaponVisualParent != null ? weaponVisualParent : transform;
            Transform weaponVisualTransform = parent.Find(weaponVisualObjectName);
            
            if (weaponVisualTransform == null)
            {
                // Create new weapon visual GameObject
                GameObject weaponVisualObj = new GameObject(weaponVisualObjectName);
                weaponVisualObj.transform.SetParent(parent);
                weaponVisualObj.transform.localPosition = new Vector3(weaponVisualOffset, 0f, 0f);
                weaponVisualObj.transform.localRotation = Quaternion.identity;
                
                // Add SpriteRenderer component
                _weaponSpriteRenderer = weaponVisualObj.AddComponent<SpriteRenderer>();
                _weaponSpriteRenderer.sortingOrder = weaponSortingOrder;
            }
            else
            {
                // Get existing SpriteRenderer
                _weaponSpriteRenderer = weaponVisualTransform.GetComponent<SpriteRenderer>();
                if (_weaponSpriteRenderer == null)
                {
                    _weaponSpriteRenderer = weaponVisualTransform.gameObject.AddComponent<SpriteRenderer>();
                }
                // Always update sorting order to ensure it's correct
                _weaponSpriteRenderer.sortingOrder = weaponSortingOrder;
            }
            
            // Update weapon sprite
            UpdateWeaponVisual();
        }
        
        /// <summary>
        /// Updates the weapon visual sprite based on currently equipped weapon
        /// </summary>
        private void UpdateWeaponVisual()
        {
            if (_weaponSpriteRenderer == null)
            {
                InitializeWeaponVisual();
                return;
            }
            
            // Get equipped weapon
            string equippedWeaponID = GetEquippedWeaponID();
            Sprite weaponSprite = null;
            
            // Try to get weapon sprite if a weapon is equipped
            if (!string.IsNullOrEmpty(equippedWeaponID))
            {
                // Try to get weapon sprite from ItemDatabase
                if (ItemDatabase.Instance != null)
                {
                    weaponSprite = ItemDatabase.Instance.GetItemSprite(equippedWeaponID);
                }
            }
            
            // Always fall back to default weapon sprite if:
            // - No weapon is equipped (equippedWeaponID is null/empty)
            // - Weapon sprite not found in database
            // - Weapon sprite is null for any reason
            if (weaponSprite == null)
            {
                weaponSprite = defaultWeaponSprite;
            }
            
            // Update sprite (this will set default sprite when unequipped)
            _weaponSpriteRenderer.sprite = weaponSprite;
            
            // Ensure sorting order is set correctly
            _weaponSpriteRenderer.sortingOrder = weaponSortingOrder;
            
            // Hide weapon visual if no sprite is available (including default)
            if (_weaponSpriteRenderer.sprite == null)
            {
                _weaponSpriteRenderer.enabled = false;
            }
            else
            {
                _weaponSpriteRenderer.enabled = true;
            }
        }
        
        /// <summary>
        /// Called when a weapon is equipped
        /// </summary>
        private void OnWeaponEquipped(EquipmentType slot, string itemID)
        {
            if (slot == EquipmentType.Weapon)
            {
                UpdateWeaponVisual();
            }
        }
        
        /// <summary>
        /// Called when a weapon is unequipped
        /// </summary>
        private void OnWeaponUnequipped(EquipmentType slot)
        {
            if (slot == EquipmentType.Weapon)
            {
                UpdateWeaponVisual();
            }
        }
        
        private void OnDrawGizmos()
        {
            // Always draw attack area gizmo (not just when selected)
            float attackRange = _playerStats != null ? _playerStats.AttackRange : 1f;
            
            // Calculate current attack direction (or use mouse direction if in play mode)
            Vector2 currentDirection = _attackDirection;
            if (currentDirection == Vector2.zero && Application.isPlaying && useMouseDirection && _mainCamera != null)
            {
                // Use helper method to get mouse position
                Vector2 mouseScreenPos = GetMousePosition();
                
                if (mouseScreenPos != Vector2.zero)
                {
                    Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, _mainCamera.nearClipPlane));
                    mouseWorldPos.z = 0f;
                    currentDirection = (mouseWorldPos - transform.position).normalized;
                }
            }
            
            // Default direction if still zero
            if (currentDirection == Vector2.zero)
            {
                currentDirection = Vector2.right; // Default direction
            }
            
            // Draw attack range circle around player
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f); // Yellow with transparency
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw attack hitbox area
            Vector2 hitboxPosition = (Vector2)transform.position + currentDirection * attackRange;
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // Red with transparency
            Gizmos.DrawWireSphere(hitboxPosition, attackRadius);
            
            // Draw line showing attack direction
            Gizmos.color = new Color(1f, 0f, 0f, 0.7f); // Red
            Gizmos.DrawLine(transform.position, hitboxPosition);
            
            // Draw filled circle for better visibility
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f); // Red with low transparency
            Gizmos.DrawSphere(hitboxPosition, attackRadius);
        }
    }
}

