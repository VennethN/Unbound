using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Component that triggers dialogue automatically when the player enters the trigger
    /// </summary>
    public class AutoStartDialogueTrigger : global::Unbound.Dialogue.BaseInteractable
    {
        [Header("Dialogue Settings")]
        [SerializeField] private string dialogueID;

        // Runtime state
        private DialogueController dialogueController;
        private SaveManager saveManager;

        // Caching
        private bool cachedCanTrigger = false;
        private float lastCheckTime = -1f;
        private const float CHECK_INTERVAL = 0.1f;
        private int lastGlobalFlagsHash = -1;

        protected override void Awake()
        {
            base.Awake();

            dialogueController = FindFirstObjectByType<DialogueController>();
            saveManager = SaveManager.Instance;

            if (dialogueController == null)
                Debug.LogWarning("No DialogueController found in scene. Dialogue triggers will not work.");
        }

        protected override bool CanInteract()
        {
            return base.CanInteract() &&
                   dialogueController != null &&
                   !dialogueController.IsDialogueActive();
        }

        protected override bool CanTrigger()
        {
            if (!base.CanTrigger() ||
                dialogueController == null ||
                string.IsNullOrEmpty(dialogueID))
            {
                cachedCanTrigger = false;
                return false;
            }

            float currentTime = Time.time;
            if (lastCheckTime >= 0f && currentTime - lastCheckTime < CHECK_INTERVAL)
            {
                int hash = GetGlobalFlagsHash();
                if (hash == lastGlobalFlagsHash)
                    return cachedCanTrigger;

                lastGlobalFlagsHash = hash;
            }
            else
            {
                lastGlobalFlagsHash = GetGlobalFlagsHash();
            }

            lastCheckTime = currentTime;

            DialogueData data = DialogueDatabase.Instance.GetDialogue(dialogueID);
            if (data == null)
            {
                cachedCanTrigger = false;
                return false;
            }

            cachedCanTrigger = data.HasValidStartNode(dialogueController);
            return cachedCanTrigger;
        }

        private int GetGlobalFlagsHash()
        {
            if (saveManager == null)
                saveManager = SaveManager.Instance;

            if (saveManager == null)
                return 0;

            var flags = saveManager.GetAllGlobalFlags();
            if (flags == null || flags.Count == 0)
                return 0;

            int hash = flags.Count;
            int sampleCount = 0;
            const int MAX = 5;

            foreach (var flag in flags)
            {
                if (sampleCount >= MAX) break;

                hash = hash * 31 + flag.Key.GetHashCode();
                hash = hash * 31 + (flag.Value ? 1 : 0);
                sampleCount++;
            }

            return hash;
        }

        protected override void UpdateVisualIndicator()
        {
            if (visualIndicator != null)
            {
                bool shouldShow = playerInRange && CanTrigger();
                if (visualIndicator.activeSelf != shouldShow)
                {
                    visualIndicator.SetActive(shouldShow);
                }
            }
        }

        protected override void PerformInteraction()
        {
            if (dialogueController != null && !string.IsNullOrEmpty(dialogueID))
            {
                dialogueController.StartDialogue(dialogueID);
            }
        }

        /// ✨ ***NEW METHOD: Auto-start dialogue when player enters trigger***
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            playerInRange = true;

            // Only start if all dialogue conditions allow it
            if (CanTrigger() && CanInteract())
            {
                TryInteract();  // automatically start dialogue
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                playerInRange = false;
        }

        public void TriggerDialogue()
        {
            TryInteract();
        }
    }
}