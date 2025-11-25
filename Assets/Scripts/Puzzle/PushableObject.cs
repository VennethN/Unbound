using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Puzzle
{
    /// <summary>
    /// Component that makes an object pushable by the player.
    /// Handles physics-based pushing mechanics and collision detection.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PushableObject : MonoBehaviour
    {
        [Header("Push Settings")]
        [SerializeField] private float pushSpeed = 2f;
        [SerializeField] private float friction = 0.95f;
        [SerializeField] private bool lockRotation = true;
        [SerializeField] private bool canPushOnAnySide = true;
        
        [Header("Constraints")]
        [SerializeField] private bool constrainX = false;
        [SerializeField] private bool constrainY = false;
        [Tooltip("If enabled, restricts pushing to only one axis at a time (X or Y, no diagonal). The axis with greater movement is chosen.")]
        [SerializeField] private bool lockToSingleAxis = false;
        
        [Header("Events")]
        [SerializeField] private UnityEvent<GameObject> onPushStart;
        [SerializeField] private UnityEvent<GameObject> onPushEnd;
        [SerializeField] private UnityEvent<Vector2> onPushed;

        private Rigidbody2D rb;
        private Collider2D pushCollider;
        private GameObject player;
        private bool isBeingPushed = false;
        private Vector2 lastVelocity;
        private float originalDrag;
        private Vector3 initialPosition;
        private bool hasInitialPosition = false;
        private PuzzleManager puzzleManager = null;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            pushCollider = GetComponent<Collider2D>();
            
            if (rb == null)
            {
                Debug.LogError($"PushableObject on {gameObject.name} requires a Rigidbody2D component!", this);
                return;
            }

            // Store original drag
            originalDrag = rb.linearDamping;
            
            // Configure rigidbody for pushing
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (lockRotation)
            {
                rb.freezeRotation = true;
            }

            // Find player
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var playerController = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                }
            }
        }

        private void Start()
        {
            // Store initial position at Start (after potential positioning by other scripts)
            if (!hasInitialPosition)
            {
                initialPosition = transform.position;
                hasInitialPosition = true;
            }
        }

        private void FixedUpdate()
        {
            // Stop movement if puzzle is completed and movement is locked
            if (puzzleManager != null && !puzzleManager.IsMovementAllowed())
            {
                if (rb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    rb.linearVelocity = Vector2.zero;
                    HandlePushEnd();
                }
                return;
            }

            // Apply friction when not being pushed
            if (!isBeingPushed && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity *= friction;
                
                // Stop if velocity is very small
                if (rb.linearVelocity.sqrMagnitude < 0.01f)
                {
                    rb.linearVelocity = Vector2.zero;
                }
            }

            // Lock to single axis if enabled (prevent diagonal movement)
            if (lockToSingleAxis && rb.linearVelocity.sqrMagnitude > 0.01f)
            {
                Vector2 velocity = rb.linearVelocity;
                if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
                {
                    rb.linearVelocity = new Vector2(velocity.x, 0f); // Lock to X axis
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, velocity.y); // Lock to Y axis
                }
            }

            // Apply constraints
            if (constrainX)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            if (constrainY)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }

            // Check boundary constraints
            if (puzzleManager != null)
            {
                Vector2 currentPosition = transform.position;
                
                // First, ensure current position is within bounds (in case boundary was set after object was placed)
                if (!puzzleManager.IsPositionWithinBoundary(currentPosition))
                {
                    Vector2 clampedPos = puzzleManager.ClampPositionToBoundary(currentPosition);
                    transform.position = clampedPos;
                    rb.linearVelocity = Vector2.zero;
                }
                // Then check if movement would take us outside bounds
                else if (rb.linearVelocity.sqrMagnitude > 0.01f)
                {
                    Vector2 nextPosition = currentPosition + rb.linearVelocity * Time.fixedDeltaTime;
                    
                    // Check if the next position would be outside the boundary
                    if (!puzzleManager.IsPositionWithinBoundary(nextPosition))
                    {
                        // Clamp the position to the boundary and adjust velocity
                        Vector2 clampedPosition = puzzleManager.ClampPositionToBoundary(nextPosition);
                        Vector2 allowedMovement = clampedPosition - currentPosition;
                        
                        // Only allow movement that keeps us within bounds
                        if (allowedMovement.sqrMagnitude > 0.001f)
                        {
                            rb.linearVelocity = allowedMovement / Time.fixedDeltaTime;
                        }
                        else
                        {
                            // Can't move in this direction, stop
                            rb.linearVelocity = Vector2.zero;
                        }
                    }
                }
            }

            // Track if pushing stopped
            if (isBeingPushed && rb.linearVelocity.sqrMagnitude < 0.1f)
            {
                HandlePushEnd();
            }

            lastVelocity = rb.linearVelocity;
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            // Check if colliding with player
            if (player != null && collision.gameObject == player)
            {
                HandlePlayerCollision(collision);
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (player != null && collision.gameObject == player)
            {
                HandlePushEnd();
            }
        }

        private void HandlePlayerCollision(Collision2D collision)
        {
            // Check if movement is allowed (e.g., puzzle completed and locked)
            if (puzzleManager != null && !puzzleManager.IsMovementAllowed())
            {
                HandlePushEnd();
                return;
            }

            // Get player movement direction
            var playerController = player.GetComponent<Unbound.Player.PlayerController2D>();
            if (playerController == null) return;

            // Get player's current velocity to determine push direction
            var playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb == null) return;

            Vector2 playerVelocity = playerRb.linearVelocity;
            
            // Only push if player is moving
            if (playerVelocity.sqrMagnitude < 0.1f)
            {
                HandlePushEnd();
                return;
            }

            // Determine push direction based on collision normal and player movement
            Vector2 pushDirection = GetPushDirection(collision, playerVelocity);
            
            if (pushDirection.sqrMagnitude > 0.1f)
            {
                if (!isBeingPushed)
                {
                    HandlePushStart();
                }

                // Apply push force
                Vector2 pushForce = pushDirection * pushSpeed;
                
                // Lock to single axis if enabled (choose the axis with greater movement)
                if (lockToSingleAxis)
                {
                    if (Mathf.Abs(pushForce.x) > Mathf.Abs(pushForce.y))
                    {
                        pushForce.y = 0f; // Lock to X axis
                    }
                    else
                    {
                        pushForce.x = 0f; // Lock to Y axis
                    }
                }
                
                // Apply constraints before adding force
                if (constrainX) pushForce.x = 0f;
                if (constrainY) pushForce.y = 0f;
                
                rb.linearVelocity = pushForce;
                onPushed?.Invoke(pushDirection);
            }
        }

        private Vector2 GetPushDirection(Collision2D collision, Vector2 playerVelocity)
        {
            if (canPushOnAnySide)
            {
                // Push in the direction the player is moving
                return playerVelocity.normalized;
            }
            else
            {
                // Only push if player is moving towards the object
                Vector2 collisionNormal = collision.GetContact(0).normal;
                Vector2 playerToObject = (transform.position - player.transform.position).normalized;
                
                // If player is moving towards the object, push in that direction
                if (Vector2.Dot(playerVelocity.normalized, playerToObject) > 0.3f)
                {
                    return playerVelocity.normalized;
                }
            }
            
            return Vector2.zero;
        }

        private void HandlePushStart()
        {
            isBeingPushed = true;
            rb.linearDamping = 0f; // Reduce drag while being pushed
            onPushStart?.Invoke(player);
        }

        private void HandlePushEnd()
        {
            if (isBeingPushed)
            {
                isBeingPushed = false;
                rb.linearDamping = originalDrag;
                onPushEnd?.Invoke(player);
            }
        }

        /// <summary>
        /// Gets whether this object is currently being pushed
        /// </summary>
        public bool IsBeingPushed()
        {
            return isBeingPushed;
        }

        /// <summary>
        /// Gets the current velocity of the pushable object
        /// </summary>
        public Vector2 GetVelocity()
        {
            return rb != null ? rb.linearVelocity : Vector2.zero;
        }

        /// <summary>
        /// Manually set the push speed
        /// </summary>
        public void SetPushSpeed(float speed)
        {
            pushSpeed = Mathf.Max(0f, speed);
        }

        /// <summary>
        /// Stops the object immediately
        /// </summary>
        public void Stop()
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            HandlePushEnd();
        }

        /// <summary>
        /// Resets the object to its initial position
        /// </summary>
        public void ResetPosition()
        {
            if (hasInitialPosition)
            {
                transform.position = initialPosition;
                Stop();
            }
        }

        /// <summary>
        /// Sets the initial position for reset purposes
        /// </summary>
        public void SetInitialPosition(Vector3 position)
        {
            initialPosition = position;
            hasInitialPosition = true;
        }

        /// <summary>
        /// Gets the initial position
        /// </summary>
        public Vector3 GetInitialPosition()
        {
            return hasInitialPosition ? initialPosition : transform.position;
        }

        /// <summary>
        /// Checks if the initial position has been set
        /// </summary>
        public bool HasInitialPosition()
        {
            return hasInitialPosition;
        }

        /// <summary>
        /// Sets the PuzzleManager that manages this pushable object's boundary constraints
        /// </summary>
        public void SetPuzzleManager(PuzzleManager manager)
        {
            puzzleManager = manager;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw constraint indicators
            Gizmos.color = Color.cyan;
            if (constrainX)
            {
                Gizmos.DrawLine(
                    transform.position + Vector3.up * 0.5f,
                    transform.position + Vector3.down * 0.5f
                );
            }
            if (constrainY)
            {
                Gizmos.DrawLine(
                    transform.position + Vector3.left * 0.5f,
                    transform.position + Vector3.right * 0.5f
                );
            }
        }
#endif
    }
}

