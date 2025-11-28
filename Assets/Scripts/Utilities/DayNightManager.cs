using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Manages the day/night cycle by controlling the Global Light 2D component.
/// Smoothly transitions between day and night lighting based on time.
/// </summary>
public class DayNightManager : MonoBehaviour
{
    private static DayNightManager _instance;
    public static DayNightManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DayNightManager");
                _instance = go.AddComponent<DayNightManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Day/Night Cycle Settings")]
    [Tooltip("If true, day/night cycle will be enabled on start. If false, it will be paused.")]
    [SerializeField] private bool startEnabled = true;
    
    [Tooltip("Duration of a full day/night cycle in seconds (real time)")]
    [SerializeField] private float cycleDuration = 120f; // 2 minutes for a full cycle
    
    [Tooltip("Time of day (0 = midnight, 0.25 = dawn, 0.5 = noon, 0.75 = dusk, 1 = midnight)")]
    [Range(0f, 1f)]
    [SerializeField] private float timeOfDay = 0.5f; // Start at noon
    
    [Header("Day Settings")]
    [SerializeField] private Color dayColor = new Color(1f, 0.956f, 0.839f, 1f); // Warm daylight
    [SerializeField] private float dayIntensity = 1f;
    
    [Header("Night Settings")]
    [SerializeField] private Color nightColor = new Color(0.2f, 0.2f, 0.4f, 1f); // Cool moonlight
    [SerializeField] private float nightIntensity = 0.3f;
    
    [Header("Dawn/Dusk Settings")]
    [Tooltip("Color during dawn/dusk transitions")]
    [SerializeField] private Color dawnDuskColor = new Color(1f, 0.6f, 0.4f, 1f); // Orange/red
    [Tooltip("Duration of dawn/dusk transition as fraction of cycle (0-0.5)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float dawnDuskDuration = 0.15f; // 15% of cycle
    
    [Header("References")]
    [Tooltip("Auto-find Global Light 2D if not assigned")]
    [SerializeField] private Light2D globalLight2D;
    
    [Header("Runtime Info (Read-Only)")]
    [SerializeField] private bool isDayTime;
    [SerializeField] private float currentCycleProgress;
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set initial enabled state
        enabled = startEnabled;
        
        // Find Global Light 2D if not assigned
        if (globalLight2D == null)
        {
            globalLight2D = FindFirstObjectByType<Light2D>();
            if (globalLight2D == null)
            {
                Debug.LogWarning("DayNightManager: No Light2D component found. Please assign a Global Light 2D or add one to the scene.");
            }
        }
    }
    
    private void Start()
    {
        UpdateLighting();
    }
    
    private void Update()
    {
        // Advance time
        timeOfDay += Time.deltaTime / cycleDuration;
        
        // Wrap around at 1.0
        if (timeOfDay >= 1f)
        {
            timeOfDay -= 1f;
        }
        
        UpdateLighting();
    }
    
    /// <summary>
    /// Updates the lighting based on current time of day
    /// </summary>
    private void UpdateLighting()
    {
        if (globalLight2D == null) return;
        
        currentCycleProgress = timeOfDay;
        
        // Determine if it's day or night
        // Day: 0.25 to 0.75 (6 AM to 6 PM)
        // Night: 0.75 to 0.25 (6 PM to 6 AM)
        isDayTime = timeOfDay >= 0.25f && timeOfDay < 0.75f;
        
        Color targetColor;
        float targetIntensity;
        
        // Calculate lighting based on time of day
        if (timeOfDay < 0.25f) // Midnight to Dawn (0.0 - 0.25)
        {
            // Night to Dawn transition
            float t = timeOfDay / 0.25f;
            targetColor = Color.Lerp(nightColor, dawnDuskColor, t);
            targetIntensity = Mathf.Lerp(nightIntensity, dayIntensity * 0.7f, t);
        }
        else if (timeOfDay < 0.25f + dawnDuskDuration) // Dawn (0.25 - 0.25+duration)
        {
            // Dawn transition
            float t = (timeOfDay - 0.25f) / dawnDuskDuration;
            targetColor = Color.Lerp(dawnDuskColor, dayColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity * 0.7f, dayIntensity, t);
        }
        else if (timeOfDay < 0.75f - dawnDuskDuration) // Day (0.25+duration - 0.75-duration)
        {
            // Full day
            targetColor = dayColor;
            targetIntensity = dayIntensity;
        }
        else if (timeOfDay < 0.75f) // Dusk (0.75-duration - 0.75)
        {
            // Day to Dusk transition
            float t = (timeOfDay - (0.75f - dawnDuskDuration)) / dawnDuskDuration;
            targetColor = Color.Lerp(dayColor, dawnDuskColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity, dayIntensity * 0.7f, t);
        }
        else // Dusk to Midnight (0.75 - 1.0)
        {
            // Dusk to Night transition
            float t = (timeOfDay - 0.75f) / 0.25f;
            targetColor = Color.Lerp(dawnDuskColor, nightColor, t);
            targetIntensity = Mathf.Lerp(dayIntensity * 0.7f, nightIntensity, t);
        }
        
        // Apply to light
        globalLight2D.color = targetColor;
        globalLight2D.intensity = targetIntensity;
    }
    
    /// <summary>
    /// Sets the time of day (0-1, where 0 is midnight, 0.5 is noon)
    /// </summary>
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
        UpdateLighting();
    }
    
    /// <summary>
    /// Gets the current time of day (0-1)
    /// </summary>
    public float GetTimeOfDay()
    {
        return timeOfDay;
    }
    
    /// <summary>
    /// Returns true if it's currently day time
    /// </summary>
    public bool IsDayTime()
    {
        return isDayTime;
    }
    
    /// <summary>
    /// Sets the cycle duration in seconds
    /// </summary>
    public void SetCycleDuration(float duration)
    {
        cycleDuration = Mathf.Max(0.1f, duration);
    }
    
    /// <summary>
    /// Manually assign the Global Light 2D component
    /// </summary>
    public void SetGlobalLight(Light2D light)
    {
        globalLight2D = light;
        UpdateLighting();
    }
    
    /// <summary>
    /// Pauses the day/night cycle
    /// </summary>
    public void PauseCycle()
    {
        enabled = false;
    }
    
    /// <summary>
    /// Resumes the day/night cycle
    /// </summary>
    public void ResumeCycle()
    {
        enabled = true;
    }
}

