using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Represents a single node in a dialogue conversation.
    /// Each node contains the text, speaker info, and branching logic.
    /// </summary>
    [System.Serializable]
    public class DialogueNode
    {
        public string nodeID;
        public string speakerID;
        public string dialogueTextKey;

        [Header("Visual Settings")]
        public Sprite portraitSprite;
        public GifAsset portraitGif;
        public string animationTrigger;

        [Header("Flow Control")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();
        public string nextNodeID; // For linear progression without choices
        public List<DialogueCondition> conditions = new List<DialogueCondition>();

        [Header("Actions & Effects")]
        public List<DialogueEffect> effects = new List<DialogueEffect>();

        [Header("Timing")]
        [Min(0f)] public float autoAdvanceDelay = 0f;
        [Min(0f)] public float textSpeed = 30f; // Characters per second

        /// <summary>
        /// Validates this node against the parent dialogue asset
        /// </summary>
        public bool IsValid(DialogueAsset asset)
        {
            return string.IsNullOrEmpty(GetValidationErrors(asset));
        }

        /// <summary>
        /// Gets detailed validation errors for this node
        /// </summary>
        public string GetValidationErrors(DialogueAsset asset)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(nodeID))
            {
                errors.Add("Node ID is null or empty");
            }

            if (string.IsNullOrEmpty(dialogueTextKey))
            {
                errors.Add("Dialogue text key is null or empty");
            }

            // Validate choices reference existing nodes
            foreach (var choice in choices)
            {
                string choiceError = choice.GetValidationErrors(asset);
                if (!string.IsNullOrEmpty(choiceError))
                {
                    errors.Add(choiceError);
                }
            }

            // Validate next node exists if specified
            if (!string.IsNullOrEmpty(nextNodeID) && asset.GetNode(nextNodeID) == null)
            {
                errors.Add($"Next node '{nextNodeID}' does not exist in dialogue asset");
            }

            return errors.Count > 0 ? $"Node '{nodeID}' errors: {string.Join("; ", errors)}" : string.Empty;
        }

        /// <summary>
        /// Gets the next node ID for linear progression (ignores choices)
        /// </summary>
        public string GetNextNodeID()
        {
            return nextNodeID;
        }

        /// <summary>
        /// Gets all available choices (filtered by conditions)
        /// </summary>
        public List<DialogueChoice> GetAvailableChoices(IDialogueConditionEvaluator evaluator)
        {
            var availableChoices = new List<DialogueChoice>();

            foreach (var choice in choices)
            {
                if (choice.EvaluateConditions(evaluator))
                {
                    availableChoices.Add(choice);
                }
            }

            return availableChoices;
        }

        /// <summary>
        /// Executes all effects for this node
        /// </summary>
        public void ExecuteEffects(IDialogueEffectExecutor executor)
        {
            foreach (var effect in effects)
            {
                effect.Execute(executor);
            }
        }

        /// <summary>
        /// Checks if this node has a GIF portrait configured
        /// </summary>
        public bool IsGifPortrait()
        {
            return portraitGif != null;
        }

        /// <summary>
        /// Checks if this node has a regular sprite portrait configured
        /// </summary>
        public bool IsSpritePortrait()
        {
            return portraitSprite != null;
        }

        /// <summary>
        /// Gets the appropriate portrait asset (prioritizes GIF over sprite if both are set)
        /// </summary>
        public Object GetPortraitAsset()
        {
            if (IsGifPortrait())
            {
                return portraitGif;
            }
            else if (IsSpritePortrait())
            {
                return portraitSprite;
            }
            return null;
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(nodeID))
            {
                nodeID = $"node_{GetHashCode()}";
            }
        }
    }
}
