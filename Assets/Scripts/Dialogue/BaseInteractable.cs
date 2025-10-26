using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Base class for all interactable objects in the game
    /// Provides common functionality for proximity detection, visual feedback, and input handling
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class BaseInteractable : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] protected float interactionRadius = 2f;
        [SerializeField] protected GameObject visualIndicator;
        [SerializeField] protected bool triggerOnce = false;
        
        [Header("Events")]
        [SerializeField] protected UnityEvent onInteractionStart;
        [SerializeField] protected UnityEvent onInteractionEnd;

        // Runtime state
        protected bool playerInRange = false;
        protected bool hasTriggered = false;
        protected GameObject player;
        protected InputAction interactAction;
        private bool isInputSubscribed = false;

        protected virtual void Awake()
        {
            // Find the player
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                var playerController = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
                if (playerController != null)
                {
                    player = playerController.gameObject;
                }
            }

            // Auto-find an indicator child if none assigned
            if (visualIndicator == null)
            {
                visualIndicator = FindIndicatorInChildren();
            }

            // Hide visual indicator initially
            if (visualIndicator != null)
            {
                visualIndicator.SetActive(false);
            }
        }

        protected virtual void OnEnable()
        {
            // Set up input action
            if (interactAction == null)
            {
                PlayerInput playerInput = null;
                if (player != null)
                {
                    playerInput = player.GetComponent<PlayerInput>();
                }

                playerInput ??= FindFirstObjectByType<PlayerInput>();

                if (playerInput != null)
                {
                    interactAction = playerInput.actions?.FindAction("Interact");
                }
            }

            if (interactAction != null && !isInputSubscribed)
            {
                interactAction.Enable();
                // Use started to support actions configured with Hold interaction
                interactAction.started += OnInteractStarted;
                isInputSubscribed = true;
            }
        }

        protected virtual void OnDisable()
        {
            if (interactAction != null && isInputSubscribed)
            {
                interactAction.started -= OnInteractStarted;
                isInputSubscribed = false;
            }

            // Hide indicator when disabled
            if (visualIndicator != null)
            {
                visualIndicator.SetActive(false);
            }
        }

        protected virtual void Update()
        {
            if (player == null) return;

            // Check proximity
            float distance = Vector3.Distance(transform.position, player.transform.position);
            bool inRange = distance <= interactionRadius;

            playerInRange = inRange;
            UpdateVisualIndicator();
        }

        /// <summary>
        /// Called when the interact input is performed
        /// </summary>
        protected virtual void OnInteractPerformed(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        /// <summary>
        /// Called when the interact input is started (button pressed)
        /// </summary>
        protected virtual void OnInteractStarted(InputAction.CallbackContext context)
        {
            TryInteract();
        }

        /// <summary>
        /// Attempts to perform the interaction if conditions are met
        /// </summary>
        public void TryInteract()
        {
            if (CanInteract() && CanTrigger())
            {
                onInteractionStart?.Invoke();
                PerformInteraction();

                if (triggerOnce)
                {
                    hasTriggered = true;
                }

                onInteractionEnd?.Invoke();
            }
        }

        /// <summary>
        /// Checks if the interaction can occur (player in range, etc.)
        /// </summary>
        protected virtual bool CanInteract()
        {
            return playerInRange && player != null;
        }

        /// <summary>
        /// Checks if the trigger conditions are met (trigger once, quest requirements, etc.)
        /// </summary>
        protected virtual bool CanTrigger()
        {
            if (triggerOnce && hasTriggered)
            {
                return false;
            }

            // TODO: Add quest requirement checking here
            return true;
        }

        /// <summary>
        /// Override this method to implement specific interaction behavior
        /// </summary>
        protected abstract void PerformInteraction();

        /// <summary>
        /// Updates the visual indicator based on whether player is in range
        /// </summary>
        protected virtual void UpdateVisualIndicator()
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

        /// <summary>
        /// Resets the trigger state (useful for testing or dynamic scenarios)
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            UpdateVisualIndicator();
        }

        /// <summary>
        /// Draws the interaction radius in the editor
        /// </summary>
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }

        private GameObject FindIndicatorInChildren()
        {
            foreach (Transform child in transform)
            {
                var nameLower = child.name.ToLowerInvariant();
                if (nameLower.Contains("indicator") || nameLower.Contains("prompt") || nameLower.Contains("interact"))
                {
                    return child.gameObject;
                }
            }
            return null;
        }
    }
}

