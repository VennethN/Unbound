namespace Unbound.Dialogue
{
    /// <summary>
    /// Interface for executing dialogue effects
    /// </summary>
    public interface IDialogueEffectExecutor
    {
        /// <summary>
        /// Sets a dialogue flag to a specific value
        /// </summary>
        void SetFlag(string flagName, bool value);

        /// <summary>
        /// Adds an item to the player's inventory
        /// </summary>
        void AddItem(string itemID, int quantity);

        /// <summary>
        /// Removes an item from the player's inventory
        /// </summary>
        void RemoveItem(string itemID, int quantity);

        /// <summary>
        /// Updates a quest's state
        /// </summary>
        void UpdateQuest(string questID, string newState);

        /// <summary>
        /// Executes a custom effect with arbitrary data
        /// </summary>
        void ExecuteCustomEffect(string effectType, string[] parameters);

        /// <summary>
        /// Plays a dialogue-specific animation or cutscene
        /// </summary>
        void PlayAnimation(string animationName);

        /// <summary>
        /// Triggers a UnityEvent or custom event
        /// </summary>
        void TriggerEvent(string eventName);
    }
}

