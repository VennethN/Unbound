using UnityEngine;

namespace Unbound.Enemy
{
    /// <summary>
    /// Basic AI for enemies - can chase player or patrol
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Enemy))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] private AIBehavior behavior = AIBehavior.ChasePlayer;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float detectionRange = 5f;
        [SerializeField] private float attackRange = 1.5f;
        
        [Header("Patrol Settings")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private float patrolReachDistance = 0.5f;
        
        [Header("Combat")]
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackDamage = 10f;
        
        private Rigidbody2D _rigidbody;
        private Enemy _enemy;
        private Transform _player;
        private int _currentPatrolIndex = 0;
        private float _lastAttackTime = 0f;
        private float _patrolWaitUntil = 0f;
        private Vector2 _targetPosition;
        
        private enum AIBehavior
        {
            Idle,
            Patrol,
            ChasePlayer,
            AttackPlayer
        }
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _enemy = GetComponent<Enemy>();
            
            // Find player
            var playerController = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
            if (playerController != null)
            {
                _player = playerController.transform;
            }
            else
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    _player = playerObj.transform;
                }
            }
        }
        
        private void Update()
        {
            if (_rigidbody == null) return;
            
            if (_enemy != null && _enemy.IsDead)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }
            
            switch (behavior)
            {
                case AIBehavior.Idle:
                    HandleIdle();
                    break;
                case AIBehavior.Patrol:
                    HandlePatrol();
                    break;
                case AIBehavior.ChasePlayer:
                    HandleChasePlayer();
                    break;
                case AIBehavior.AttackPlayer:
                    HandleAttackPlayer();
                    break;
            }
        }
        
        private void HandleIdle()
        {
            _rigidbody.linearVelocity = Vector2.zero;
            
            // Check if player is within detection range to start chasing
            if (_player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
                if (distanceToPlayer < detectionRange)
                {
                    behavior = AIBehavior.ChasePlayer;
                }
            }
        }
        
        private void HandlePatrol()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                behavior = AIBehavior.Idle;
                return;
            }
            
            // Check if we should chase player instead
            if (_player != null)
            {
                float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
                if (distanceToPlayer < detectionRange)
                {
                    behavior = AIBehavior.ChasePlayer;
                    return;
                }
            }
            
            // Wait at patrol point
            if (Time.time < _patrolWaitUntil)
            {
                _rigidbody.linearVelocity = Vector2.zero;
                return;
            }
            
            // Move towards current patrol point
            _targetPosition = patrolPoints[_currentPatrolIndex].position;
            Vector2 direction = (_targetPosition - (Vector2)transform.position).normalized;
            
            if (Vector2.Distance(transform.position, _targetPosition) < patrolReachDistance)
            {
                // Reached patrol point, wait and move to next
                _patrolWaitUntil = Time.time + patrolWaitTime;
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
            }
            else
            {
                _rigidbody.linearVelocity = direction * moveSpeed;
            }
        }
        
        private void HandleChasePlayer()
        {
            if (_player == null)
            {
                behavior = AIBehavior.Idle;
                return;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            
            // If player is too far, go back to patrol/idle
            if (distanceToPlayer > detectionRange * 1.5f)
            {
                if (patrolPoints != null && patrolPoints.Length > 0)
                {
                    behavior = AIBehavior.Patrol;
                }
                else
                {
                    behavior = AIBehavior.Idle;
                }
                return;
            }
            
            // If close enough, attack
            if (distanceToPlayer <= attackRange)
            {
                behavior = AIBehavior.AttackPlayer;
                return;
            }
            
            // Chase player
            Vector2 direction = (_player.position - transform.position).normalized;
            _rigidbody.linearVelocity = direction * moveSpeed;
        }
        
        private void HandleAttackPlayer()
        {
            if (_player == null)
            {
                behavior = AIBehavior.Idle;
                return;
            }
            
            float distanceToPlayer = Vector2.Distance(transform.position, _player.position);
            
            // If player is too far, chase again
            if (distanceToPlayer > attackRange * 1.2f)
            {
                behavior = AIBehavior.ChasePlayer;
                return;
            }
            
            // Stop moving
            _rigidbody.linearVelocity = Vector2.zero;
            
            // Attack if cooldown is ready
            if (Time.time - _lastAttackTime >= attackCooldown)
            {
                AttackPlayer();
                _lastAttackTime = Time.time;
            }
        }
        
        private void AttackPlayer()
        {
            // Try to damage player using PlayerCombat (which has TakeDamage method)
            var playerCombat = _player.GetComponent<Unbound.Player.PlayerCombat>();
            if (playerCombat != null)
            {
                playerCombat.TakeDamage(attackDamage);
                Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage");
            }
            else
            {
                // Fallback: try to find any component with TakeDamage method via reflection
                var takeDamageMethod = _player.GetComponent<MonoBehaviour>()?.GetType().GetMethod("TakeDamage");
                if (takeDamageMethod != null)
                {
                    takeDamageMethod.Invoke(_player.GetComponent<MonoBehaviour>(), new object[] { attackDamage });
                    Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage (via reflection)");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name} tried to attack player but no TakeDamage method found. Player needs PlayerCombat component or a component with TakeDamage method.");
                }
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw patrol path
            if (patrolPoints != null && patrolPoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] == null) continue;
                    
                    int nextIndex = (i + 1) % patrolPoints.Length;
                    if (patrolPoints[nextIndex] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                    }
                    
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                }
            }
        }
    }
}

