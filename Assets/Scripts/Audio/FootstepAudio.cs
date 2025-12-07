using UnityEngine;
using Unbound.Utility;

namespace Unbound.Audio
{
    public class FootstepAudio : MonoBehaviour
    {
        [Header("Footstep Settings")]
        [SerializeField] private string footstepGroupID = "footsteps";
        [SerializeField] private float stepInterval = 0.4f;
        [SerializeField] private float runningMultiplier = 0.6f;
        [SerializeField, Min(0f)] private float movementThreshold = 0.01f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Surface Types")]
        [SerializeField] private SurfaceType defaultSurface = SurfaceType.Stone;
        [SerializeField] private bool detectSurfaceType = false;

        [Header("Debug")]
        [ReadOnly] [SerializeField] private bool isGroundedDebug;
        [ReadOnly] [SerializeField] private bool isMovingDebug;
        [ReadOnly] [SerializeField] private bool isRunningDebug;
        [ReadOnly] [SerializeField] private float stepTimerDebug;
        [ReadOnly] [SerializeField] private int detectedCollidersCount;
        [ReadOnly] [SerializeField] private SurfaceType currentSurfaceDebug;
        [ReadOnly] [SerializeField] private string lastPlayedGroupID;

        private Rigidbody2D _rigidbody2D;
        private float _stepTimer;
        private bool _isMoving;
        private bool _isRunning;
        private Vector3 _lastPosition;

        public enum SurfaceType
        {
            Stone,
            Wood,
            Grass,
            Water,
            Metal,
            Dirt
        }

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _lastPosition = transform.position;
        }

        private void Update()
        {
            // Check if moving
            if (_rigidbody2D != null)
            {
                Vector2 velocity = _rigidbody2D.linearVelocity;
                _isMoving = velocity.sqrMagnitude > movementThreshold * movementThreshold;
            }
            else
            {
                Vector3 movement = transform.position - _lastPosition;
                movement.y = 0f;
                _isMoving = movement.sqrMagnitude > movementThreshold * movementThreshold;
                _lastPosition = transform.position;
            }

            // Update debug values
            bool grounded = IsGrounded();
            isGroundedDebug = grounded;
            isMovingDebug = _isMoving;
            isRunningDebug = _isRunning;
            stepTimerDebug = _stepTimer;

            if (_isMoving && grounded)
            {
                float interval = _isRunning ? stepInterval * runningMultiplier : stepInterval;

                _stepTimer += Time.deltaTime;
                if (_stepTimer >= interval)
                {
                    _stepTimer = 0f;
                    PlayFootstep();
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        /// <summary>
        /// Checks if the character is grounded
        /// </summary>
        private bool IsGrounded()
        {
            // For top-down 2D games, check if there's any collider nearby (overlap check)
            // This works better than raycasting downward in top-down view
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, groundCheckDistance, groundLayer);
            detectedCollidersCount = colliders.Length;
            
            if (colliders.Length > 0)
            {
                return true;
            }
            
            // Fallback to raycast (works for side-scrolling or if ground is below)
            bool raycastHit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer) ||
                              Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
            
            if (!raycastHit)
            {
                detectedCollidersCount = 0;
            }
            
            return raycastHit;
        }

        /// <summary>
        /// Plays a footstep sound
        /// </summary>
        private void PlayFootstep()
        {
            string groupID = footstepGroupID;

            if (detectSurfaceType)
            {
                SurfaceType surface = DetectSurface();
                currentSurfaceDebug = surface;
                groupID = $"{footstepGroupID}_{surface.ToString().ToLower()}";
            }
            else
            {
                currentSurfaceDebug = defaultSurface;
            }

            lastPlayedGroupID = groupID;

            if (SFXController.Instance != null)
            {
                SFXController.Instance.PlayFromGroup(groupID, transform.position);
            }
            else
            {
                AudioManager.Instance?.PlaySFXOneShot(groupID, transform.position);
            }
        }

        /// <summary>
        /// Detects the current surface type
        /// </summary>
        private SurfaceType DetectSurface()
        {
            // For top-down 2D games, use overlap check to find nearby colliders
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, groundCheckDistance * 2f, groundLayer);
            foreach (Collider2D col in colliders)
            {
                SurfaceTag tag = col.GetComponent<SurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }
            }

            // Fallback to raycast
            RaycastHit2D hit2D = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance * 2f, groundLayer);
            if (hit2D.collider != null)
            {
                SurfaceTag tag = hit2D.collider.GetComponent<SurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }
            }

            // Try 3D raycast
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundLayer))
            {
                SurfaceTag tag = hit.collider.GetComponent<SurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }
            }

            return defaultSurface;
        }

        /// <summary>
        /// Sets the running state
        /// </summary>
        public void SetRunning(bool running)
        {
            _isRunning = running;
        }

        /// <summary>
        /// Manually triggers a footstep sound
        /// </summary>
        public void TriggerFootstep()
        {
            PlayFootstep();
        }
    }
}