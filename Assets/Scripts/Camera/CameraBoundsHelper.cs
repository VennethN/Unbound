using UnityEngine;

namespace Unbound.Camera
{
    /// <summary>
    /// Helper component for easily defining camera bounds in the editor.
    /// Place this component on an empty GameObject to define camera boundaries.
    /// </summary>
    public class CameraBoundsHelper : MonoBehaviour
    {
        [Header("Camera Bounds Settings")]
        [Tooltip("Width of the camera bounds area")]
        [SerializeField, Min(0f)] private float width = 10f;

        [Tooltip("Height of the camera bounds area")]
        [SerializeField, Min(0f)] private float height = 10f;

        [Tooltip("Color of the bounds visualization in the editor")]
        [SerializeField] private Color boundsColor = Color.yellow;

        [Tooltip("Whether to show bounds visualization in the editor")]
        [SerializeField] private bool showBounds = true;

        [Header("Automation")]
        [Tooltip("Automatically push these bounds to every BoundedCameraController in the scene. " +
                 "Note: If controllers use dynamic bounds, they will automatically find this helper.")]
        [SerializeField] private bool autoApplyToCameraControllers = false;

        private CameraBounds _cameraBounds;

        /// <summary>
        /// Gets the camera bounds defined by this helper.
        /// </summary>
        public CameraBounds Bounds
        {
            get
            {
                if (_cameraBounds.Size == Vector3.zero)
                {
                    UpdateBounds();
                }
                return _cameraBounds;
            }
        }

        private void Awake()
        {
            UpdateBounds();
            ApplyBoundsToControllers();
        }

        private void OnEnable()
        {
            ApplyBoundsToControllers();
        }

        private void UpdateBounds()
        {
            Vector3 position = transform.position;
            _cameraBounds = new CameraBounds(
                position.x - width / 2f,
                position.x + width / 2f,
                position.y - height / 2f,
                position.y + height / 2f
            );
        }

        /// <summary>
        /// Sets the bounds size.
        /// </summary>
        /// <param name="newWidth">New width</param>
        /// <param name="newHeight">New height</param>
        public void SetSize(float newWidth, float newHeight)
        {
            width = Mathf.Max(0f, newWidth);
            height = Mathf.Max(0f, newHeight);
            UpdateBounds();
            ApplyBoundsToControllers();
        }

        /// <summary>
        /// Centers the bounds on a specific position.
        /// </summary>
        /// <param name="centerPosition">Position to center on</param>
        public void CenterOn(Vector3 centerPosition)
        {
            transform.position = new Vector3(centerPosition.x, centerPosition.y, transform.position.z);
            UpdateBounds();
            ApplyBoundsToControllers();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0f, width);
            height = Mathf.Max(0f, height);
            UpdateBounds();
            ApplyBoundsToControllers();
        }

        private void OnDrawGizmos()
        {
            if (!showBounds) return;

            UpdateBounds();

            Gizmos.color = boundsColor;
            Vector3 center = _cameraBounds.Center;
            Vector3 size = _cameraBounds.Size;

            // Draw the bounds rectangle
            Gizmos.DrawWireCube(center, size);

            // Draw corner indicators
            float cornerSize = 0.2f;
            Vector3 topRight = new Vector3(_cameraBounds.maxX, _cameraBounds.maxY, center.z);
            Vector3 topLeft = new Vector3(_cameraBounds.minX, _cameraBounds.maxY, center.z);
            Vector3 bottomRight = new Vector3(_cameraBounds.maxX, _cameraBounds.minY, center.z);
            Vector3 bottomLeft = new Vector3(_cameraBounds.minX, _cameraBounds.minY, center.z);

            Gizmos.DrawWireCube(topRight, Vector3.one * cornerSize);
            Gizmos.DrawWireCube(topLeft, Vector3.one * cornerSize);
            Gizmos.DrawWireCube(bottomRight, Vector3.one * cornerSize);
            Gizmos.DrawWireCube(bottomLeft, Vector3.one * cornerSize);

            // Draw center point
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(center, cornerSize * 0.5f);

            // Draw labels
            Gizmos.color = Color.white;
            // Note: Unity doesn't have built-in text drawing in Gizmos, so we'll skip labels for now
        }

        private void ApplyBoundsToControllers()
        {
            if (!autoApplyToCameraControllers)
                return;

            CameraBounds currentBounds = Bounds;
            BoundedCameraController[] controllers = FindObjectsOfType<BoundedCameraController>(true);
            if (controllers == null || controllers.Length == 0)
                return;

            foreach (BoundedCameraController controller in controllers)
            {
                if (controller == null)
                    continue;

                controller.SetBounds(currentBounds, true);
            }
        }
    }
}
