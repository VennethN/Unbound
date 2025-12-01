using UnityEngine;

namespace Unbound.Utilities
{
    /// <summary>
    /// Simple utility component that destroys its GameObject after a specified time.
    /// Useful for temporary effects, particles, sounds, etc.
    /// </summary>
    public class SelfDestroy : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Time in seconds before the GameObject is destroyed")]
        [SerializeField] private float lifetime = 2f;
        
        [Tooltip("If true, starts countdown on Awake. If false, call StartCountdown() manually.")]
        [SerializeField] private bool destroyOnAwake = true;

        private void Awake()
        {
            if (destroyOnAwake)
            {
                Destroy(gameObject, lifetime);
            }
        }

        /// <summary>
        /// Starts the destruction countdown manually
        /// </summary>
        public void StartCountdown()
        {
            Destroy(gameObject, lifetime);
        }

        /// <summary>
        /// Starts countdown with a custom time
        /// </summary>
        public void StartCountdown(float customLifetime)
        {
            Destroy(gameObject, customLifetime);
        }

        /// <summary>
        /// Sets the lifetime (only effective before countdown starts)
        /// </summary>
        public void SetLifetime(float newLifetime)
        {
            lifetime = Mathf.Max(0f, newLifetime);
        }
    }
}

