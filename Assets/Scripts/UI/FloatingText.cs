using UnityEngine;
using TMPro;

namespace Unbound.UI
{
    /// <summary>
    /// All-in-one floating text effect with configurable movement, lifetime, and transparency.
    /// Can be used for damage numbers, exp gains, level ups, pickups, etc.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshPro textMesh;
        
        [Header("Lifetime")]
        [Tooltip("How long before the object is destroyed (0 = never)")]
        [SerializeField] private float lifetime = 1.5f;
        [Tooltip("If true, automatically starts on Awake")]
        [SerializeField] private bool autoStart = true;
        
        [Header("Float Movement")]
        [Tooltip("Enable floating/drifting movement")]
        [SerializeField] private bool enableFloating = true;
        [Tooltip("Upward float speed")]
        [SerializeField] private float floatSpeed = 1.5f;
        [Tooltip("Random horizontal drift range")]
        [SerializeField] private float horizontalDrift = 0.5f;
        [Tooltip("Decelerate movement over time (0 = constant speed, 1 = full stop at end)")]
        [Range(0f, 1f)]
        [SerializeField] private float deceleration = 0.5f;
        
        [Header("Bobbing (Optional)")]
        [Tooltip("Enable sine wave bobbing")]
        [SerializeField] private bool enableBobbing = false;
        [Tooltip("Bobbing amplitude (height)")]
        [SerializeField] private float bobAmplitude = 0.1f;
        [Tooltip("Bobbing frequency (speed)")]
        [SerializeField] private float bobFrequency = 2f;
        
        [Header("Scale Animation")]
        [Tooltip("Enable scale pop animation")]
        [SerializeField] private bool enableScaleAnimation = true;
        [Tooltip("Starting scale multiplier")]
        [SerializeField] private float startScale = 0.5f;
        [Tooltip("Peak scale (overshoot)")]
        [SerializeField] private float peakScale = 1.2f;
        [Tooltip("Final scale")]
        [SerializeField] private float endScale = 0.8f;
        [Tooltip("Time to reach peak scale")]
        [SerializeField] private float scaleUpDuration = 0.15f;
        
        [Header("Transparency / Fade")]
        [Tooltip("Enable fade effect")]
        [SerializeField] private bool enableFade = true;
        [Tooltip("Starting alpha (0 = invisible, 1 = fully visible)")]
        [Range(0f, 1f)]
        [SerializeField] private float startAlpha = 1f;
        [Tooltip("When to start fading out (as fraction of lifetime, 0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float fadeStartTime = 0.6f;
        [Tooltip("Final alpha value")]
        [Range(0f, 1f)]
        [SerializeField] private float endAlpha = 0f;
        [Tooltip("Enable fade IN at start")]
        [SerializeField] private bool fadeIn = false;
        [Tooltip("Fade in duration (seconds)")]
        [SerializeField] private float fadeInDuration = 0.2f;
        
        [Header("Rotation (Optional)")]
        [Tooltip("Enable rotation")]
        [SerializeField] private bool enableRotation = false;
        [Tooltip("Rotation speed (degrees per second)")]
        [SerializeField] private float rotationSpeed = 0f;
        
        // Runtime state
        private float elapsedTime = 0f;
        private Vector3 velocity;
        private Color originalColor;
        private Vector3 originalScale;
        private Vector3 startPosition;
        private bool isInitialized = false;
        private bool isRunning = false;
        private float bobTimeOffset;

        private void Awake()
        {
            if (textMesh == null)
            {
                textMesh = GetComponent<TextMeshPro>();
            }
            
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMeshPro>();
            }
            
            bobTimeOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Start()
        {
            if (autoStart && !isInitialized)
            {
                Initialize();
                isRunning = true;
            }
        }

        /// <summary>
        /// Initializes and starts the floating effect
        /// </summary>
        public void Initialize()
        {
            if (textMesh != null)
            {
                originalColor = textMesh.color;
                
                // Apply start alpha
                if (fadeIn)
                {
                    textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                }
                else
                {
                    textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, startAlpha);
                }
            }
            
            originalScale = transform.localScale;
            startPosition = transform.position;
            
            // Apply start scale
            if (enableScaleAnimation)
            {
                transform.localScale = originalScale * startScale;
            }
            
            // Calculate velocity with random horizontal drift
            if (enableFloating)
            {
                float randomDrift = Random.Range(-horizontalDrift, horizontalDrift);
                velocity = new Vector3(randomDrift, floatSpeed, 0f);
            }
            else
            {
                velocity = Vector3.zero;
            }
            
            elapsedTime = 0f;
            isInitialized = true;
            isRunning = true;
        }

        /// <summary>
        /// Sets up the floating text with custom parameters
        /// </summary>
        public void Setup(string text, Color color, float scale = 1f)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
                originalColor = color;
            }
            
            originalScale = Vector3.one * scale;
            
            if (enableScaleAnimation)
            {
                transform.localScale = originalScale * startScale;
            }
            else
            {
                transform.localScale = originalScale;
            }
            
            Initialize();
        }

        /// <summary>
        /// Sets up with full customization
        /// </summary>
        public void Setup(string text, Color color, float scale, float customLifetime, float customFloatSpeed)
        {
            lifetime = customLifetime;
            floatSpeed = customFloatSpeed;
            Setup(text, color, scale);
        }

        private void Update()
        {
            if (!isInitialized || !isRunning) return;
            
            elapsedTime += Time.deltaTime;
            float normalizedTime = lifetime > 0f ? elapsedTime / lifetime : 0f;
            
            // Movement
            if (enableFloating)
            {
                UpdateMovement(normalizedTime);
            }
            
            // Bobbing
            if (enableBobbing)
            {
                UpdateBobbing();
            }
            
            // Scale animation
            if (enableScaleAnimation)
            {
                UpdateScale(normalizedTime);
            }
            
            // Fade / Transparency
            if (enableFade || fadeIn)
            {
                UpdateFade(normalizedTime);
            }
            
            // Rotation
            if (enableRotation && rotationSpeed != 0f)
            {
                transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }
            
            // Destroy when lifetime is over
            if (lifetime > 0f && elapsedTime >= lifetime)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateMovement(float normalizedTime)
        {
            float speedMultiplier = 1f - (normalizedTime * deceleration);
            transform.position += velocity * speedMultiplier * Time.deltaTime;
        }

        private void UpdateBobbing()
        {
            float bobOffset = Mathf.Sin((Time.time + bobTimeOffset) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            Vector3 pos = transform.position;
            // Apply bobbing relative to current Y (not start position, so it combines with float)
            transform.position = new Vector3(pos.x, pos.y + bobOffset * Time.deltaTime * 60f, pos.z);
        }

        private void UpdateScale(float normalizedTime)
        {
            float targetScale;
            float scaleUpNormalized = lifetime > 0f ? scaleUpDuration / lifetime : 0.1f;
            
            if (normalizedTime < scaleUpNormalized)
            {
                // Scale up phase (pop in)
                float scaleProgress = normalizedTime / scaleUpNormalized;
                targetScale = Mathf.Lerp(startScale, peakScale, EaseOutBack(scaleProgress));
            }
            else
            {
                // Scale down phase (settle)
                float scaleProgress = (normalizedTime - scaleUpNormalized) / (1f - scaleUpNormalized);
                targetScale = Mathf.Lerp(peakScale, endScale, EaseOutQuad(scaleProgress));
            }
            
            transform.localScale = originalScale * targetScale;
        }

        private void UpdateFade(float normalizedTime)
        {
            if (textMesh == null) return;
            
            float alpha = startAlpha;
            
            // Fade in
            if (fadeIn && elapsedTime < fadeInDuration)
            {
                float fadeInProgress = elapsedTime / fadeInDuration;
                alpha = Mathf.Lerp(0f, startAlpha, EaseOutQuad(fadeInProgress));
            }
            // Fade out
            else if (enableFade && normalizedTime > fadeStartTime)
            {
                float fadeProgress = (normalizedTime - fadeStartTime) / (1f - fadeStartTime);
                alpha = Mathf.Lerp(startAlpha, endAlpha, EaseInQuad(fadeProgress));
            }
            
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        #region Public API

        /// <summary>
        /// Pauses the animation
        /// </summary>
        public void Pause()
        {
            isRunning = false;
        }

        /// <summary>
        /// Resumes the animation
        /// </summary>
        public void Resume()
        {
            isRunning = true;
        }

        /// <summary>
        /// Sets the text content
        /// </summary>
        public void SetText(string text)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
            }
        }

        /// <summary>
        /// Sets the text color
        /// </summary>
        public void SetColor(Color color)
        {
            if (textMesh != null)
            {
                originalColor = color;
                textMesh.color = color;
            }
        }

        /// <summary>
        /// Sets lifetime (must be called before Initialize)
        /// </summary>
        public void SetLifetime(float newLifetime)
        {
            lifetime = Mathf.Max(0f, newLifetime);
        }

        /// <summary>
        /// Sets float parameters
        /// </summary>
        public void SetFloatParameters(float speed, float drift)
        {
            floatSpeed = speed;
            horizontalDrift = drift;
        }

        /// <summary>
        /// Sets fade parameters
        /// </summary>
        public void SetFadeParameters(float start, float end, float fadeStart)
        {
            startAlpha = Mathf.Clamp01(start);
            endAlpha = Mathf.Clamp01(end);
            fadeStartTime = Mathf.Clamp01(fadeStart);
        }

        #endregion

        #region Easing Functions

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private float EaseInQuad(float t)
        {
            return t * t;
        }

        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        #endregion
    }
}
