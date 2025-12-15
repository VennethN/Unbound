using UnityEngine;
using UnityEngine.Events;

using Unbound.UI;
using Unbound.Player;

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
        
        [Header("Experience Drop")]
        [Tooltip("Amount of experience the player gains when this enemy dies")]
        [SerializeField] private int experienceDrop = 10;
        [Tooltip("If true, will grant experience to the player on death")]
        [SerializeField] private bool grantExperienceOnDeath = true;
        [Tooltip("If true, shows floating text at enemy position when exp is dropped")]
        [SerializeField] private bool showExpFloatingText = true;
        
        [Header("Death")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField] private float destroyDelay = 0f;
        [SerializeField] private GameObject deathEffectPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private Vector2 healthBarOffset = new Vector2(0f, 1f);
        
        [Header("Events")]
        public UnityEvent OnDeathEvent;
        public DamageEvent OnTakeDamageEvent;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => currentHealth <= 0f;
        
        /// <summary>
        /// Experience points dropped when this enemy is killed
        /// </summary>
        public int ExperienceDrop => experienceDrop;
        
        public System.Action<Enemy> OnDeath;
        public System.Action<Enemy, float> OnDamageTaken;
        public System.Action<Enemy, float> OnHealthChanged;
        
        private HealthBar _healthBar;
        private Animator animator;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
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

            OnTakeDamageEvent?.Invoke(damage);
            
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

            OnDeathEvent?.Invoke();

            GrantExperience();

            // Spawn death effect prefab
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }

            // If there's an animator, play death animation
            if (animator != null)
            {
                animator.SetTrigger("Die");
                // Delay destruction until animation is done
                float deathAnimLength = GetAnimationClipLength("bat_dying");
                Destroy(gameObject, deathAnimLength + 0.5f);
            }
            else
            {
                // No animator = fallback destroy
                if (destroyOnDeath)
                {
                    Destroy(gameObject, destroyDelay);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        
        /// <summary>
        /// Grants experience to the player when this enemy is killed
        /// </summary>
        private void GrantExperience()
        {
            if (!grantExperienceOnDeath || experienceDrop <= 0) return;
            
            var levelingSystem = LevelingSystem.Instance;
            if (levelingSystem != null)
            {
                // Show floating text at enemy position
                if (showExpFloatingText)
                {
                    var notificationManager = ExpNotificationManager.Instance;
                    if (notificationManager != null)
                    {
                        // Tell the notification manager to skip the auto-notification since we're showing our own
                        notificationManager.SkipNextExpNotification();
                        
                        notificationManager.SpawnFloatingTextAt(
                            transform.position + Vector3.up * 0.5f,
                            $"+{experienceDrop} EXP",
                            new Color(0.4f, 0.9f, 1f, 1f),
                            0.5f
                        );
                    }
                }
                
                levelingSystem.AddExperience(experienceDrop);
                Debug.Log($"{gameObject.name} dropped {experienceDrop} EXP");
            }
        }
        
        /// <summary>
        /// Sets the experience drop amount
        /// </summary>
        public void SetExperienceDrop(int amount)
        {
            experienceDrop = Mathf.Max(0, amount);
        }
        
        /// <summary>
        /// Enables or disables experience drop on death
        /// </summary>
        public void SetGrantExperienceOnDeath(bool grant)
        {
            grantExperienceOnDeath = grant;
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

        private float GetAnimationClipLength(string clipName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return 0f;

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == clipName)
                    return clip.length;
            }

            return 0f;
        }

    }

    [System.Serializable]
    public class DamageEvent : UnityEvent<float> { }

}

