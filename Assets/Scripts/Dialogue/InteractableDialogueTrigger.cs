using UnityEngine;
using UnityEngine.Events;
using Unbound.Player;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Component that triggers dialogue when the player interacts with this object
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class InteractableDialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue Settings")]
        [SerializeField] private DialogueAsset dialogueAsset;
        [SerializeField] private bool triggerOnce = false;
        [SerializeField] private bool requireInRange = true;
        [SerializeField] private float interactionRadius = 2f;

        [Header("Trigger Conditions")]
        [SerializeField] private bool requireSpecificQuestState = false;
        [SerializeField] private string requiredQuestID;
        [SerializeField] private string requiredQuestState;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject interactionIndicator;
        [SerializeField] private bool showIndicatorWhenInRange = true;

        [Header("Events")]
        public UnityEvent OnInteractionStart;
        public UnityEvent OnInteractionEnd;

        // Runtime state
        private bool hasBeenTriggered;
        private bool playerInRange;
        private DialogueController dialogueController;
        private Collider2D interactionCollider;

        private void Awake()
        {
            interactionCollider = GetComponent<Collider2D>();
            dialogueController = FindFirstObjectByType<DialogueController>();

            if (dialogueController == null)
            {
                Debug.LogWarning("No DialogueController found in scene. Dialogue triggers will not work.");
            }

            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (requireInRange)
            {
                StartCoroutine(CheckPlayerProximity());
            }
        }

        private void OnDisable()
        {
            if (interactionIndicator != null)
            {
                interactionIndicator.SetActive(false);
            }
        }

        private void Update()
        {
            // Check for interaction input when player is in range and dialogue is not active
            if (playerInRange && dialogueController != null && !dialogueController.IsDialogueActive())
            {
                // Check for interact input (using the Input System action)
                var playerInput = FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>();
                if (playerInput != null)
                {
                    var interactAction = playerInput.actions?.FindAction("Interact", false);
                    if (interactAction != null && interactAction.WasPressedThisFrame())
                    {
                        TryTriggerDialogue();
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to trigger the dialogue if conditions are met
        /// </summary>
        public void TryTriggerDialogue()
        {
            if (!CanTrigger())
                return;

            if (dialogueController != null && dialogueAsset != null)
            {
                dialogueController.StartDialogue(dialogueAsset);
                hasBeenTriggered = true;

                OnInteractionStart?.Invoke();
            }
        }

        /// <summary>
        /// Checks if the dialogue can be triggered
        /// </summary>
        private bool CanTrigger()
        {
            // Check if already triggered (if triggerOnce is enabled)
            if (triggerOnce && hasBeenTriggered)
                return false;

            // Check quest requirements
            if (requireSpecificQuestState)
            {
                // TODO: Integrate with quest system to check quest state
                // For now, assume it's met
            }

            return dialogueController != null && dialogueAsset != null;
        }

        /// <summary>
        /// Coroutine to check if player is in proximity
        /// </summary>
        private System.Collections.IEnumerator CheckPlayerProximity()
        {
            while (true)
            {
                if (requireInRange)
                {
                    var player = GameObject.FindFirstObjectByType<PlayerController2D>()?.gameObject ?? GameObject.FindGameObjectWithTag("Player");
                    if (player != null)
                    {
                        float distance = Vector2.Distance(transform.position, player.transform.position);
                        bool wasInRange = playerInRange;
                        playerInRange = distance <= interactionRadius;

                        // Update indicator visibility
                        if (interactionIndicator != null && showIndicatorWhenInRange)
                        {
                            interactionIndicator.SetActive(playerInRange && !hasBeenTriggered);
                        }

                        // Trigger events for range changes
                        if (playerInRange && !wasInRange)
                        {
                            OnPlayerEnterRange();
                        }
                        else if (!playerInRange && wasInRange)
                        {
                            OnPlayerExitRange();
                        }
                    }
                    else
                    {
                        playerInRange = false;
                        if (interactionIndicator != null)
                        {
                            interactionIndicator.SetActive(false);
                        }
                    }
                }

                yield return new UnityEngine.WaitForSeconds(0.1f);
            }
        }

        /// <summary>
        /// Called when player enters interaction range
        /// </summary>
        private void OnPlayerEnterRange()
        {
            // Could add visual/audio feedback here
        }

        /// <summary>
        /// Called when player exits interaction range
        /// </summary>
        private void OnPlayerExitRange()
        {
            // Could add visual/audio feedback here
        }

        /// <summary>
        /// Manually triggers the dialogue (for testing or programmatic use)
        /// </summary>
        public void TriggerDialogue()
        {
            TryTriggerDialogue();
        }

        /// <summary>
        /// Resets the trigger so it can be used again
        /// </summary>
        public void ResetTrigger()
        {
            hasBeenTriggered = false;
        }

        /// <summary>
        /// Sets a new dialogue asset
        /// </summary>
        public void SetDialogueAsset(DialogueAsset newDialogue)
        {
            dialogueAsset = newDialogue;
        }

        private void OnDrawGizmosSelected()
        {
            if (requireInRange)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, interactionRadius);
            }
        }
    }
}
