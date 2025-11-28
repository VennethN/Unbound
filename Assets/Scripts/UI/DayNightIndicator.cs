using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unbound.UI
{
    /// <summary>
    /// Rotates a UI element (day/night indicator) based on the current time of day.
    /// Finds the indicator by tag in each scene. Manager persists, but indicator is found fresh per scene.
    /// </summary>
    public class DayNightIndicator : MonoBehaviour
    {
        private static DayNightIndicator _instance;
        public static DayNightIndicator Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("DayNightIndicator");
                    _instance = go.AddComponent<DayNightIndicator>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Tag Settings")]
        [Tooltip("Tag to search for the day/night indicator UI element")]
        [SerializeField] private string indicatorTag = "DayNightIndicator";
        
        [Header("Pivot Settings")]
        [Tooltip("Set pivot point for UI element (0.5, 1.0) = top-center, (0.5, 0.5) = center, (0.5, 0.0) = bottom-center")]
        [SerializeField] private Vector2 pivotPoint = new Vector2(0.5f, 1f); // Top-center by default
        
        [Header("Rotation Settings")]
        [Tooltip("Starting rotation offset in degrees (adjust if day/night sections need alignment)")]
        [SerializeField] private float rotationOffset = 0f;
        
        [Tooltip("Rotation direction: true = clockwise, false = counter-clockwise")]
        [SerializeField] private bool clockwise = true;
        
        [Tooltip("Full rotation angle for a complete cycle (360 = full rotation, 180 = half rotation)")]
        [SerializeField] private float fullRotationAngle = 180f;
        
        [Header("Smoothing")]
        [Tooltip("Smooth rotation speed (higher = faster, 0 = instant)")]
        [SerializeField] private float rotationSmoothing = 5f;
        
        [Header("References")]
        [Tooltip("Auto-find DayNightManager if not assigned")]
        [SerializeField] private DayNightManager dayNightManager;
        
        private RectTransform rectTransform;
        private Transform worldTransform;
        private GameObject indicatorObject;
        private float targetRotation;
        private float currentRotation;
        private string currentSceneName;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Find DayNightManager if not assigned
            if (dayNightManager == null)
            {
                dayNightManager = DayNightManager.Instance;
            }
            
            // Subscribe to scene loaded event to re-find indicator on scene changes
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from scene loaded event
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Reset indicator reference when scene changes
            indicatorObject = null;
            rectTransform = null;
            worldTransform = null;
            currentSceneName = scene.name;
            
            // Find the indicator in the new scene
            FindIndicator();
        }
        
        private void Start()
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            FindIndicator();
        }
        
        private void Update()
        {
            if (dayNightManager == null) return;
            
            // Check if scene changed (fallback check)
            string activeSceneName = SceneManager.GetActiveScene().name;
            if (activeSceneName != currentSceneName)
            {
                OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
            }
            
            // Try to find indicator if we don't have a reference
            if (indicatorObject == null || (rectTransform == null && worldTransform == null))
            {
                FindIndicator();
            }
            
            if (indicatorObject == null) return;
            
            UpdateRotation();
            
            // Smooth rotation
            if (rotationSmoothing > 0f)
            {
                currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSmoothing);
            }
            else
            {
                currentRotation = targetRotation;
            }
            
            ApplyRotation();
        }
        
        /// <summary>
        /// Finds the indicator UI element by tag
        /// </summary>
        private void FindIndicator()
        {
            GameObject found = GameObject.FindGameObjectWithTag(indicatorTag);
            
            if (found == null)
            {
                // Only log warning once per scene load to avoid spam
                if (indicatorObject == null)
                {
                    Debug.LogWarning($"[DayNightIndicator] No GameObject found with tag '{indicatorTag}'. Make sure your day/night UI element has this tag.");
                }
                indicatorObject = null;
                rectTransform = null;
                worldTransform = null;
                return;
            }
            
            indicatorObject = found;
            
            // Try to get RectTransform (for UI elements)
            rectTransform = indicatorObject.GetComponent<RectTransform>();
            
            // If RectTransform found, set pivot point
            if (rectTransform != null)
            {
                rectTransform.pivot = pivotPoint;
            }
            
            // If no RectTransform, try regular Transform (for world space sprites)
            if (rectTransform == null)
            {
                worldTransform = indicatorObject.transform;
            }
            
            // Initialize rotation when found
            if (dayNightManager != null)
            {
                UpdateRotation();
                currentRotation = targetRotation;
                ApplyRotation();
            }
        }
        
        /// <summary>
        /// Calculates the target rotation based on time of day
        /// </summary>
        private void UpdateRotation()
        {
            if (dayNightManager == null) return;
            
            float timeOfDay = dayNightManager.GetTimeOfDay();
            
            // Calculate rotation based on time of day
            // timeOfDay: 0 = midnight, 0.5 = noon, 1 = midnight
            // We want: 0 = start position, 0.5 = 180 degrees, 1 = 360 degrees (or back to 0)
            float rotationProgress = timeOfDay * fullRotationAngle;
            
            if (!clockwise)
            {
                rotationProgress = -rotationProgress;
            }
            
            targetRotation = rotationOffset + rotationProgress;
        }
        
        /// <summary>
        /// Applies the rotation to the transform
        /// </summary>
        private void ApplyRotation()
        {
            if (rectTransform != null)
            {
                // For UI elements (RectTransform)
                rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
            }
            else if (worldTransform != null)
            {
                // For world space sprites (Transform)
                worldTransform.localRotation = Quaternion.Euler(0f, 0f, currentRotation);
            }
        }
        
        /// <summary>
        /// Sets the tag to search for the indicator
        /// </summary>
        public void SetIndicatorTag(string tag)
        {
            indicatorTag = tag;
            FindIndicator();
        }
        
        /// <summary>
        /// Sets the rotation offset (useful for aligning the indicator)
        /// </summary>
        public void SetRotationOffset(float offset)
        {
            rotationOffset = offset;
        }
        
        /// <summary>
        /// Sets whether rotation should be clockwise
        /// </summary>
        public void SetClockwise(bool isClockwise)
        {
            clockwise = isClockwise;
        }
        
        /// <summary>
        /// Sets the full rotation angle for a complete cycle
        /// </summary>
        public void SetFullRotationAngle(float angle)
        {
            fullRotationAngle = angle;
        }
        
        /// <summary>
        /// Manually assign the DayNightManager reference
        /// </summary>
        public void SetDayNightManager(DayNightManager manager)
        {
            dayNightManager = manager;
        }
        
        /// <summary>
        /// Forces a refresh to find the indicator (useful after scene changes)
        /// </summary>
        public void RefreshIndicator()
        {
            FindIndicator();
        }
    }
}

