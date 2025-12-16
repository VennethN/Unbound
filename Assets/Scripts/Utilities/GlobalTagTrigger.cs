using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using Unbound.Global;

namespace Unbound.Global
{
    /// <summary>
    /// Triggers an event automatically when all required global flags
    /// have entered this trigger area at least once.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GlobalFlagTrigger : MonoBehaviour
    {
        [Header("Required Global Flags")]
        [Tooltip("All flags that must enter this trigger at least once")]
        [SerializeField] private List<string> requiredFlags = new List<string>();

        [Header("Trigger Settings")]
        [Tooltip("If true, event only fires once")]
        [SerializeField] private bool triggerOnce = true;

        [Header("Events")]
        public UnityEvent OnAllFlagsMet;

        private HashSet<string> collectedFlags = new HashSet<string>();
        private bool hasTriggered = false;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (hasTriggered && triggerOnce)
                return;

            GlobalFlagCarrier flagCarrier = other.GetComponent<GlobalFlagCarrier>();
            if (flagCarrier == null)
                return;

            if (string.IsNullOrEmpty(flagCarrier.flagID))
                return;

            collectedFlags.Add(flagCarrier.flagID);

            CheckConditions();
        }

        private void CheckConditions()
        {
            foreach (string flag in requiredFlags)
            {
                if (!collectedFlags.Contains(flag))
                    return;
            }

            TriggerEvent();
        }

        private void TriggerEvent()
        {
            if (hasTriggered && triggerOnce)
                return;

            hasTriggered = true;
            OnAllFlagsMet?.Invoke();
        }
    }
}
