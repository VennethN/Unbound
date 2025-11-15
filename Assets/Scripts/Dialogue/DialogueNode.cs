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
        [Tooltip("Path to portrait sprite (for JSON loading). Loaded at runtime if portraitSprite is null.")]
        public string portraitSpritePath;
        [Tooltip("Path to portrait GIF asset (for JSON loading). Loaded at runtime if portraitGif is null.")]
        public string portraitGifPath;
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
        /// Validates this node against the parent dialogue data
        /// </summary>
        public bool IsValid(IDialogueDataProvider data)
        {
            return string.IsNullOrEmpty(GetValidationErrors(data));
        }

        /// <summary>
        /// Gets detailed validation errors for this node
        /// </summary>
        public string GetValidationErrors(IDialogueDataProvider data)
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
                string choiceError = choice.GetValidationErrors(data);
                if (!string.IsNullOrEmpty(choiceError))
                {
                    errors.Add(choiceError);
                }
            }

            // Validate next node exists if specified
            if (!string.IsNullOrEmpty(nextNodeID) && data.GetNode(nextNodeID) == null)
            {
                errors.Add($"Next node '{nextNodeID}' does not exist in dialogue data");
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
            return GetPortraitGif() != null;
        }

        /// <summary>
        /// Gets the portrait GIF, loading it from path if necessary
        /// </summary>
        public GifAsset GetPortraitGif()
        {
            // Return directly assigned GIF if available
            if (portraitGif != null)
                return portraitGif;

            // Try loading from path if provided
            if (!string.IsNullOrEmpty(portraitGifPath))
            {
                portraitGif = GifAssetLoader.Load(portraitGifPath);
                return portraitGif;
            }

            return null;
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
                return GetPortraitGif();
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
