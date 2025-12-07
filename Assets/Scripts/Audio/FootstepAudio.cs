using UnityEngine;

namespace Unbound.Audio
{
    public class FootstepAudio : MonoBehaviour
    {
        [Header("Footstep Settings")]
        [SerializeField] private string footstepGroupID = "footsteps";
        [SerializeField] private float stepInterval = 0.4f;
        [SerializeField] private float runningMultiplier = 0.6f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Surface Types")]
        [SerializeField] private SurfaceType defaultSurface = SurfaceType.Stone;
        [SerializeField] private bool detectSurfaceType = false;

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

        private void Update()
        {
            // Check if moving
            Vector3 movement = transform.position - _lastPosition;
            movement.y = 0f;
            _isMoving = movement.sqrMagnitude > 0.0001f;
            _lastPosition = transform.position;

            if (_isMoving && IsGrounded())
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
            return Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer) ||
                   Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
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
                groupID = $"{footstepGroupID}_{surface.ToString().ToLower()}";
            }

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
            // Try 2D raycast first
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