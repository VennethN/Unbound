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
        
        private PlayerStats _playerStats;
        private Camera _mainCamera;
        private float _lastAttackTime = 0f;
        private bool _isAttacking = false;
        private Vector2 _attackDirection = Vector2.zero;
        private System.Collections.Generic.HashSet<Collider2D> _hitEnemies = new System.Collections.Generic.HashSet<Collider2D>();
        
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
        private InputAction _attackAction;
#endif
        
        public bool IsAttacking => _isAttacking;
        
        private void Awake()
        {
            _playerStats = GetComponent<PlayerStats>();
            if (_playerStats == null)
            {
                Debug.LogWarning("PlayerCombat: PlayerStats component not found!");
            }
            
            // Get animator if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Get main camera for mouse position
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                _mainCamera = FindFirstObjectByType<Camera>();
            }
            
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
        
        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _attackAction?.Enable();
#endif
        }
        
        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            _attackAction?.Disable();
#endif
        }
        
        private void Update()
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
        
        /// <summary>
        /// Attempts to perform an attack if conditions are met
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
                Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
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
        /// Performs the actual attack with weapon stats
        /// </summary>
        private void PerformAttack(string weaponItemID)
        {
            _isAttacking = true;
            
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
            float attackRange = _playerStats != null ? _playerStats.AttackRange : 1f;
            
            // Create hitbox position in attack direction
            Vector2 hitboxPosition = (Vector2)transform.position + _attackDirection * attackRange;
            
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
                // Check for SaveableEntity (which has TakeDamage method)
                var saveableEntity = hit.GetComponent<SaveableEntity>();
                if (saveableEntity != null)
                {
                    saveableEntity.TakeDamage(damage);
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
        
        private void OnDrawGizmosSelected()
        {
            // Draw attack range and hitbox in editor
            float attackRange = _playerStats != null ? _playerStats.AttackRange : 1f;
            
            // Draw attack range circle
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw hitbox position if attacking
            if (_isAttacking && _attackDirection != Vector2.zero)
            {
                Vector2 hitboxPosition = (Vector2)transform.position + _attackDirection * attackRange;
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(hitboxPosition, attackRadius);
                
                // Draw line showing attack direction
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, hitboxPosition);
            }
            
            // Always draw attack direction preview
            if (_attackDirection != Vector2.zero && Application.isPlaying)
            {
                Vector2 hitboxPosition = (Vector2)transform.position + _attackDirection * attackRange;
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(hitboxPosition, attackRadius * 0.5f);
            }
        }
    }
}

