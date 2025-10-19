using UnityEngine;
using UnityEditor;

namespace Unbound.Camera
{
    /// <summary>
    /// Example script showing how to set up the BoundedCameraController.
    /// This script can be used to automatically configure the camera in your scene.
    /// </summary>
    public class CameraSetupExample : MonoBehaviour
    {
        [Header("Setup Configuration")]
        [Tooltip("Camera bounds width (X-axis)")]
        [SerializeField] private float boundsWidth = 20f;

        [Tooltip("Camera bounds height (Y-axis)")]
        [SerializeField] private float boundsHeight = 15f;

        [Tooltip("Camera offset from player")]
        [SerializeField] private Vector3 cameraOffset = Vector3.zero;

        private void Awake()
        {
            // This script sets up the camera controller automatically
            // You can remove this script after setup if desired
            SetupCameraController();
        }

        /// <summary>
        /// Sets up the camera controller with the specified settings.
        /// </summary>
        public void SetupCameraController()
        {
            // Find the main camera
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("CameraSetupExample: No main camera found in the scene!");
                return;
            }

            // Add the BoundedCameraController component if it doesn't exist
            BoundedCameraController cameraController = mainCamera.GetComponent<BoundedCameraController>();
            if (cameraController == null)
            {
                cameraController = mainCamera.gameObject.AddComponent<BoundedCameraController>();
                Debug.Log("CameraSetupExample: Added BoundedCameraController to Main Camera");
            }

            // Note: The BoundedCameraController uses serialized fields for configuration
            // These are set in the Inspector or can be modified directly
            // For runtime changes, you would need to add public setter methods to BoundedCameraController

            // Set up camera bounds
            SetupCameraBounds();

            Debug.Log("CameraSetupExample: Camera controller setup complete!");
        }

        /// <summary>
        /// Sets up camera bounds in the scene.
        /// </summary>
        public void SetupCameraBounds()
        {
            // Try to find existing CameraBoundsHelper
            CameraBoundsHelper boundsHelper = FindFirstObjectByType<CameraBoundsHelper>();

            if (boundsHelper == null)
            {
                // Create a new GameObject for camera bounds
                GameObject boundsObject = new GameObject("CameraBounds");
                boundsHelper = boundsObject.AddComponent<CameraBoundsHelper>();
                Debug.Log("CameraSetupExample: Created new CameraBounds GameObject");
            }

            // Configure the bounds
            boundsHelper.SetSize(boundsWidth, boundsHeight);

            // Get the main camera
            UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
            if (mainCamera != null)
            {
                BoundedCameraController cameraController = mainCamera.GetComponent<BoundedCameraController>();
                if (cameraController != null)
                {
                    cameraController.SetBounds(boundsHelper.Bounds);
                    Debug.Log($"CameraSetupExample: Set camera bounds to {boundsWidth}x{boundsHeight}");
                }
            }
        }

    }

#if UNITY_EDITOR
    /// <summary>
    /// Custom editor for the CameraSetupExample to provide setup buttons.
    /// </summary>
    [CustomEditor(typeof(CameraSetupExample))]
    public class CameraSetupExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CameraSetupExample setup = (CameraSetupExample)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Setup Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Setup Camera Controller"))
            {
                setup.SetupCameraController();
            }

            if (GUILayout.Button("Setup Camera Bounds Only"))
            {
                setup.SetupCameraBounds();
            }

            if (GUILayout.Button("Find Player and Setup"))
            {
                // Force find player target
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    BoundedCameraController controller = mainCamera.GetComponent<BoundedCameraController>();
                    if (controller != null)
                    {
                        controller.FindPlayerTarget();
                        Debug.Log("CameraSetupExample: Player target found and set");
                    }
                }
            }
        }
    }
#endif
}
