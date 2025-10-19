using UnityEngine;
using Unbound.Player;

namespace Unbound.Camera
{
    /// <summary>
    /// Controls a camera that follows the player but stays within specified bounds.
    /// The camera smoothly follows the player and respects the map boundaries.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class BoundedCameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        [Tooltip("The target transform to follow (usually the player)")]
        [SerializeField] private Transform target;

        [Header("Follow Settings")]
        [Tooltip("How smoothly the camera follows the target (higher values = more responsive)")]
        [SerializeField, Range(0.1f, 20f)] private float followSpeed = 5f;

        [Tooltip("Offset from the target's position")]
        [SerializeField] private Vector3 offset = Vector3.zero;

        [Tooltip("If true, camera will maintain a constant Z position")]
        [SerializeField] private bool lockZPosition = true;

        [Header("Bounds Settings")]
        [Tooltip("Camera movement boundaries. Leave empty to disable bounds")]
        [SerializeField] private CameraBounds cameraBounds;

        [Tooltip("If true, bounds are used. If false, camera can move freely")]
        [SerializeField] private bool useBounds = true;

        [Header("Advanced Settings")]
        [Tooltip("Minimum distance the target must move before camera updates")]
        [SerializeField, Min(0f)] private float positionThreshold = 0.01f;

        [Tooltip("Camera follow behavior mode")]
        [SerializeField] private FollowMode followMode = FollowMode.SmoothDamp;

        private UnityEngine.Camera _camera;
        private Vector3 _currentVelocity;
        private Vector3 _targetPosition;
        private bool _hasValidTarget;

        public enum FollowMode
        {
            SmoothDamp,     // Smooth, spring-like following
            Lerp,          // Linear interpolation
            Instant        // Immediate following (teleporting)
        }

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
            if (target == null)
            {
                // Try to find the player automatically
                FindPlayerTarget();
            }
        }

        private void Start()
        {
            UpdateTargetPosition();
        }

        private void LateUpdate()
        {
            if (!_hasValidTarget || target == null)
            {
                FindPlayerTarget();
                return;
            }

            UpdateTargetPosition();
            MoveCamera();
        }

        /// <summary>
        /// Automatically finds the player object as the camera target.
        /// </summary>
        public void FindPlayerTarget()
        {
            PlayerController2D playerController = FindFirstObjectByType<PlayerController2D>();
            if (playerController != null)
            {
                target = playerController.transform;
                _hasValidTarget = true;
            }
            else
            {
                _hasValidTarget = false;
                Debug.LogWarning("BoundedCameraController: No player target found. Camera will not follow anything.");
            }
        }

        /// <summary>
        /// Updates the target position the camera should move towards.
        /// </summary>
        private void UpdateTargetPosition()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            if (lockZPosition)
            {
                desiredPosition.z = transform.position.z;
            }

            _targetPosition = desiredPosition;
        }

        /// <summary>
        /// Moves the camera towards the target position based on the follow mode.
        /// </summary>
        private void MoveCamera()
        {
            if (Vector3.Distance(transform.position, _targetPosition) < positionThreshold)
                return;

            Vector3 newPosition = transform.position;

            switch (followMode)
            {
                case FollowMode.SmoothDamp:
                    newPosition = Vector3.SmoothDamp(
                        transform.position,
                        _targetPosition,
                        ref _currentVelocity,
                        1f / followSpeed,
                        Mathf.Infinity,
                        Time.deltaTime
                    );
                    break;

                case FollowMode.Lerp:
                    newPosition = Vector3.Lerp(
                        transform.position,
                        _targetPosition,
                        followSpeed * Time.deltaTime
                    );
                    break;

                case FollowMode.Instant:
                    newPosition = _targetPosition;
                    break;
            }

            // Apply bounds if enabled
            if (useBounds && cameraBounds.Size != Vector3.zero)
            {
                newPosition = cameraBounds.ClampPosition(newPosition);
            }

            transform.position = newPosition;
        }

        /// <summary>
        /// Sets a new target for the camera to follow.
        /// </summary>
        /// <param name="newTarget">The new target transform</param>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            _hasValidTarget = target != null;
        }

        /// <summary>
        /// Sets new camera bounds.
        /// </summary>
        /// <param name="bounds">The new camera bounds</param>
        /// <param name="enable">Whether to use the bounds</param>
        public void SetBounds(CameraBounds bounds, bool enable = true)
        {
            cameraBounds = bounds;
            useBounds = enable;
        }

        /// <summary>
        /// Gets the current camera bounds.
        /// </summary>
        public CameraBounds GetBounds() => cameraBounds;

        /// <summary>
        /// Centers the camera on the target immediately.
        /// </summary>
        public void CenterOnTarget()
        {
            if (target == null) return;

            Vector3 newPosition = target.position + offset;

            if (lockZPosition)
            {
                newPosition.z = transform.position.z;
            }

            if (useBounds && cameraBounds.Size != Vector3.zero)
            {
                newPosition = cameraBounds.ClampPosition(newPosition);
            }

            transform.position = newPosition;
        }

        /// <summary>
        /// Checks if the camera is currently within bounds.
        /// </summary>
        public bool IsWithinBounds()
        {
            return !useBounds || cameraBounds.Contains(transform.position);
        }

        /// <summary>
        /// Gets the camera's orthographic size (for 2D cameras) or field of view (for 3D cameras).
        /// </summary>
        public float GetCameraSize()
        {
            if (_camera.orthographic)
                return _camera.orthographicSize;
            else
                return _camera.fieldOfView;
        }

        private void OnValidate()
        {
            followSpeed = Mathf.Max(0.1f, followSpeed);
            positionThreshold = Mathf.Max(0f, positionThreshold);
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds || cameraBounds.Size == Vector3.zero) return;

            Gizmos.color = Color.yellow;
            Vector3 center = cameraBounds.Center;
            Vector3 size = cameraBounds.Size;

            // Draw bounds rectangle
            Gizmos.DrawWireCube(center, size);

            // Draw camera position indicator
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(transform.position, 0.1f);

            // Draw line from camera to target if target exists
            if (target != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
