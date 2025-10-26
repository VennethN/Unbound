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

        protected override void Awake()
        {
            base.Awake();

            dialogueController = FindFirstObjectByType<DialogueController>();

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
            return base.CanTrigger() && dialogueController != null && dialogueAsset != null;
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
    }
}
