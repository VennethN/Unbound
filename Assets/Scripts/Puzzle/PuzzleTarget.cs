using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Puzzle
{
    /// <summary>
    /// Component that acts as a target/receiver for pushable objects.
    /// Detects when pushable objects reach this target and triggers events.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PuzzleTarget : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private float detectionRadius = 0.5f;
        [SerializeField] private bool useTriggerCollider = true;
        [SerializeField] private bool requireExactMatch = false;
        [Tooltip("If set, only this specific pushable object will trigger this target")]
        [SerializeField] private PushableObject requiredPushableObject;
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        [Header("Events")]
        public UnityEvent<PushableObject> onTargetReached;
        public UnityEvent<PushableObject> onTargetLeft;
        [SerializeField] private UnityEvent onPuzzleSolved;

        private Collider2D targetCollider;
        private PushableObject currentPushableObject;
        private bool isSolved = false;
        private float lastResetTime = -1f;
        private const float RESET_COOLDOWN = 0.5f; // Prevent immediate re-trigger after reset

        private void Awake()
        {
            targetCollider = GetComponent<Collider2D>();
            
            if (targetCollider == null)
            {
                Debug.LogError($"PuzzleTarget on {gameObject.name} requires a Collider2D component!", this);
                return;
            }

            // Configure collider as trigger if needed
            if (useTriggerCollider)
            {
                targetCollider.isTrigger = true;
            }
        }

        private void Update()
        {
            // Continuous detection if not using trigger collider
            if (!useTriggerCollider && !isSolved)
            {
                CheckForPushableObjects();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (useTriggerCollider)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PuzzleTarget] Trigger entered by '{other.gameObject.name}'", this);
                }
                CheckPushableObject(other.gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            // Also check during stay to catch objects that entered when already solved
            // But skip if already solved with this object
            if (useTriggerCollider)
            {
                var pushable = other.GetComponent<PushableObject>();
                if (pushable != null && isSolved && currentPushableObject == pushable)
                {
                    return; // Already solved, don't check again
                }
                CheckPushableObject(other.gameObject);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (useTriggerCollider)
            {
                var pushable = other.GetComponent<PushableObject>();
                if (pushable != null && pushable == currentPushableObject)
                {
                    HandleTargetLeft(pushable);
                }
            }
        }

        private void CheckForPushableObjects()
        {
            // Find all pushable objects within detection radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
            
            foreach (var collider in colliders)
            {
                CheckPushableObject(collider.gameObject);
            }
        }

        private void CheckPushableObject(GameObject obj)
        {
            var pushable = obj.GetComponent<PushableObject>();
            if (pushable == null) return;

            // Prevent re-triggering immediately after reset
            if (Time.time - lastResetTime < RESET_COOLDOWN)
            {
                return;
            }

            // Check if this is the required pushable object (if specified)
            if (requiredPushableObject != null && pushable != requiredPushableObject)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PuzzleTarget] Object '{obj.name}' is not the required pushable object", this);
                }
                return;
            }

            // When using trigger collider, trust the trigger - just check distance as secondary validation
            // When not using trigger, use detection radius
            float distance = Vector2.Distance(transform.position, obj.transform.position);
            float checkRadius = useTriggerCollider ? detectionRadius * 2f : detectionRadius; // More lenient with triggers
            
            if (distance <= checkRadius)
            {
                // If already solved with this object, don't trigger again
                if (isSolved && currentPushableObject == pushable)
                {
                    return;
                }
                
                HandleTargetReached(pushable);
            }
            else if (enableDebugLogs)
            {
                Debug.Log($"[PuzzleTarget] Object '{obj.name}' too far: {distance:F2} > {checkRadius:F2}", this);
            }
        }

        private void HandleTargetReached(PushableObject pushable)
        {
            if (currentPushableObject == pushable && isSolved)
            {
                return; // Already solved with this object
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[PuzzleTarget] Target '{gameObject.name}' reached by '{pushable.gameObject.name}'", this);
            }

            currentPushableObject = pushable;
            isSolved = true;
            
            // Stop the pushable object if exact match is required
            if (requireExactMatch)
            {
                pushable.Stop();
            }

            onTargetReached?.Invoke(pushable);
            onPuzzleSolved?.Invoke();
        }

        private void HandleTargetLeft(PushableObject pushable)
        {
            if (currentPushableObject == pushable)
            {
                currentPushableObject = null;
                isSolved = false;
                onTargetLeft?.Invoke(pushable);
            }
        }

        /// <summary>
        /// Checks if this target has been reached
        /// </summary>
        public bool IsSolved()
        {
            return isSolved;
        }

        /// <summary>
        /// Gets the current pushable object on this target
        /// </summary>
        public PushableObject GetCurrentPushableObject()
        {
            return currentPushableObject;
        }

        /// <summary>
        /// Resets the target state
        /// </summary>
        public void ResetTarget()
        {
            currentPushableObject = null;
            isSolved = false;
            lastResetTime = Time.time;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[PuzzleTarget] Target '{gameObject.name}' reset", this);
            }
        }

        /// <summary>
        /// Sets the required pushable object for this target
        /// </summary>
        public void SetRequiredPushableObject(PushableObject pushable)
        {
            requiredPushableObject = pushable;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = isSolved ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
#endif
    }
}

