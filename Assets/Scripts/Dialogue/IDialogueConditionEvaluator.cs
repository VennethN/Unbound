namespace Unbound.Dialogue
{
    /// <summary>
    /// Interface for evaluating dialogue conditions
    /// </summary>
    public interface IDialogueConditionEvaluator
    {
        /// <summary>
        /// Evaluates whether a dialogue flag condition is met
        /// </summary>
        bool EvaluateFlagCondition(string flagName, bool requiredValue);

        /// <summary>
        /// Evaluates whether an inventory condition is met
        /// </summary>
        bool EvaluateInventoryCondition(string itemID, int requiredQuantity);

        /// <summary>
        /// Evaluates whether a quest condition is met
        /// </summary>
        bool EvaluateQuestCondition(string questID, string requiredState);

        /// <summary>
        /// Evaluates a custom condition with arbitrary data
        /// </summary>
        bool EvaluateCustomCondition(string conditionType, string[] parameters);
    }
}


