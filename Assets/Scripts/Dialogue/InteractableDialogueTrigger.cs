using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Component that triggers dialogue when the player interacts with this object
    /// </summary>
    public class InteractableDialogueTrigger : global::Unbound.Dialogue.BaseInteractable
    {
        [Header("Dialogue Settings")]
        [SerializeField] private DialogueAsset dialogueAsset;

        // Runtime state
        private DialogueController dialogueController;
        private SaveManager saveManager;
        
        // Caching for performance optimization
        private bool cachedCanTrigger = false;
        private float lastCheckTime = -1f;
        private const float CHECK_INTERVAL = 0.1f; // Check every 0.1 seconds instead of every frame
        private int lastGlobalFlagsHash = -1;

        protected override void Awake()
        {
            base.Awake();

            dialogueController = FindFirstObjectByType<DialogueController>();
            saveManager = SaveManager.Instance;

            if (dialogueController == null)
            {
                Debug.LogWarning("No DialogueController found in scene. Dialogue triggers will not work.");
            }
        }

        /// <summary>
        /// Override to check if dialogue is not currently active
        /// </summary>
        protected override bool CanInteract()
        {
            return base.CanInteract() && dialogueController != null && !dialogueController.IsDialogueActive();
        }

        /// <summary>
        /// Override to add dialogue-specific trigger conditions
        /// </summary>
        protected override bool CanTrigger()
        {
            if (!base.CanTrigger() || dialogueController == null || dialogueAsset == null)
            {
                cachedCanTrigger = false;
                return false;
            }

            // Use cached result if recently checked (performance optimization)
            float currentTime = Time.time;
            if (lastCheckTime >= 0f && currentTime - lastCheckTime < CHECK_INTERVAL)
            {
                // Only check hash if we're within the check interval
                int currentHash = GetGlobalFlagsHash();
                if (currentHash == lastGlobalFlagsHash)
                {
                    return cachedCanTrigger;
                }
                // Flags changed, update hash but continue to re-evaluate
                lastGlobalFlagsHash = currentHash;
            }
            else
            {
                // Update hash when checking after interval
                lastGlobalFlagsHash = GetGlobalFlagsHash();
            }
            
            lastCheckTime = currentTime;
            
            // Check if dialogue has a valid start node based on conditions
            cachedCanTrigger = dialogueAsset.HasValidStartNode(dialogueController);
            return cachedCanTrigger;
        }
        
        /// <summary>
        /// Gets a hash of current global flags for change detection (optimized)
        /// </summary>
        private int GetGlobalFlagsHash()
        {
            // Cache SaveManager reference
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }
            
            if (saveManager == null)
                return 0;
            
            var flags = saveManager.GetAllGlobalFlags();
            if (flags == null || flags.Count == 0)
                return 0;
            
            // Fast hash based on count and a sample of flags (faster than hashing all flags)
            int hash = flags.Count;
            int sampleCount = 0;
            const int MAX_SAMPLE = 5; // Only hash first 5 flags for performance
            
            foreach (var flag in flags)
            {
                if (sampleCount >= MAX_SAMPLE)
                    break;
                    
                hash = hash * 31 + flag.Key.GetHashCode();
                hash = hash * 31 + (flag.Value ? 1 : 0);
                sampleCount++;
            }
            
            return hash;
        }

        /// <summary>
        /// Override to check start node conditions for indicator visibility
        /// </summary>
        protected override void UpdateVisualIndicator()
        {
            if (visualIndicator != null)
            {
                // Check if player is in range, can trigger, and dialogue has valid start node
                bool shouldShow = playerInRange && CanTrigger();
                if (visualIndicator.activeSelf != shouldShow)
                {
                    visualIndicator.SetActive(shouldShow);
                }
            }
        }

        /// <summary>
        /// Performs the dialogue interaction
        /// </summary>
        protected override void PerformInteraction()
        {
            if (dialogueController != null && dialogueAsset != null)
            {
                dialogueController.StartDialogue(dialogueAsset);
            }
        }

        /// <summary>
        /// Manually triggers the dialogue (for testing or programmatic use)
        /// </summary>
        public void TriggerDialogue()
        {
            TryInteract();
        }

        /// <summary>
        /// Sets a new dialogue asset
        /// </summary>
        public void SetDialogueAsset(DialogueAsset newDialogue)
        {
            dialogueAsset = newDialogue;
        }

        /// <summary>
        /// Gets the current dialogue asset
        /// </summary>
        public DialogueAsset GetDialogueAsset()
        {
            return dialogueAsset;
        }

        /// <summary>
        /// Gets the dialogue controller
        /// </summary>
        public DialogueController GetDialogueController()
        {
            return dialogueController;
        }
        
        /// <summary>
        /// Invalidates the cached trigger state (call this when flags change externally)
        /// </summary>
        public void InvalidateCache()
        {
            lastCheckTime = -1f;
            lastGlobalFlagsHash = -1;
        }
    }
}
