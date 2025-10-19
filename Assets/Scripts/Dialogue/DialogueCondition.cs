using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Represents a condition that must be met for dialogue choices or nodes to be available
    /// </summary>
    [System.Serializable]
    public class DialogueCondition
    {
        public enum ConditionType
        {
            Flag,
            Inventory,
            Quest,
            Custom
        }

        public ConditionType conditionType;

        [Header("Flag Condition")]
        public string flagName;
        public bool requiredFlagValue;

        [Header("Inventory Condition")]
        public string itemID;
        public int requiredQuantity = 1;

        [Header("Quest Condition")]
        public string questID;
        public string requiredQuestState;

        [Header("Custom Condition")]
        public string customConditionType;
        public string[] customParameters;

        /// <summary>
        /// Evaluates this condition using the provided evaluator
        /// </summary>
        public bool Evaluate(IDialogueConditionEvaluator evaluator)
        {
            switch (conditionType)
            {
                case ConditionType.Flag:
                    return evaluator.EvaluateFlagCondition(flagName, requiredFlagValue);

                case ConditionType.Inventory:
                    return evaluator.EvaluateInventoryCondition(itemID, requiredQuantity);

                case ConditionType.Quest:
                    return evaluator.EvaluateQuestCondition(questID, requiredQuestState);

                case ConditionType.Custom:
                    return evaluator.EvaluateCustomCondition(customConditionType, customParameters);

                default:
                    Debug.LogWarning($"Unknown condition type: {conditionType}");
                    return true; // Default to allowing if condition type is unknown
            }
        }

        /// <summary>
        /// Gets a human-readable description of this condition
        /// </summary>
        public string GetDescription()
        {
            switch (conditionType)
            {
                case ConditionType.Flag:
                    return $"Flag '{flagName}' must be {requiredFlagValue}";

                case ConditionType.Inventory:
                    return $"Must have at least {requiredQuantity} of item '{itemID}'";

                case ConditionType.Quest:
                    return $"Quest '{questID}' must be in state '{requiredQuestState}'";

                case ConditionType.Custom:
                    return $"Custom condition '{customConditionType}'";

                default:
                    return "Unknown condition";
            }
        }
    }
}


