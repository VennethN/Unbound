using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Base class for all collideable objects in the game
    /// Provides common functionality for collision detection and trigger handling
    /// Automatically triggers when the player touches/collides with the object
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseCollideable : MonoBehaviour
    {
        [Header("Collision Settings")]
        [SerializeField] protected bool triggerOnce = false;
        [SerializeField] protected bool isTrigger = true;
        
        [Header("Events")]
        [SerializeField] protected UnityEvent onCollisionStart;
        [SerializeField] protected UnityEvent onCollisionEnd;

        // Runtime state
        protected bool hasTriggered = false;
        protected GameObject player;

        protected virtual void Awake()
        {
            // Find the player
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var playerController = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                }
            }

            // Ensure collider is set up correctly
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.isTrigger = isTrigger;
            }
        }

        /// <summary>
        /// Called when a trigger collision occurs (requires trigger collider)
        /// </summary>
        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                TryCollide();
            }
        }

        /// <summary>
        /// Called when a physical collision occurs (requires non-trigger collider)
        /// </summary>
        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsPlayer(collision.collider))
            {
                TryCollide();
            }
        }

        /// <summary>
        /// Checks if the collider belongs to the player
        /// </summary>
        protected virtual bool IsPlayer(Collider2D collider)
        {
            if (collider == null) return false;

            // Check by tag
            if (collider.CompareTag("Player"))
            {
                return true;
            }

            // Check by component
            if (collider.GetComponent<Unbound.Player.PlayerController2D>() != null)
            {
                return true;
            }

            // Check if it's the cached player object
            if (player != null && collider.gameObject == player)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to perform the collision action if conditions are met
        /// </summary>
        public void TryCollide()
        {
            if (CanTrigger())
            {
                onCollisionStart?.Invoke();
                PerformCollision();

                if (triggerOnce)
                {
                    hasTriggered = true;
                }

                onCollisionEnd?.Invoke();
            }
        }

        /// <summary>
        /// Checks if the trigger conditions are met (trigger once, quest requirements, etc.)
        /// </summary>
        protected virtual bool CanTrigger()
        {
            if (triggerOnce && hasTriggered)
            {
                return false;
            }

            // TODO: Add quest requirement checking here
            return true;
        }

        /// <summary>
        /// Override this method to implement specific collision behavior
        /// </summary>
        protected abstract void PerformCollision();

        /// <summary>
        /// Resets the trigger state (useful for testing or dynamic scenarios)
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
        }

        /// <summary>
        /// Manually triggers the collision (for testing or programmatic use)
        /// </summary>
        public void TriggerCollision()
        {
            TryCollide();
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the collider bounds (visible globally, not just when selected)
        /// </summary>
        protected virtual void OnDrawGizmos()
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider == null) return;

            // Use different colors for trigger vs non-trigger colliders
            Gizmos.color = collider.isTrigger ? new Color(0f, 1f, 1f, 0.3f) : new Color(1f, 0.5f, 0f, 0.3f);

            // Draw based on collider type
            if (collider is BoxCollider2D boxCollider)
            {
                Vector3 center = transform.position + (Vector3)boxCollider.offset;
                Vector3 size = new Vector3(boxCollider.size.x * transform.lossyScale.x, 
                                           boxCollider.size.y * transform.lossyScale.y, 0f);
                // For 2D colliders, use Z rotation only
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, transform.eulerAngles.z), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (collider is CircleCollider2D circleCollider)
            {
                Vector3 center = transform.position + (Vector3)circleCollider.offset;
                float radius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
                Gizmos.DrawWireSphere(center, radius);
            }
            else if (collider is CapsuleCollider2D capsuleCollider)
            {
                Vector3 center = transform.position + (Vector3)capsuleCollider.offset;
                Vector3 size = new Vector3(capsuleCollider.size.x * transform.lossyScale.x, 
                                          capsuleCollider.size.y * transform.lossyScale.y, 0f);
                // For 2D colliders, use Z rotation only
                Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, transform.eulerAngles.z), Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }
            else if (collider is PolygonCollider2D polygonCollider)
            {
                // Draw polygon outline
                Vector2[] points = polygonCollider.points;
                if (points.Length > 1)
                {
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 point1 = transform.TransformPoint(points[i]);
                        Vector3 point2 = transform.TransformPoint(points[(i + 1) % points.Length]);
                        Gizmos.DrawLine(point1, point2);
                    }
                }
            }
            else if (collider is EdgeCollider2D edgeCollider)
            {
                // Draw edge outline
                Vector2[] points = edgeCollider.points;
                if (points.Length > 1)
                {
                    for (int i = 0; i < points.Length - 1; i++)
                    {
                        Vector3 point1 = transform.TransformPoint(points[i]);
                        Vector3 point2 = transform.TransformPoint(points[i + 1]);
                        Gizmos.DrawLine(point1, point2);
                    }
                }
            }
            else
            {
                // Fallback: draw bounds
                Bounds bounds = collider.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}

