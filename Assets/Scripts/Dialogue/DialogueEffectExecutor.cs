using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace Unbound.Dialogue
{
    public class DialogueEffectExecutor : MonoBehaviour, IDialogueEffectExecutor
    {
        [System.Serializable]
        public class NamedEvent
        {
            public string eventName;
            public UnityEvent unityEvent;
        }

        [Header("Custom Dialogue Events")]
        public List<NamedEvent> events = new List<NamedEvent>();

        private Dictionary<string, UnityEvent> _eventLookup;

        private void Awake()
        {
            _eventLookup = new Dictionary<string, UnityEvent>();

            foreach (var e in events)
            {
                if (!string.IsNullOrEmpty(e.eventName) && !_eventLookup.ContainsKey(e.eventName))
                {
                    _eventLookup.Add(e.eventName, e.unityEvent);
                }
            }
        }

        public void TriggerEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return;

            if (_eventLookup.TryGetValue(eventName, out var unityEvent))
            {
                unityEvent?.Invoke();
            }
            else
            {
                Debug.LogWarning($"DialogueEffectExecutor: No event found for '{eventName}'");
            }
        }

        // --- Stub the rest for now (or implement later) ---
        public void SetFlag(string flagName, bool value) {}
        public void SetGlobalFlag(string flagName, bool value) {}
        public void AddItem(string itemID, int quantity) {}
        public void RemoveItem(string itemID, int quantity) {}
        public void UpdateQuest(string questID, string newState) {}
        public void ExecuteCustomEffect(string effectType, string[] parameters) {}
        public void PlayAnimation(string animationName) {}
    }
}
