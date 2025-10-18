using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Represents an effect that occurs when a dialogue node or choice is executed
    /// </summary>
    [System.Serializable]
    public class DialogueEffect
    {
        public enum EffectType
        {
            SetFlag,
            AddItem,
            RemoveItem,
            UpdateQuest,
            PlayAnimation,
            TriggerEvent,
            Custom
        }

        public EffectType effectType;

        [Header("Flag Effect")]
        public string flagName;
        public bool flagValue;

        [Header("Item Effect")]
        public string itemID;
        public int itemQuantity = 1;

        [Header("Quest Effect")]
        public string questID;
        public string questNewState;

        [Header("Animation Effect")]
        public string animationName;

        [Header("Event Effect")]
        public string eventName;

        [Header("Custom Effect")]
        public string customEffectType;
        public string[] customParameters;

        /// <summary>
        /// Executes this effect using the provided executor
        /// </summary>
        public void Execute(IDialogueEffectExecutor executor)
        {
            switch (effectType)
            {
                case EffectType.SetFlag:
                    executor.SetFlag(flagName, flagValue);
                    break;

                case EffectType.AddItem:
                    executor.AddItem(itemID, itemQuantity);
                    break;

                case EffectType.RemoveItem:
                    executor.RemoveItem(itemID, itemQuantity);
                    break;

                case EffectType.UpdateQuest:
                    executor.UpdateQuest(questID, questNewState);
                    break;

                case EffectType.PlayAnimation:
                    executor.PlayAnimation(animationName);
                    break;

                case EffectType.TriggerEvent:
                    executor.TriggerEvent(eventName);
                    break;

                case EffectType.Custom:
                    executor.ExecuteCustomEffect(customEffectType, customParameters);
                    break;

                default:
                    Debug.LogWarning($"Unknown effect type: {effectType}");
                    break;
            }
        }

        /// <summary>
        /// Gets a human-readable description of this effect
        /// </summary>
        public string GetDescription()
        {
            switch (effectType)
            {
                case EffectType.SetFlag:
                    return $"Set flag '{flagName}' to {flagValue}";

                case EffectType.AddItem:
                    return $"Add {itemQuantity} of item '{itemID}'";

                case EffectType.RemoveItem:
                    return $"Remove {itemQuantity} of item '{itemID}'";

                case EffectType.UpdateQuest:
                    return $"Set quest '{questID}' to state '{questNewState}'";

                case EffectType.PlayAnimation:
                    return $"Play animation '{animationName}'";

                case EffectType.TriggerEvent:
                    return $"Trigger event '{eventName}'";

                case EffectType.Custom:
                    return $"Execute custom effect '{customEffectType}'";

                default:
                    return "Unknown effect";
            }
        }
    }
}
