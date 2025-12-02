using UnityEngine;
using UnityEngine.Events;
using Unbound.Player;

namespace Unbound.Dialogue
{
    /// <summary>
    /// An interactable that requires the player to be at a certain level to interact with.
    /// If the level requirement is not met, an optional dialogue can be played.
    /// </summary>
    public class LevelRequiredInteractable : BaseInteractable
    {
        [Header("Level Requirement")]
        [Tooltip("Whether to enable level requirement checking")]
        [SerializeField] private bool requireLevel = true;
        
        [Tooltip("Minimum level required to interact with this object")]
        [SerializeField] private int requiredLevel = 1;
        
        [Header("Blocked Interaction")]
        [Tooltip("Dialogue ID to play when the player doesn't meet the level requirement")]
        [SerializeField] private string blockedDialogueID;
        
        [Tooltip("Whether to show the interaction indicator when requirements aren't met")]
        [SerializeField] private bool showIndicatorWhenBlocked = true;
        
        [Header("Interaction Events")]
        [SerializeField] private UnityEvent onSuccessfulInteraction;
        [SerializeField] private UnityEvent onBlockedInteraction;
        
        // Runtime state
        private DialogueController dialogueController;
        private LevelingSystem levelingSystem;

        /// <summary>
        /// The required level for this interactable
        /// </summary>
        public int RequiredLevel => requiredLevel;
        
        /// <summary>
        /// Whether level requirement is enabled
        /// </summary>
        public bool RequiresLevel => requireLevel;

        protected override void Awake()
        {
            base.Awake();

            // Find DialogueController if blocked dialogue is set
            if (!string.IsNullOrEmpty(blockedDialogueID))
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
                if (dialogueController == null)
                {
                    Debug.LogWarning("LevelRequiredInteractable requires DialogueController when blocked dialogue is set, but none found in scene.");
                }
            }
        }

        private void OnValidate()
        {
            requiredLevel = Mathf.Max(1, requiredLevel);
        }

        /// <summary>
        /// Override to check if dialogue is not currently active
        /// </summary>
        protected override bool CanInteract()
        {
            bool baseCanInteract = base.CanInteract();

            // If blocked dialogue is set, make sure dialogue controller exists and dialogue isn't active
            if (!string.IsNullOrEmpty(blockedDialogueID) && dialogueController != null)
            {
                return baseCanInteract && !dialogueController.IsDialogueActive();
            }

            return baseCanInteract;
        }

        /// <summary>
        /// Override to handle level requirement in trigger check
        /// </summary>
        protected override bool CanTrigger()
        {
            // Always check base trigger first
            if (!base.CanTrigger())
            {
                return false;
            }

            // If level requirement is disabled, allow interaction
            if (!requireLevel)
            {
                return true;
            }

            // If we show indicator when blocked, we want CanTrigger to return true
            // so the indicator shows, but we'll handle the actual blocking in PerformInteraction
            if (showIndicatorWhenBlocked)
            {
                return true;
            }

            // Otherwise, only allow trigger if level requirement is met
            return MeetsLevelRequirement();
        }

        /// <summary>
        /// Override to check level requirement visibility
        /// </summary>
        protected override void UpdateVisualIndicator()
        {
            if (visualIndicator != null)
            {
                bool shouldShow = playerInRange && base.CanTrigger();
                
                // Only show if we meet level requirement or if showIndicatorWhenBlocked is true
                if (!showIndicatorWhenBlocked && !MeetsLevelRequirement())
                {
                    shouldShow = false;
                }
                
                if (visualIndicator.activeSelf != shouldShow)
                {
                    visualIndicator.SetActive(shouldShow);
                }
            }
        }

        /// <summary>
        /// Checks if the player meets the level requirement
        /// </summary>
        public bool MeetsLevelRequirement()
        {
            if (!requireLevel) return true;
            
            if (levelingSystem == null)
            {
                levelingSystem = LevelingSystem.Instance;
            }

            if (levelingSystem != null)
            {
                return levelingSystem.MeetsLevelRequirement(requiredLevel);
            }

            // If no leveling system, assume requirement is met
            Debug.LogWarning("LevelRequiredInteractable: No LevelingSystem found. Allowing interaction.");
            return true;
        }

        /// <summary>
        /// Performs the interaction, checking level requirement first
        /// </summary>
        protected override void PerformInteraction()
        {
            // Check level requirement
            if (requireLevel && !MeetsLevelRequirement())
            {
                // Level requirement not met - show blocked dialogue
                HandleBlockedInteraction();
                return;
            }

            // Level requirement met - perform successful interaction
            onSuccessfulInteraction?.Invoke();
        }

        /// <summary>
        /// Handles what happens when the player doesn't meet the level requirement
        /// </summary>
        private void HandleBlockedInteraction()
        {
            onBlockedInteraction?.Invoke();

            // Play blocked dialogue if set
            if (!string.IsNullOrEmpty(blockedDialogueID))
            {
                EnsureDialogueController();
                
                if (dialogueController != null)
                {
                    dialogueController.StartDialogue(blockedDialogueID);
                }
                else
                {
                    Debug.LogWarning($"Cannot play blocked dialogue '{blockedDialogueID}': DialogueController not found.");
                }
            }
            else
            {
                Debug.Log($"Interaction blocked: Player level ({GetCurrentPlayerLevel()}) is below required level ({requiredLevel})");
            }
        }

        /// <summary>
        /// Ensures DialogueController is found if needed
        /// </summary>
        private void EnsureDialogueController()
        {
            if (dialogueController == null && !string.IsNullOrEmpty(blockedDialogueID))
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
            }
        }

        /// <summary>
        /// Gets the current player level
        /// </summary>
        private int GetCurrentPlayerLevel()
        {
            if (levelingSystem == null)
            {
                levelingSystem = LevelingSystem.Instance;
            }

            return levelingSystem != null ? levelingSystem.CurrentLevel : 0;
        }

        #region Public API

        /// <summary>
        /// Sets the required level for this interactable
        /// </summary>
        public void SetRequiredLevel(int level)
        {
            requiredLevel = Mathf.Max(1, level);
        }

        /// <summary>
        /// Enables or disables the level requirement
        /// </summary>
        public void SetRequireLevel(bool require)
        {
            requireLevel = require;
        }

        /// <summary>
        /// Sets the dialogue ID to play when blocked
        /// </summary>
        public void SetBlockedDialogueID(string dialogueID)
        {
            blockedDialogueID = dialogueID;
        }

        /// <summary>
        /// Gets the blocked dialogue ID
        /// </summary>
        public string GetBlockedDialogueID()
        {
            return blockedDialogueID;
        }

        /// <summary>
        /// Gets whether the interaction is currently blocked due to level
        /// </summary>
        public bool IsBlockedByLevel()
        {
            return requireLevel && !MeetsLevelRequirement();
        }

        #endregion

        #region Editor

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Draw level requirement indicator
            if (requireLevel)
            {
                // Draw a number above the object showing required level
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (interactionRadius + 0.5f),
                    $"Lvl {requiredLevel}+",
                    new GUIStyle
                    {
                        normal = { textColor = MeetsLevelRequirement() ? Color.green : Color.red },
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold
                    }
                );
#endif
            }
        }

        #endregion
    }
}

