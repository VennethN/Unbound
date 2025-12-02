using UnityEngine;

namespace Unbound.Utilities
{
    /// <summary>
    /// Adds a floating/bobbing effect to a GameObject.
    /// Useful for pickups, items, UI elements, etc.
    /// </summary>
    public class FloatingEffect : MonoBehaviour
    {
        [Header("Vertical Movement")]
        [Tooltip("How high the object floats up and down")]
        [SerializeField] private float amplitude = 0.25f;
        
        [Tooltip("How fast the object floats")]
        [SerializeField] private float frequency = 1f;
        
        [Header("Horizontal Drift (Optional)")]
        [Tooltip("Horizontal drift amplitude (0 = disabled)")]
        [SerializeField] private float horizontalAmplitude = 0f;
        
        [Tooltip("Horizontal drift frequency")]
        [SerializeField] private float horizontalFrequency = 0.5f;
        
        [Header("Rotation (Optional)")]
        [Tooltip("Rotation speed in degrees per second (0 = disabled)")]
        [SerializeField] private float rotationSpeed = 0f;
        
        [Tooltip("Axis to rotate around")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        
        [Header("Settings")]
        [Tooltip("Randomize starting phase so multiple objects don't sync")]
        [SerializeField] private bool randomizePhase = true;
        
        [Tooltip("Use unscaled time (ignores Time.timeScale)")]
        [SerializeField] private bool useUnscaledTime = false;

        // Runtime
        private Vector3 startPosition;
        private float timeOffset;

        private void Awake()
        {
            startPosition = transform.localPosition;
            
            if (randomizePhase)
            {
                timeOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            float time = useUnscaledTime ? Time.unscaledTime : Time.time;
            time += timeOffset;
            
            // Calculate vertical offset
            float verticalOffset = Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude;
            
            // Calculate horizontal offset
            float horizontalOffset = 0f;
            if (horizontalAmplitude > 0f)
            {
                horizontalOffset = Mathf.Sin(time * horizontalFrequency * Mathf.PI * 2f) * horizontalAmplitude;
            }
            
            // Apply position
            transform.localPosition = startPosition + new Vector3(horizontalOffset, verticalOffset, 0f);
            
            // Apply rotation
            if (rotationSpeed != 0f)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                transform.Rotate(rotationAxis, rotationSpeed * deltaTime, Space.Self);
            }
        }

        /// <summary>
        /// Resets the start position (call after moving the object)
        /// </summary>
        public void ResetStartPosition()
        {
            startPosition = transform.localPosition;
        }

        /// <summary>
        /// Sets float parameters at runtime
        /// </summary>
        public void SetFloatParameters(float newAmplitude, float newFrequency)
        {
            amplitude = newAmplitude;
            frequency = newFrequency;
        }

        /// <summary>
        /// Sets rotation parameters at runtime
        /// </summary>
        public void SetRotation(float speed, Vector3 axis)
        {
            rotationSpeed = speed;
            rotationAxis = axis.normalized;
        }
    }
}

