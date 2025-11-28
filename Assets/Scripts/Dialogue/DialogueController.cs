using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using Unbound.Inventory;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Runtime controller that manages dialogue flow and presentation
    /// </summary>
    public class DialogueController : MonoBehaviour, IDialogueConditionEvaluator, IDialogueEffectExecutor
    {
        [Header("Configuration")]
        [SerializeField] private string defaultDialogueID;

        [Header("Input")]
        [SerializeField] private InputActionReference continueAction;

        [Header("UI References")]
        [SerializeField] private DialogueView dialogueView;

        [Header("Settings")]
        [SerializeField] private bool autoAdvanceOnComplete = false;
        [SerializeField] private bool freezePlayerMovement = true;

        // Runtime state
        private DialogueData currentDialogue;
        private DialogueNode currentNode;
        private DialogueState dialogueState;
        private Coroutine currentTextCoroutine;
        private Unbound.Player.PlayerController2D playerController;
        private Unbound.Player.PlayerCombat playerCombat;

        // Event system
        public UnityEvent<DialogueNode> OnNodeStart;
        public UnityEvent<DialogueNode> OnNodeEnd;
        public UnityEvent OnDialogueStart;
        public UnityEvent OnDialogueEnd;
        public UnityEvent<string> OnChoiceSelected;
        public UnityEvent OnChoicesPresented;

        // Flag storage for dialogue state
        private readonly System.Collections.Generic.Dictionary<string, bool> dialogueFlags =
            new System.Collections.Generic.Dictionary<string, bool>();

        private void Awake()
        {
            if (dialogueView == null)
            {
                Debug.LogError("DialogueController requires a DialogueView reference");
                return;
            }

            dialogueState = new DialogueState();
        }

        private void OnEnable()
        {
            dialogueView.OnChoiceSelected.AddListener(HandleChoiceSelected);
            dialogueView.OnContinuePressed.AddListener(HandleContinuePressed);

            // Enable input action when dialogue controller is active
            if (continueAction != null)
            {
                continueAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            dialogueView.OnChoiceSelected.RemoveListener(HandleChoiceSelected);
            dialogueView.OnContinuePressed.RemoveListener(HandleContinuePressed);

            // Disable input action when dialogue controller is inactive
            if (continueAction != null)
            {
                continueAction.action.Disable();
            }
        }

        private void Update()
        {
            // Handle input for continuing dialogue using new Input System
            if (IsDialogueActive() && continueAction != null && continueAction.action.WasPressedThisFrame())
            {
                HandleContinuePressed();
            }
        }

        /// <summary>
        /// Starts a dialogue conversation by dialogue ID
        /// </summary>
        public void StartDialogue(string dialogueID)
        {
            if (string.IsNullOrEmpty(dialogueID))
            {
                Debug.LogError("Cannot start dialogue: dialogue ID is null or empty");
                return;
            }

            DialogueData dialogueData = DialogueDatabase.Instance.GetDialogue(dialogueID);
            if (dialogueData == null)
            {
                Debug.LogError($"Cannot start dialogue: Dialogue with ID '{dialogueID}' not found in database");
                return;
            }

            string validationErrors = dialogueData.GetValidationErrors();
            if (!string.IsNullOrEmpty(validationErrors))
            {
                Debug.LogError($"Cannot start dialogue: {validationErrors}");
                return;
            }

            currentDialogue = dialogueData;
            dialogueState.Reset();

            // Restore any existing progress for this dialogue
            RestoreDialogueProgress();

            // Freeze player movement if enabled
            if (freezePlayerMovement)
            {
                FreezePlayerMovement();
            }

            OnDialogueStart?.Invoke();

            // Get the appropriate start node based on conditions
            string startNodeID = dialogueData.GetStartNodeID(this);
            if (string.IsNullOrEmpty(startNodeID))
            {
                Debug.LogError($"Cannot start dialogue: No valid start node found for dialogue '{dialogueData.dialogueID}'");
                EndDialogue();
                return;
            }

            AdvanceToNode(startNodeID);
        }

        /// <summary>
        /// Starts a dialogue conversation (legacy method for backward compatibility)
        /// </summary>
        [System.Obsolete("Use StartDialogue(string dialogueID) instead. This method will be removed in a future version.")]
        public void StartDialogue(DialogueAsset dialogueAsset)
        {
            if (dialogueAsset != null)
            {
                StartDialogue(dialogueAsset.dialogueID);
            }
        }

        /// <summary>
        /// Starts the default dialogue
        /// </summary>
        public void StartDefaultDialogue()
        {
            if (!string.IsNullOrEmpty(defaultDialogueID))
            {
                StartDialogue(defaultDialogueID);
            }
            else
            {
                Debug.LogWarning("No default dialogue ID assigned");
            }
        }

        /// <summary>
        /// Ends the current dialogue
        /// </summary>
        public void EndDialogue()
        {
            if (currentDialogue == null)
                return;

            // Save dialogue progress
            SaveDialogueProgress();

            currentDialogue = null;
            currentNode = null;

            // Unfreeze player movement if it was frozen
            if (freezePlayerMovement)
            {
                UnfreezePlayerMovement();
            }

            dialogueView.Hide();
            OnDialogueEnd?.Invoke();
        }

        /// <summary>
        /// Advances to a specific node by ID
        /// </summary>
        private void AdvanceToNode(string nodeID)
        {
            if (currentDialogue == null)
                return;

            var node = currentDialogue.GetNode(nodeID);
            if (node == null)
            {
                Debug.LogError($"Dialogue node '{nodeID}' not found");
                EndDialogue();
                return;
            }

            currentNode = node;
            dialogueState.currentNodeID = nodeID;
            dialogueState.visitedNodes.Add(nodeID);

            // Check node conditions
            bool nodeConditionsMet = true;
            foreach (var condition in node.conditions)
            {
                if (!condition.Evaluate(this))
                {
                    nodeConditionsMet = false;
                    break;
                }
            }

            if (!nodeConditionsMet)
            {
                // Try to find a fallback node or end dialogue
                Debug.LogWarning($"Node '{nodeID}' conditions not met, ending dialogue");
                EndDialogue();
                return;
            }

            // Execute node effects
            node.ExecuteEffects(this);

            // Show the node
            ShowNode(node);
        }

        /// <summary>
        /// Displays a dialogue node
        /// </summary>
        private void ShowNode(DialogueNode node)
        {
            OnNodeStart?.Invoke(node);

            // Show dialogue UI
            dialogueView.ShowNode(node, this);

            // Start typing animation
            if (currentTextCoroutine != null)
            {
                StopCoroutine(currentTextCoroutine);
            }

            currentTextCoroutine = StartCoroutine(TypeTextCoroutine(node));
        }

        /// <summary>
        /// Coroutine for typing text effect
        /// </summary>
        private IEnumerator TypeTextCoroutine(DialogueNode node)
        {
            yield return dialogueView.TypeText(node, this);

            OnNodeEnd?.Invoke(node);

            // Auto-advance if no choices and auto-advance is enabled
            if (node.choices.Count == 0 && autoAdvanceOnComplete && node.autoAdvanceDelay > 0)
            {
                yield return new WaitForSeconds(node.autoAdvanceDelay);
                HandleContinuePressed();
            }
        }

        /// <summary>
        /// Handles continue button press (speed up text or advance to next node)
        /// </summary>
        private void HandleContinuePressed()
        {
            if (currentNode == null || currentDialogue == null)
                return;

            // First, check if text is still typing - if so, speed up the animation
            if (!dialogueView.IsTextComplete())
            {
                dialogueView.SpeedUpTextAnimation();
                return;
            }

            // Text is complete, proceed with normal flow
            // If there are choices, show them
            var availableChoices = currentNode.GetAvailableChoices(this);
            if (availableChoices.Count > 0)
            {
                ShowChoices(availableChoices);
                return;
            }

            // Otherwise, advance to next node
            string nextNodeID = currentNode.GetNextNodeID();
            if (!string.IsNullOrEmpty(nextNodeID))
            {
                AdvanceToNode(nextNodeID);
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// Shows available dialogue choices
        /// </summary>
        private void ShowChoices(System.Collections.Generic.List<DialogueChoice> choices)
        {
            dialogueView.ShowChoices(choices, this);
            OnChoicesPresented?.Invoke();
        }

        /// <summary>
        /// Handles choice selection
        /// </summary>
        private void HandleChoiceSelected(DialogueChoice choice)
        {
            OnChoiceSelected?.Invoke(choice.choiceID);

            // Execute choice effects
            choice.ExecuteEffects(this);

            // Advance to target node
            AdvanceToNode(choice.targetNodeID);
        }

        /// <summary>
        /// Gets the current dialogue data
        /// </summary>
        public DialogueData GetCurrentDialogue()
        {
            return currentDialogue;
        }

        /// <summary>
        /// Gets the current dialogue ID
        /// </summary>
        public string GetCurrentDialogueID()
        {
            return currentDialogue?.dialogueID;
        }

        /// <summary>
        /// Gets the current dialogue node
        /// </summary>
        public DialogueNode GetCurrentNode()
        {
            return currentNode;
        }

        /// <summary>
        /// Checks if dialogue is currently active
        /// </summary>
        public bool IsDialogueActive()
        {
            return currentDialogue != null && currentNode != null;
        }

        /// <summary>
        /// Gets all dialogue flags (read-only)
        /// </summary>
        public System.Collections.Generic.IReadOnlyDictionary<string, bool> GetDialogueFlags()
        {
            return dialogueFlags;
        }

        #region IDialogueConditionEvaluator Implementation

        public bool EvaluateFlagCondition(string flagName, bool requiredValue)
        {
            // Check global flags first (persist across scenes)
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                if (saveManager.EvaluateGlobalFlag(flagName, requiredValue))
                {
                    return true;
                }
            }
            
            // Fallback to local dialogue flags (session-specific)
            return dialogueFlags.TryGetValue(flagName, out bool value) && value == requiredValue;
        }

        public bool EvaluateInventoryCondition(string itemID, int requiredQuantity)
        {
            if (InventoryManager.Instance != null)
            {
                return InventoryManager.Instance.HasItem(itemID, requiredQuantity);
            }
            return false;
        }

        public bool EvaluateQuestCondition(string questID, string requiredState)
        {
            // TODO: Integrate with actual quest system
            // For now, return true (assume quest state is correct)
            return true;
        }

        public bool EvaluateCustomCondition(string conditionType, string[] parameters)
        {
            // TODO: Implement custom condition evaluation
            // For now, return true
            return true;
        }

        #endregion

        #region IDialogueEffectExecutor Implementation

        public void SetFlag(string flagName, bool value)
        {
            dialogueFlags[flagName] = value;
        }

        public void SetGlobalFlag(string flagName, bool value)
        {
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SetGlobalFlag(flagName, value);
            }
            else
            {
                Debug.LogWarning($"Cannot set global flag '{flagName}': SaveManager.Instance is null");
            }
        }

        public void AddItem(string itemID, int quantity)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(itemID, quantity);
            }
            else
            {
                Debug.LogWarning($"Cannot add item '{itemID}': InventoryManager.Instance is null");
            }
        }

        public void RemoveItem(string itemID, int quantity)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.RemoveItem(itemID, quantity);
            }
            else
            {
                Debug.LogWarning($"Cannot remove item '{itemID}': InventoryManager.Instance is null");
            }
        }

        public void UpdateQuest(string questID, string newState)
        {
            // TODO: Integrate with actual quest system
            Debug.Log($"Updating quest '{questID}' to state '{newState}'");
        }

        public void ExecuteCustomEffect(string effectType, string[] parameters)
        {
            // TODO: Implement custom effect execution
            Debug.Log($"Executing custom effect '{effectType}'");
        }

        public void PlayAnimation(string animationName)
        {
            // TODO: Integrate with animation system
            Debug.Log($"Playing animation '{animationName}'");
        }

        public void TriggerEvent(string eventName)
        {
            // TODO: Integrate with event system
            Debug.Log($"Triggering event '{eventName}'");
        }

        #endregion

        #region Player Movement Control

        /// <summary>
        /// Freezes the player movement and combat during dialogue
        /// </summary>
        private void FreezePlayerMovement()
        {
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
            }

            if (playerController != null)
            {
                playerController.SetMovementEnabled(false);
            }
            
            // Also freeze combat to prevent sword throwing during dialogue
            if (playerCombat == null)
            {
                playerCombat = FindFirstObjectByType<Unbound.Player.PlayerCombat>();
            }

            if (playerCombat != null)
            {
                playerCombat.SetCombatEnabled(false);
            }
        }

        /// <summary>
        /// Unfreezes the player movement and combat after dialogue
        /// </summary>
        private void UnfreezePlayerMovement()
        {
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
            }
            
            // Also unfreeze combat
            if (playerCombat != null)
            {
                playerCombat.SetCombatEnabled(true);
            }
        }

        #endregion

        #region Dialogue Progress Persistence

        /// <summary>
        /// Saves the current dialogue progress
        /// </summary>
        private void SaveDialogueProgress()
        {
            if (currentDialogue == null)
                return;

            var progressData = new DialogueProgressData
            {
                dialogueID = currentDialogue.dialogueID,
                currentNodeID = dialogueState.currentNodeID,
                visitedNodes = new System.Collections.Generic.List<string>(dialogueState.visitedNodes),
                flags = new System.Collections.Generic.Dictionary<string, bool>(dialogueFlags)
            };

            // Save to persistent storage (integrate with SaveManager)
            var saveManager = SaveManager.Instance;
            if (saveManager != null && currentDialogue != null)
            {
                var saveData = saveManager.GetCurrentSaveData();
                saveData.AddCustomData($"dialogue_progress_{currentDialogue.dialogueID}", progressData);
                saveManager.Save(saveData);
            }
        }

        /// <summary>
        /// Restores dialogue progress for the current dialogue
        /// </summary>
        private void RestoreDialogueProgress()
        {
            if (currentDialogue == null)
                return;

            // Load from persistent storage (integrate with SaveManager)
            var saveManager = SaveManager.Instance;
            if (saveManager != null)
            {
                var saveData = saveManager.GetCurrentSaveData();
                var progressData = saveData.GetCustomData<DialogueProgressData>($"dialogue_progress_{currentDialogue.dialogueID}");

                if (progressData != null)
                {
                    dialogueState.currentNodeID = progressData.currentNodeID;
                    foreach (var nodeID in progressData.visitedNodes)
                    {
                        dialogueState.visitedNodes.Add(nodeID);
                    }

                    foreach (var flag in progressData.flags)
                    {
                        dialogueFlags[flag.Key] = flag.Value;
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Runtime state of a dialogue session
    /// </summary>
    [System.Serializable]
    public class DialogueState
    {
        public string currentNodeID;
        public System.Collections.Generic.HashSet<string> visitedNodes = new System.Collections.Generic.HashSet<string>();

        public void Reset()
        {
            currentNodeID = null;
            visitedNodes.Clear();
        }
    }

    /// <summary>
    /// Serializable data for saving dialogue progress
    /// </summary>
    [System.Serializable]
    public class DialogueProgressData
    {
        public string dialogueID;
        public string currentNodeID;
        public System.Collections.Generic.List<string> visitedNodes = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.Dictionary<string, bool> flags = new System.Collections.Generic.Dictionary<string, bool>();
    }
}
