using System.Collections.Generic;
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

        [Tooltip("If true, automatically finds and uses CameraBoundsHelper instances in the scene")]
        [SerializeField] private bool useDynamicBounds = true;

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
            // Check if target is in any bounds zone
            if (useBounds && !IsTargetInAnyBoundsZone())
            {
                // Target is not in any bounds zone - stop following
                return;
            }

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
            if (useBounds)
            {
                CameraBounds effectiveBounds = GetEffectiveBounds();
                if (effectiveBounds.Size != Vector3.zero)
                {
                    newPosition = effectiveBounds.ClampPosition(newPosition);
                }
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

            if (useBounds)
            {
                CameraBounds effectiveBounds = GetEffectiveBounds();
                if (effectiveBounds.Size != Vector3.zero)
                {
                    newPosition = effectiveBounds.ClampPosition(newPosition);
                }
            }

            transform.position = newPosition;
        }

        /// <summary>
        /// Checks if the camera is currently within bounds.
        /// </summary>
        public bool IsWithinBounds()
        {
            if (!useBounds) return true;
            CameraBounds effectiveBounds = GetEffectiveBounds();
            return effectiveBounds.Size == Vector3.zero || effectiveBounds.Contains(transform.position);
        }

        /// <summary>
        /// Checks if the target position is within any bounds zone.
        /// </summary>
        private bool IsTargetInAnyBoundsZone()
        {
            if (!useDynamicBounds)
            {
                // If not using dynamic bounds, check serialized bounds
                return cameraBounds.Size == Vector3.zero || cameraBounds.Contains(_targetPosition);
            }

            CameraBoundsHelper[] helpers = FindObjectsByType<CameraBoundsHelper>(FindObjectsSortMode.None);
            if (helpers == null || helpers.Length == 0)
            {
                // No helpers found, fall back to serialized bounds
                return cameraBounds.Size == Vector3.zero || cameraBounds.Contains(_targetPosition);
            }

            foreach (var helper in helpers)
            {
                if (helper == null) continue;
                CameraBounds bounds = helper.Bounds;
                if (bounds.Size == Vector3.zero) continue;

                if (bounds.Contains(_targetPosition))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the effective bounds to use, either from dynamic lookup or the serialized bounds.
        /// Only returns bounds if the target is within a bounds zone.
        /// </summary>
        private CameraBounds GetEffectiveBounds()
        {
            if (useDynamicBounds)
            {
                CameraBoundsHelper[] helpers = FindObjectsByType<CameraBoundsHelper>(FindObjectsSortMode.None);
                if (helpers != null && helpers.Length > 0)
                {
                    // Find bounds that contain the target position
                    List<CameraBounds> containingBounds =
                        new List<CameraBounds>();

                    foreach (var helper in helpers)
                    {
                        if (helper == null) continue;
                        CameraBounds bounds = helper.Bounds;
                        if (bounds.Size == Vector3.zero) continue;

                        // Check if this bounds zone contains the target position
                        if (bounds.Contains(_targetPosition))
                        {
                            containingBounds.Add(bounds);
                        }
                    }

                    if (containingBounds.Count > 0)
                    {
                        // Use union of all containing bounds (allows movement between overlapping zones)
                        return CameraBounds.Union(containingBounds.ToArray());
                    }
                }
            }

            // Fall back to serialized bounds (only if target is in them)
            if (cameraBounds.Size != Vector3.zero && cameraBounds.Contains(_targetPosition))
            {
                return cameraBounds;
            }

            // Return empty bounds if target is not in any zone
            return new CameraBounds(0f, 0f, 0f, 0f);
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
            if (!useBounds) return;

            CameraBounds effectiveBounds = GetEffectiveBounds();
            if (effectiveBounds.Size == Vector3.zero) return;

            Gizmos.color = Color.yellow;
            Vector3 center = effectiveBounds.Center;
            Vector3 size = effectiveBounds.Size;

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
