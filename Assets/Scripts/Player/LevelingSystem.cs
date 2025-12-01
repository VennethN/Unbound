using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Player
{
    /// <summary>
    /// Defines how experience thresholds are calculated for each level
    /// </summary>
    public enum ExpThresholdMode
    {
        /// <summary>
        /// Use manually defined thresholds for each level
        /// </summary>
        Manual,
        
        /// <summary>
        /// Use an AnimationCurve to calculate thresholds based on level
        /// </summary>
        Curve,
        
        /// <summary>
        /// Use a formula: baseExp * (level ^ exponent)
        /// </summary>
        Formula
    }

    /// <summary>
    /// Singleton manager that handles player leveling, experience, and level-up logic
    /// </summary>
    public class LevelingSystem : MonoBehaviour
    {
        private static LevelingSystem _instance;
        public static LevelingSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<LevelingSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("LevelingSystem");
                        _instance = go.AddComponent<LevelingSystem>();
                    }
                }
                return _instance;
            }
        }

        [Header("Level Settings")]
        [SerializeField] private int maxLevel = 100;
        [SerializeField] private int startingLevel = 1;
        
        [Header("Experience Threshold Mode")]
        [SerializeField] private ExpThresholdMode thresholdMode = ExpThresholdMode.Curve;
        
        [Header("Manual Thresholds")]
        [Tooltip("Experience required to reach each level (index 0 = level 1, etc.)")]
        [SerializeField] private List<int> manualThresholds = new List<int> { 100, 250, 500, 800, 1200, 1700, 2300, 3000, 3800, 4700 };
        
        [Header("Curve-Based Thresholds")]
        [Tooltip("X-axis: level (0-1 normalized), Y-axis: exp required (will be multiplied by expMultiplier)")]
        [SerializeField] private AnimationCurve expCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Maximum experience value that the curve's Y=1 represents")]
        [SerializeField] private float curveExpMultiplier = 10000f;
        
        [Header("Formula-Based Thresholds")]
        [Tooltip("Base experience for level 1")]
        [SerializeField] private float formulaBaseExp = 100f;
        [Tooltip("Exponent for the formula: baseExp * (level ^ exponent)")]
        [SerializeField] private float formulaExponent = 1.5f;
        
        [Header("Events")]
        public UnityEvent<int> OnLevelUp;
        public UnityEvent<int, int> OnExpGained; // current exp, exp gained
        public UnityEvent<int, int, int> OnExpChanged; // current exp, exp to next level, current level
        
        // Runtime state
        private int currentLevel = 1;
        private int currentExp = 0;
        
        // Cached thresholds
        private Dictionary<int, int> cachedThresholds = new Dictionary<int, int>();
        
        // Save system reference
        private SaveManager saveManager;
        
        /// <summary>
        /// Current player level
        /// </summary>
        public int CurrentLevel => currentLevel;
        
        /// <summary>
        /// Current experience points
        /// </summary>
        public int CurrentExp => currentExp;
        
        /// <summary>
        /// Maximum level cap
        /// </summary>
        public int MaxLevel => maxLevel;
        
        /// <summary>
        /// Experience required to reach next level
        /// </summary>
        public int ExpToNextLevel => GetExpForLevel(currentLevel + 1);
        
        /// <summary>
        /// Progress towards next level (0-1)
        /// </summary>
        public float LevelProgress
        {
            get
            {
                if (currentLevel >= maxLevel) return 1f;
                int expForCurrent = GetExpForLevel(currentLevel);
                int expForNext = GetExpForLevel(currentLevel + 1);
                int expRange = expForNext - expForCurrent;
                if (expRange <= 0) return 1f;
                return Mathf.Clamp01((float)(currentExp - expForCurrent) / expRange);
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Don't destroy on load if this is a persistent manager
            // DontDestroyOnLoad(gameObject);
            
            currentLevel = startingLevel;
            currentExp = 0;
            
            // Pre-cache thresholds for performance
            CacheAllThresholds();
        }

        private void Start()
        {
            // Load saved data
            LoadLevelData();
        }

        private void OnValidate()
        {
            // Clamp values
            maxLevel = Mathf.Max(1, maxLevel);
            startingLevel = Mathf.Clamp(startingLevel, 1, maxLevel);
            formulaBaseExp = Mathf.Max(1f, formulaBaseExp);
            formulaExponent = Mathf.Max(0.1f, formulaExponent);
            curveExpMultiplier = Mathf.Max(1f, curveExpMultiplier);
            
            // Ensure manual thresholds list has at least one entry
            if (manualThresholds == null || manualThresholds.Count == 0)
            {
                manualThresholds = new List<int> { 100 };
            }
            
            // Clear cache when values change in editor
            if (Application.isPlaying)
            {
                cachedThresholds.Clear();
                CacheAllThresholds();
            }
        }

        #region Experience & Level Management

        /// <summary>
        /// Adds experience to the player
        /// </summary>
        public void AddExperience(int amount)
        {
            if (amount <= 0 || currentLevel >= maxLevel) return;
            
            int previousExp = currentExp;
            currentExp += amount;
            
            OnExpGained?.Invoke(currentExp, amount);
            
            // Check for level ups
            CheckLevelUp();
            
            OnExpChanged?.Invoke(currentExp, ExpToNextLevel, currentLevel);
            
            // Save progress
            SaveLevelData();
            
            Debug.Log($"[LevelingSystem] Gained {amount} EXP. Total: {currentExp}/{ExpToNextLevel} (Level {currentLevel})");
        }

        /// <summary>
        /// Sets experience directly (useful for loading)
        /// </summary>
        public void SetExperience(int exp)
        {
            currentExp = Mathf.Max(0, exp);
            CheckLevelUp();
            OnExpChanged?.Invoke(currentExp, ExpToNextLevel, currentLevel);
        }

        /// <summary>
        /// Sets level directly (useful for loading or debugging)
        /// </summary>
        public void SetLevel(int level)
        {
            int previousLevel = currentLevel;
            currentLevel = Mathf.Clamp(level, 1, maxLevel);
            
            // Adjust exp to match level
            if (currentLevel > 1)
            {
                currentExp = Mathf.Max(currentExp, GetExpForLevel(currentLevel));
            }
            
            if (currentLevel != previousLevel)
            {
                OnExpChanged?.Invoke(currentExp, ExpToNextLevel, currentLevel);
            }
        }

        /// <summary>
        /// Checks if player should level up based on current experience
        /// </summary>
        private void CheckLevelUp()
        {
            while (currentLevel < maxLevel && currentExp >= GetExpForLevel(currentLevel + 1))
            {
                currentLevel++;
                OnLevelUp?.Invoke(currentLevel);
                Debug.Log($"[LevelingSystem] LEVEL UP! Now level {currentLevel}");
            }
        }

        /// <summary>
        /// Checks if the player meets a minimum level requirement
        /// </summary>
        public bool MeetsLevelRequirement(int requiredLevel)
        {
            return currentLevel >= requiredLevel;
        }

        #endregion

        #region Threshold Calculations

        /// <summary>
        /// Gets the total experience required to reach a specific level
        /// </summary>
        public int GetExpForLevel(int level)
        {
            if (level <= 1) return 0;
            if (level > maxLevel) level = maxLevel;
            
            // Check cache first
            if (cachedThresholds.TryGetValue(level, out int cached))
            {
                return cached;
            }
            
            // Calculate based on mode
            int exp = CalculateExpForLevel(level);
            cachedThresholds[level] = exp;
            return exp;
        }

        /// <summary>
        /// Calculates experience threshold based on current mode
        /// </summary>
        private int CalculateExpForLevel(int level)
        {
            switch (thresholdMode)
            {
                case ExpThresholdMode.Manual:
                    return CalculateManualThreshold(level);
                    
                case ExpThresholdMode.Curve:
                    return CalculateCurveThreshold(level);
                    
                case ExpThresholdMode.Formula:
                    return CalculateFormulaThreshold(level);
                    
                default:
                    return CalculateCurveThreshold(level);
            }
        }

        /// <summary>
        /// Gets threshold from manual list
        /// </summary>
        private int CalculateManualThreshold(int level)
        {
            if (level <= 1) return 0;
            
            int index = level - 2; // Level 2 is at index 0
            if (index >= 0 && index < manualThresholds.Count)
            {
                return manualThresholds[index];
            }
            
            // Extrapolate for levels beyond the list
            if (manualThresholds.Count == 0) return level * 100;
            
            int lastThreshold = manualThresholds[manualThresholds.Count - 1];
            int secondLastThreshold = manualThresholds.Count > 1 
                ? manualThresholds[manualThresholds.Count - 2] 
                : lastThreshold / 2;
            
            int difference = lastThreshold - secondLastThreshold;
            int extraLevels = index - (manualThresholds.Count - 1);
            
            return lastThreshold + (difference * extraLevels);
        }

        /// <summary>
        /// Gets threshold from animation curve
        /// </summary>
        private int CalculateCurveThreshold(int level)
        {
            if (level <= 1) return 0;
            
            // Normalize level to 0-1 range
            float normalizedLevel = (float)(level - 1) / (maxLevel - 1);
            float curveValue = expCurve.Evaluate(normalizedLevel);
            
            return Mathf.RoundToInt(curveValue * curveExpMultiplier);
        }

        /// <summary>
        /// Gets threshold from formula
        /// </summary>
        private int CalculateFormulaThreshold(int level)
        {
            if (level <= 1) return 0;
            
            return Mathf.RoundToInt(formulaBaseExp * Mathf.Pow(level, formulaExponent));
        }

        /// <summary>
        /// Pre-caches all threshold values for performance
        /// </summary>
        private void CacheAllThresholds()
        {
            cachedThresholds.Clear();
            for (int i = 1; i <= maxLevel + 1; i++)
            {
                cachedThresholds[i] = CalculateExpForLevel(i);
            }
        }

        /// <summary>
        /// Clears and recalculates the threshold cache
        /// </summary>
        public void RefreshThresholdCache()
        {
            CacheAllThresholds();
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Saves current level and experience to the save system
        /// </summary>
        public void SaveLevelData()
        {
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }
            
            if (saveManager != null)
            {
                var saveData = saveManager.GetCurrentSaveData();
                if (saveData != null && saveData.playerData != null)
                {
                    saveData.playerData.level = currentLevel;
                    saveData.playerData.experience = currentExp;
                    saveManager.Save(saveData);
                }
            }
        }

        /// <summary>
        /// Loads level and experience from the save system
        /// </summary>
        public void LoadLevelData()
        {
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }
            
            if (saveManager != null)
            {
                var saveData = saveManager.GetCurrentSaveData();
                if (saveData != null && saveData.playerData != null)
                {
                    currentLevel = Mathf.Clamp(saveData.playerData.level, 1, maxLevel);
                    currentExp = (int)saveData.playerData.experience;
                    
                    // Ensure exp matches level (fix any inconsistencies)
                    if (currentLevel > 1 && currentExp < GetExpForLevel(currentLevel))
                    {
                        currentExp = GetExpForLevel(currentLevel);
                    }
                    
                    OnExpChanged?.Invoke(currentExp, ExpToNextLevel, currentLevel);
                    Debug.Log($"[LevelingSystem] Loaded: Level {currentLevel}, EXP {currentExp}");
                }
            }
        }

        #endregion

        #region Debug/Testing

        /// <summary>
        /// Debug: Adds a large amount of experience
        /// </summary>
        [ContextMenu("Debug: Add 1000 EXP")]
        public void DebugAdd1000Exp()
        {
            AddExperience(1000);
        }

        /// <summary>
        /// Debug: Level up instantly
        /// </summary>
        [ContextMenu("Debug: Level Up")]
        public void DebugLevelUp()
        {
            if (currentLevel < maxLevel)
            {
                int expNeeded = GetExpForLevel(currentLevel + 1) - currentExp;
                AddExperience(expNeeded);
            }
        }

        /// <summary>
        /// Debug: Reset to level 1
        /// </summary>
        [ContextMenu("Debug: Reset Level")]
        public void DebugResetLevel()
        {
            currentLevel = startingLevel;
            currentExp = 0;
            OnExpChanged?.Invoke(currentExp, ExpToNextLevel, currentLevel);
            SaveLevelData();
        }

        /// <summary>
        /// Debug: Print all thresholds
        /// </summary>
        [ContextMenu("Debug: Print All Thresholds")]
        public void DebugPrintThresholds()
        {
            Debug.Log($"[LevelingSystem] Threshold Mode: {thresholdMode}");
            for (int i = 1; i <= Mathf.Min(maxLevel, 20); i++)
            {
                Debug.Log($"  Level {i}: {GetExpForLevel(i)} EXP required");
            }
            if (maxLevel > 20)
            {
                Debug.Log($"  ... (showing first 20 of {maxLevel} levels)");
            }
        }

        #endregion
    }
}

