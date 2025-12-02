using UnityEngine;
using UnityEngine.Events;
using Unbound.Player;

namespace Unbound.Dialogue
{
    /// <summary>
    /// A collideable that requires the player to be at a certain level to trigger.
    /// If the level requirement is not met, an optional dialogue can be played.
    /// Useful for level-gated areas, doors, or zone transitions.
    /// </summary>
    public class LevelRequiredCollideable : BaseCollideable
    {
        [Header("Level Requirement")]
        [Tooltip("Whether to enable level requirement checking")]
        [SerializeField] private bool requireLevel = true;
        
        [Tooltip("Minimum level required to trigger this collideable")]
        [SerializeField] private int requiredLevel = 1;
        
        [Header("Blocked Collision")]
        [Tooltip("Dialogue ID to play when the player doesn't meet the level requirement")]
        [SerializeField] private string blockedDialogueID;
        
        [Tooltip("If true, the blocked dialogue will only play once per encounter (until player leaves and returns)")]
        [SerializeField] private bool playBlockedDialogueOnce = true;
        
        [Tooltip("Cooldown in seconds before the blocked dialogue can play again (0 = no cooldown)")]
        [SerializeField] private float blockedDialogueCooldown = 2f;
        
        [Header("Collision Events")]
        [SerializeField] private UnityEvent onSuccessfulCollision;
        [SerializeField] private UnityEvent onBlockedCollision;
        
        // Runtime state
        private DialogueController dialogueController;
        private LevelingSystem levelingSystem;
        private bool hasPlayedBlockedDialogue = false;
        private float lastBlockedDialogueTime = -1000f;

        /// <summary>
        /// The required level for this collideable
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
                    Debug.LogWarning("LevelRequiredCollideable requires DialogueController when blocked dialogue is set, but none found in scene.");
                }
            }
        }

        private void OnValidate()
        {
            requiredLevel = Mathf.Max(1, requiredLevel);
            blockedDialogueCooldown = Mathf.Max(0f, blockedDialogueCooldown);
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

            // If dialogue controller exists, make sure dialogue isn't active
            if (dialogueController != null && dialogueController.IsDialogueActive())
            {
                return false;
            }

            // Always allow trigger - we'll handle blocking in PerformCollision
            return true;
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
            Debug.LogWarning("LevelRequiredCollideable: No LevelingSystem found. Allowing collision.");
            return true;
        }

        /// <summary>
        /// Performs the collision, checking level requirement first
        /// </summary>
        protected override void PerformCollision()
        {
            // Check level requirement
            if (requireLevel && !MeetsLevelRequirement())
            {
                // Level requirement not met - handle blocked collision
                HandleBlockedCollision();
                return;
            }

            // Level requirement met - perform successful collision
            hasPlayedBlockedDialogue = false; // Reset for next time they fail
            onSuccessfulCollision?.Invoke();
        }

        /// <summary>
        /// Handles what happens when the player doesn't meet the level requirement
        /// </summary>
        private void HandleBlockedCollision()
        {
            onBlockedCollision?.Invoke();

            // Check if we should play the blocked dialogue
            bool shouldPlayDialogue = !string.IsNullOrEmpty(blockedDialogueID);
            
            if (shouldPlayDialogue && playBlockedDialogueOnce && hasPlayedBlockedDialogue)
            {
                shouldPlayDialogue = false;
            }
            
            if (shouldPlayDialogue && blockedDialogueCooldown > 0f)
            {
                float timeSinceLastDialogue = Time.time - lastBlockedDialogueTime;
                if (timeSinceLastDialogue < blockedDialogueCooldown)
                {
                    shouldPlayDialogue = false;
                }
            }

            // Play blocked dialogue if all conditions are met
            if (shouldPlayDialogue)
            {
                EnsureDialogueController();
                
                if (dialogueController != null && !dialogueController.IsDialogueActive())
                {
                    dialogueController.StartDialogue(blockedDialogueID);
                    hasPlayedBlockedDialogue = true;
                    lastBlockedDialogueTime = Time.time;
                }
                else if (dialogueController == null)
                {
                    Debug.LogWarning($"Cannot play blocked dialogue '{blockedDialogueID}': DialogueController not found.");
                }
            }
            else if (string.IsNullOrEmpty(blockedDialogueID))
            {
                Debug.Log($"Collision blocked: Player level ({GetCurrentPlayerLevel()}) is below required level ({requiredLevel})");
            }
        }

        /// <summary>
        /// Reset the blocked dialogue flag when player exits the trigger
        /// </summary>
        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                hasPlayedBlockedDialogue = false;
            }
        }

        /// <summary>
        /// Reset the blocked dialogue flag when player exits collision
        /// </summary>
        protected virtual void OnCollisionExit2D(Collision2D collision)
        {
            if (IsPlayer(collision.collider))
            {
                hasPlayedBlockedDialogue = false;
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
        /// Sets the required level for this collideable
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
        /// Gets whether the collision is currently blocked due to level
        /// </summary>
        public bool IsBlockedByLevel()
        {
            return requireLevel && !MeetsLevelRequirement();
        }

        /// <summary>
        /// Resets the blocked dialogue flag (allows it to play again)
        /// </summary>
        public void ResetBlockedDialogueFlag()
        {
            hasPlayedBlockedDialogue = false;
        }

        #endregion

        #region Editor

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            // Draw level requirement indicator
            if (requireLevel)
            {
                Collider2D collider = GetComponent<Collider2D>();
                if (collider != null)
                {
                    Vector3 labelPos = collider.bounds.center + Vector3.up * (collider.bounds.extents.y + 0.5f);
                    
#if UNITY_EDITOR
                    UnityEditor.Handles.Label(
                        labelPos,
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
        }

        #endregion
    }
}

