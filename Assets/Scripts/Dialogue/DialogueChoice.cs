using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Represents a choice that the player can select in a dialogue.
    /// </summary>
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceID;
        public string choiceTextKey;
        public string targetNodeID;

        [Header("Conditions")]
        public List<DialogueCondition> conditions = new List<DialogueCondition>();

        [Header("Effects")]
        public List<DialogueEffect> effects = new List<DialogueEffect>();

        /// <summary>
        /// Validates this choice against the parent dialogue data
        /// </summary>
        public bool IsValid(IDialogueDataProvider data)
        {
            return string.IsNullOrEmpty(GetValidationErrors(data));
        }

        /// <summary>
        /// Gets detailed validation errors for this choice
        /// </summary>
        public string GetValidationErrors(IDialogueDataProvider data)
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(choiceID))
            {
                errors.Add("Choice ID is null or empty");
            }

            if (string.IsNullOrEmpty(choiceTextKey))
            {
                errors.Add("Choice text key is null or empty");
            }

            if (string.IsNullOrEmpty(targetNodeID))
            {
                errors.Add("Target node ID is null or empty");
            }
            else if (data.GetNode(targetNodeID) == null)
            {
                errors.Add($"Target node '{targetNodeID}' does not exist in dialogue data");
            }

            return errors.Count > 0 ? $"Choice '{choiceID}' errors: {string.Join("; ", errors)}" : string.Empty;
        }

        /// <summary>
        /// Evaluates whether this choice should be available
        /// </summary>
        public bool EvaluateConditions(IDialogueConditionEvaluator evaluator)
        {
            foreach (var condition in conditions)
            {
                if (!condition.Evaluate(evaluator))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Executes all effects for this choice
        /// </summary>
        public void ExecuteEffects(IDialogueEffectExecutor executor)
        {
            foreach (var effect in effects)
            {
                effect.Execute(executor);
            }
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(choiceID))
            {
                choiceID = $"choice_{GetHashCode()}";
            }
        }
    }
}
