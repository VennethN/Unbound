using UnityEngine;
using TMPro;
using Unbound.Player;

namespace Unbound.UI
{
    /// <summary>
    /// Manages spawning floating text notifications for experience gains and level ups.
    /// Listens to the LevelingSystem events and spawns appropriate notifications.
    /// </summary>
    public class ExpNotificationManager : MonoBehaviour
    {
        private static ExpNotificationManager _instance;
        public static ExpNotificationManager Instance => _instance;

        [Header("Prefab References")]
        [Tooltip("Prefab for floating text. If null, will create one at runtime.")]
        [SerializeField] private GameObject floatingTextPrefab;
        
        [Header("Spawn Settings")]
        [Tooltip("Offset from player position where text spawns")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 1f, 0f);
        [Tooltip("Random offset range for varied spawn positions")]
        [SerializeField] private float randomOffsetRange = 0.3f;
        
        [Header("EXP Gain Settings")]
        [SerializeField] private Color expGainColor = new Color(0.4f, 0.9f, 1f, 1f); // Cyan/light blue
        [SerializeField] private float expGainScale = 0.5f;
        [SerializeField] private string expGainPrefix = "+";
        [SerializeField] private string expGainSuffix = " EXP";
        
        [Header("Level Up Settings")]
        [SerializeField] private Color levelUpColor = new Color(1f, 0.85f, 0.2f, 1f); // Gold
        [SerializeField] private float levelUpScale = 0.8f;
        [SerializeField] private string levelUpText = "LEVEL UP!";
        [SerializeField] private bool showNewLevelNumber = true;
        
        [Header("Animation Overrides")]
        [SerializeField] private float expGainLifetime = 1.2f;
        [SerializeField] private float levelUpLifetime = 2f;
        [SerializeField] private float expGainFloatSpeed = 1f;
        [SerializeField] private float levelUpFloatSpeed = 0.8f;
        
        [Header("Font Settings")]
        [SerializeField] private TMP_FontAsset customFont;
        [SerializeField] private FontStyles expFontStyle = FontStyles.Bold;
        [SerializeField] private FontStyles levelUpFontStyle = FontStyles.Bold;
        
        // Runtime
        private Transform playerTransform;
        private LevelingSystem levelingSystem;
        
        // Flag to skip next exp notification (used when enemy spawns its own floating text)
        private bool skipNextExpNotification = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            // Find player
            FindPlayer();
            
            // Subscribe to leveling events
            SubscribeToLevelingEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLevelingEvents();
        }

        private void FindPlayer()
        {
            // Try to find player by tag
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                return;
            }
            
            // Try to find by PlayerController2D
            var playerController = FindFirstObjectByType<PlayerController2D>();
            if (playerController != null)
            {
                playerTransform = playerController.transform;
            }
        }

        private void SubscribeToLevelingEvents()
        {
            levelingSystem = LevelingSystem.Instance;
            if (levelingSystem != null)
            {
                levelingSystem.OnExpGained.AddListener(OnExpGained);
                levelingSystem.OnLevelUp.AddListener(OnLevelUp);
            }
            else
            {
                // Try again after a short delay (LevelingSystem might not be initialized yet)
                Invoke(nameof(RetrySubscription), 0.1f);
            }
        }

        private void RetrySubscription()
        {
            if (levelingSystem == null)
            {
                levelingSystem = LevelingSystem.Instance;
                if (levelingSystem != null)
                {
                    levelingSystem.OnExpGained.AddListener(OnExpGained);
                    levelingSystem.OnLevelUp.AddListener(OnLevelUp);
                }
            }
        }

        private void UnsubscribeFromLevelingEvents()
        {
            if (levelingSystem != null)
            {
                levelingSystem.OnExpGained.RemoveListener(OnExpGained);
                levelingSystem.OnLevelUp.RemoveListener(OnLevelUp);
            }
        }

        /// <summary>
        /// Called when exp is gained
        /// </summary>
        private void OnExpGained(int currentExp, int expGained)
        {
            if (expGained <= 0) return;
            
            // Skip if an enemy already spawned floating text for this exp gain
            if (skipNextExpNotification)
            {
                skipNextExpNotification = false;
                return;
            }
            
            string text = $"{expGainPrefix}{expGained}{expGainSuffix}";
            SpawnFloatingText(text, expGainColor, expGainScale, expGainLifetime, expGainFloatSpeed, expFontStyle);
        }
        
        /// <summary>
        /// Call this before adding exp to skip the automatic notification
        /// (useful when spawning custom floating text at a different position)
        /// </summary>
        public void SkipNextExpNotification()
        {
            skipNextExpNotification = true;
        }

        /// <summary>
        /// Called when player levels up
        /// </summary>
        private void OnLevelUp(int newLevel)
        {
            string text = showNewLevelNumber ? $"{levelUpText}\nLevel {newLevel}" : levelUpText;
            SpawnFloatingText(text, levelUpColor, levelUpScale, levelUpLifetime, levelUpFloatSpeed, levelUpFontStyle, true);
        }

        /// <summary>
        /// Spawns a floating text at the player's position
        /// </summary>
        public void SpawnFloatingText(string text, Color color, float scale, float lifetime, float floatSpeed, FontStyles fontStyle, bool isLevelUp = false)
        {
            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null)
                {
                    Debug.LogWarning("ExpNotificationManager: Cannot spawn floating text - player not found.");
                    return;
                }
            }
            
            // Calculate spawn position with random offset
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(-randomOffsetRange * 0.5f, randomOffsetRange * 0.5f),
                0f
            );
            
            // Level up text spawns higher
            Vector3 offset = isLevelUp ? spawnOffset + Vector3.up * 0.5f : spawnOffset;
            Vector3 spawnPosition = playerTransform.position + offset + randomOffset;
            
            // Create the floating text
            GameObject textObj = CreateFloatingTextObject(spawnPosition);
            
            // Configure the text
            var textMesh = textObj.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
                textMesh.fontStyle = fontStyle;
                textMesh.alignment = TextAlignmentOptions.Center;
                
                if (customFont != null)
                {
                    textMesh.font = customFont;
                }
                
                // Add outline for better visibility
                textMesh.outlineWidth = 0.2f;
                textMesh.outlineColor = new Color32(0, 0, 0, 200);
            }
            
            // Configure the floating behavior
            var floatingText = textObj.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.Setup(text, color, scale, lifetime, floatSpeed);
            }
        }

        /// <summary>
        /// Spawns floating text at a specific world position (useful for enemy exp drops)
        /// </summary>
        public void SpawnFloatingTextAt(Vector3 position, string text, Color color, float scale = 0.5f)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomOffsetRange, randomOffsetRange),
                Random.Range(0f, randomOffsetRange),
                0f
            );
            
            GameObject textObj = CreateFloatingTextObject(position + randomOffset);
            
            var textMesh = textObj.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
                textMesh.fontStyle = expFontStyle;
                textMesh.alignment = TextAlignmentOptions.Center;
                
                if (customFont != null)
                {
                    textMesh.font = customFont;
                }
                
                textMesh.outlineWidth = 0.2f;
                textMesh.outlineColor = new Color32(0, 0, 0, 200);
            }
            
            var floatingText = textObj.GetComponent<FloatingText>();
            if (floatingText != null)
            {
                floatingText.Setup(text, color, scale, expGainLifetime, expGainFloatSpeed);
            }
        }

        /// <summary>
        /// Creates a floating text game object
        /// </summary>
        private GameObject CreateFloatingTextObject(Vector3 position)
        {
            GameObject textObj;
            
            if (floatingTextPrefab != null)
            {
                textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create from scratch if no prefab assigned
                textObj = new GameObject("FloatingText");
                textObj.transform.position = position;
                
                var textMesh = textObj.AddComponent<TextMeshPro>();
                textMesh.fontSize = 5f;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.sortingOrder = 100; // Render on top
                
                // Set the rect transform size
                var rectTransform = textObj.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(10f, 3f);
                }
                
                textObj.AddComponent<FloatingText>();
            }
            
            return textObj;
        }

        #region Public API for Manual Spawning

        /// <summary>
        /// Manually spawn an exp gain notification
        /// </summary>
        public void ShowExpGain(int amount)
        {
            if (amount <= 0) return;
            string text = $"{expGainPrefix}{amount}{expGainSuffix}";
            SpawnFloatingText(text, expGainColor, expGainScale, expGainLifetime, expGainFloatSpeed, expFontStyle);
        }

        /// <summary>
        /// Manually spawn a level up notification
        /// </summary>
        public void ShowLevelUp(int level)
        {
            string text = showNewLevelNumber ? $"{levelUpText}\nLevel {level}" : levelUpText;
            SpawnFloatingText(text, levelUpColor, levelUpScale, levelUpLifetime, levelUpFloatSpeed, levelUpFontStyle, true);
        }

        /// <summary>
        /// Spawn a custom floating text at the player
        /// </summary>
        public void ShowCustomText(string text, Color color, float scale = 0.5f)
        {
            SpawnFloatingText(text, color, scale, expGainLifetime, expGainFloatSpeed, FontStyles.Bold);
        }

        #endregion
    }
}

