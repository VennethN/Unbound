using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Puzzle
{
    /// <summary>
    /// Manages a puzzle with multiple pushable objects and targets.
    /// Tracks puzzle state and triggers completion events when all targets are satisfied.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        [System.Serializable]
        public class PuzzlePair
        {
            [Tooltip("The pushable object that needs to reach the target")]
            public PushableObject pushableObject;
            
            [Tooltip("The target that the pushable object needs to reach")]
            public PuzzleTarget target;
            
            [Tooltip("Whether this pair is required for puzzle completion")]
            public bool isRequired = true;
        }

        [Header("Puzzle Configuration")]
        [SerializeField] private List<PuzzlePair> puzzlePairs = new List<PuzzlePair>();
        [SerializeField] private bool autoFindPairsInScene = false;
        [SerializeField] private bool resetOnCompletion = false;
        
        [Header("Completion Settings")]
        [SerializeField] private float completionDelay = 0f;
        [SerializeField] private bool checkContinuous = true;
        [SerializeField] private float resetDelay = 0.1f;
        [SerializeField] private bool resetPositions = true;
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        [Header("Events")]
        public UnityEvent onPuzzleComplete;
        public UnityEvent onPuzzleReset;
        [SerializeField] private UnityEvent<int, int> onProgressChanged; // (solved, total)

        private bool isCompleted = false;
        private int totalRequiredPairs = 0;

        private void Awake()
        {
            if (autoFindPairsInScene)
            {
                FindPairsInScene();
            }

            ValidatePairs();
            CalculateTotalRequiredPairs();
        }

        private void Start()
        {
            // Store initial positions for pushable objects
            if (resetPositions)
            {
                foreach (var pair in puzzlePairs)
                {
                    if (pair.pushableObject != null && !pair.pushableObject.HasInitialPosition())
                    {
                        pair.pushableObject.SetInitialPosition(pair.pushableObject.transform.position);
                    }
                }
            }

            // Subscribe to target events
            foreach (var pair in puzzlePairs)
            {
                if (pair.target != null)
                {
                    pair.target.onTargetReached.AddListener(OnTargetReached);
                    pair.target.onTargetLeft.AddListener(OnTargetLeft);
                }
            }

            UpdateProgress();
        }

        private void Update()
        {
            if (checkContinuous && !isCompleted)
            {
                CheckPuzzleCompletion();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            foreach (var pair in puzzlePairs)
            {
                if (pair.target != null)
                {
                    pair.target.onTargetReached.RemoveListener(OnTargetReached);
                    pair.target.onTargetLeft.RemoveListener(OnTargetLeft);
                }
            }
        }

        private void FindPairsInScene()
        {
            puzzlePairs.Clear();
            
            var pushables = FindObjectsByType<PushableObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var targets = FindObjectsByType<PuzzleTarget>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            // Try to match pushables with targets based on proximity or names
            foreach (var pushable in pushables)
            {
                PuzzleTarget closestTarget = null;
                float closestDistance = float.MaxValue;

                foreach (var target in targets)
                {
                    float distance = Vector2.Distance(pushable.transform.position, target.transform.position);
                    if (distance < closestDistance && distance < 10f) // Within 10 units
                    {
                        closestDistance = distance;
                        closestTarget = target;
                    }
                }

                if (closestTarget != null)
                {
                    puzzlePairs.Add(new PuzzlePair
                    {
                        pushableObject = pushable,
                        target = closestTarget,
                        isRequired = true
                    });
                }
            }
        }

        private void ValidatePairs()
        {
            for (int i = puzzlePairs.Count - 1; i >= 0; i--)
            {
                var pair = puzzlePairs[i];
                if (pair.pushableObject == null || pair.target == null)
                {
                    Debug.LogWarning($"PuzzleManager on {gameObject.name} has an invalid pair at index {i}. Removing it.", this);
                    puzzlePairs.RemoveAt(i);
                }
            }
        }

        private void CalculateTotalRequiredPairs()
        {
            totalRequiredPairs = puzzlePairs.Count(p => p.isRequired);
        }

        private void OnTargetReached(PushableObject pushable)
        {
            UpdateProgress();
            
            if (!isCompleted)
            {
                CheckPuzzleCompletion();
            }
        }

        private void OnTargetLeft(PushableObject pushable)
        {
            if (isCompleted)
            {
                isCompleted = false;
            }
            
            UpdateProgress();
        }

        private void CheckPuzzleCompletion()
        {
            if (isCompleted) return;

            int solvedCount = 0;
            foreach (var pair in puzzlePairs)
            {
                if (pair.isRequired && pair.target != null && pair.target.IsSolved())
                {
                    solvedCount++;
                }
            }

            if (solvedCount >= totalRequiredPairs)
            {
                CompletePuzzle();
            }
        }

        private void CompletePuzzle()
        {
            if (isCompleted) return;

            isCompleted = true;

            if (completionDelay > 0f)
            {
                Invoke(nameof(TriggerCompletion), completionDelay);
            }
            else
            {
                TriggerCompletion();
            }
        }

        private void TriggerCompletion()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PuzzleManager] Puzzle completed on '{gameObject.name}'", this);
            }
            
            onPuzzleComplete?.Invoke();

            if (resetOnCompletion)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PuzzleManager] Resetting puzzle in {resetDelay} seconds", this);
                }
                Invoke(nameof(ResetPuzzle), resetDelay);
            }
        }

        private void UpdateProgress()
        {
            int solvedCount = puzzlePairs.Count(p => p.isRequired && p.target != null && p.target.IsSolved());
            onProgressChanged?.Invoke(solvedCount, totalRequiredPairs);
        }

        /// <summary>
        /// Resets the puzzle by resetting all targets and optionally positions
        /// </summary>
        public void ResetPuzzle()
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PuzzleManager] Resetting puzzle on '{gameObject.name}'", this);
            }

            foreach (var pair in puzzlePairs)
            {
                if (pair.target != null)
                {
                    pair.target.ResetTarget();
                }
                if (pair.pushableObject != null)
                {
                    pair.pushableObject.Stop();
                    if (resetPositions)
                    {
                        pair.pushableObject.ResetPosition();
                    }
                }
            }

            isCompleted = false;
            UpdateProgress();
            onPuzzleReset?.Invoke();
        }

        /// <summary>
        /// Gets the current progress (solved pairs / total required pairs)
        /// </summary>
        public float GetProgress()
        {
            if (totalRequiredPairs == 0) return 1f;
            
            int solvedCount = puzzlePairs.Count(p => p.isRequired && p.target != null && p.target.IsSolved());
            return (float)solvedCount / totalRequiredPairs;
        }

        /// <summary>
        /// Gets whether the puzzle is completed
        /// </summary>
        public bool IsCompleted()
        {
            return isCompleted;
        }

        /// <summary>
        /// Adds a new puzzle pair at runtime
        /// </summary>
        public void AddPuzzlePair(PushableObject pushable, PuzzleTarget target, bool isRequired = true)
        {
            var pair = new PuzzlePair
            {
                pushableObject = pushable,
                target = target,
                isRequired = isRequired
            };

            puzzlePairs.Add(pair);

            if (target != null)
            {
                target.onTargetReached.AddListener(OnTargetReached);
                target.onTargetLeft.AddListener(OnTargetLeft);
            }

            CalculateTotalRequiredPairs();
            UpdateProgress();
        }

        /// <summary>
        /// Removes a puzzle pair at runtime
        /// </summary>
        public void RemovePuzzlePair(PushableObject pushable, PuzzleTarget target)
        {
            for (int i = puzzlePairs.Count - 1; i >= 0; i--)
            {
                var pair = puzzlePairs[i];
                if (pair.pushableObject == pushable && pair.target == target)
                {
                    if (pair.target != null)
                    {
                        pair.target.onTargetReached.RemoveListener(OnTargetReached);
                        pair.target.onTargetLeft.RemoveListener(OnTargetLeft);
                    }
                    puzzlePairs.RemoveAt(i);
                    break;
                }
            }

            CalculateTotalRequiredPairs();
            UpdateProgress();
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw lines connecting pushable objects to their targets
            Gizmos.color = Color.magenta;
            foreach (var pair in puzzlePairs)
            {
                if (pair.pushableObject != null && pair.target != null)
                {
                    Gizmos.DrawLine(
                        pair.pushableObject.transform.position,
                        pair.target.transform.position
                    );
                }
            }
        }
#endif
    }
}

