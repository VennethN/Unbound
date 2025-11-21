using UnityEngine;
using Unbound.UI;

namespace Unbound.Enemy
{
    /// <summary>
    /// Basic enemy component that can take damage and be destroyed
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Enemy : MonoBehaviour
    {
        [Header("Enemy Stats")]
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float currentHealth;
        
        [Header("Death")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float destroyDelay = 0f;
        [SerializeField] private GameObject deathEffectPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector2 healthBarOffset = new Vector2(0f, 1f);
        
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        
        public System.Action<Enemy> OnDeath;
        public System.Action<Enemy, float> OnDamageTaken;
        public System.Action<Enemy, float> OnHealthChanged;
        
        private HealthBar _healthBar;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            
            // Set up health bar
            SetupHealthBar();
        }
        
        /// <summary>
        /// Sets up the health bar component
        /// </summary>
        private void SetupHealthBar()
        {
            _healthBar = GetComponent<HealthBar>();
            if (_healthBar == null)
            {
                _healthBar = gameObject.AddComponent<HealthBar>();
            }
            
            _healthBar.SetupForEnemy(this);
        }
        
        /// <summary>
        /// Deals damage to this enemy
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (IsDead)
                return;
            
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            
            OnDamageTaken?.Invoke(this, damage);
            OnHealthChanged?.Invoke(this, currentHealth);
            
            Debug.Log($"{gameObject.name} took {damage} damage. Health: {currentHealth}/{maxHealth}");
            
            if (IsDead)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Heals this enemy
        /// </summary>
        public void Heal(float amount)
        {
            if (IsDead)
                return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(this, currentHealth);
        }
        
        /// <summary>
        /// Sets the health directly
        /// </summary>
        public void SetHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            OnHealthChanged?.Invoke(this, currentHealth);
            
            if (IsDead && currentHealth <= 0f)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Kills the enemy immediately
        /// </summary>
        public void Kill()
        {
            currentHealth = 0f;
            Die();
        }
        
        /// <summary>
        /// Handles enemy death
        /// </summary>
        private void Die()
        {
            OnDeath?.Invoke(this);
            
            // Spawn death effect if assigned
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Destroy or disable the enemy
            if (destroyOnDeath)
            {
                if (destroyDelay > 0f)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                // Just disable the enemy
                gameObject.SetActive(false);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            if (showHealthBar && Application.isPlaying)
            {
                // Draw health bar position
                Vector3 barPosition = transform.position + (Vector3)healthBarOffset;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(barPosition, new Vector3(1f, 0.2f, 0f));
                
                // Draw current health
                if (maxHealth > 0f)
                {
                    float healthPercent = currentHealth / maxHealth;
                    Gizmos.color = Color.green;
                    Gizmos.DrawCube(barPosition, new Vector3(healthPercent, 0.15f, 0f));
                }
            }
        }
    }
}

