using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Runtime controller that manages dialogue flow and presentation
    /// </summary>
    public class DialogueController : MonoBehaviour, IDialogueConditionEvaluator, IDialogueEffectExecutor
    {
        [Header("Configuration")]
        [SerializeField] private DialogueAsset defaultDialogueAsset;

        [Header("Input")]
        [SerializeField] private InputActionReference continueAction;

        [Header("UI References")]
        [SerializeField] private DialogueView dialogueView;

        [Header("Settings")]
        [SerializeField] private bool autoAdvanceOnComplete = false;

        // Runtime state
        private DialogueAsset currentDialogue;
        private DialogueNode currentNode;
        private DialogueState dialogueState;
        private Coroutine currentTextCoroutine;

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
        /// Starts a dialogue conversation
        /// </summary>
        public void StartDialogue(DialogueAsset dialogueAsset)
        {
            if (dialogueAsset == null)
            {
                Debug.LogError("Cannot start dialogue: dialogue asset is null");
                return;
            }

            string validationErrors = dialogueAsset.GetValidationErrors();
            if (!string.IsNullOrEmpty(validationErrors))
            {
                Debug.LogError($"Cannot start dialogue: {validationErrors}");
                return;
            }

            currentDialogue = dialogueAsset;
            dialogueState.Reset();

            // Restore any existing progress for this dialogue
            RestoreDialogueProgress();

            OnDialogueStart?.Invoke();
            AdvanceToNode(dialogueAsset.startNodeID);
        }

        /// <summary>
        /// Starts the default dialogue
        /// </summary>
        public void StartDefaultDialogue()
        {
            if (defaultDialogueAsset != null)
            {
                StartDialogue(defaultDialogueAsset);
            }
            else
            {
                Debug.LogWarning("No default dialogue asset assigned");
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
        /// Gets the current dialogue asset
        /// </summary>
        public DialogueAsset GetCurrentDialogue()
        {
            return currentDialogue;
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

        #region IDialogueConditionEvaluator Implementation

        public bool EvaluateFlagCondition(string flagName, bool requiredValue)
        {
            return dialogueFlags.TryGetValue(flagName, out bool value) && value == requiredValue;
        }

        public bool EvaluateInventoryCondition(string itemID, int requiredQuantity)
        {
            // TODO: Integrate with actual inventory system
            // For now, return true (assume item is available)
            return true;
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

        public void AddItem(string itemID, int quantity)
        {
            // TODO: Integrate with actual inventory system
            Debug.Log($"Adding {quantity} of item '{itemID}' to inventory");
        }

        public void RemoveItem(string itemID, int quantity)
        {
            // TODO: Integrate with actual inventory system
            Debug.Log($"Removing {quantity} of item '{itemID}' from inventory");
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
            if (saveManager != null)
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
