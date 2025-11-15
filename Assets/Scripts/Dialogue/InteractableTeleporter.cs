using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unbound.Utilities;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Interactable component that teleports the player to a destination within the current scene.
    /// Supports optional global flag requirements and blocked dialogue feedback.
    /// </summary>
    public class InteractableTeleporter : BaseInteractable
    {
        [Header("Teleport Settings")]
        [SerializeField] private Transform destinationPoint;
        [SerializeField] private string destinationPointTag = "";
        [SerializeField] private bool useManualDestination = false;
        [SerializeField] private Vector2 manualDestination = Vector2.zero;
        [Tooltip("If true, manual destination is relative to this teleporter's position. If false, it's a global world position.")]
        [SerializeField] private bool manualDestinationIsRelative = false;
        [SerializeField] private bool resetPlayerVelocity = true;
        [SerializeField] private bool useFadeTransition = false;

        [Header("Global Flag Requirement")]
        [SerializeField] private bool requireGlobalFlag = false;

        public enum FlagEvaluationLogic
        {
            AllMustPass,  // AND logic - all flags must meet their requirements
            AnyCanPass    // OR logic - at least one flag must meet its requirement
        }

        [Tooltip("How to evaluate multiple flags: All Must Pass (AND) or Any Can Pass (OR)")]
        [SerializeField] private FlagEvaluationLogic flagEvaluationLogic = FlagEvaluationLogic.AllMustPass;

        [Tooltip("List of global flag requirements that must succeed before teleporting")]
        [SerializeField] private List<FlagRequirement> flagRequirements = new List<FlagRequirement>();

        [Tooltip("Dialogue ID to play when flag requirements are not met")]
        [SerializeField] private string blockedDialogueID;

        private DialogueController dialogueController;
        private SaveManager saveManager;

        protected override void Awake()
        {
            base.Awake();

            saveManager = SaveManager.Instance;

            if (!string.IsNullOrEmpty(blockedDialogueID))
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
                if (dialogueController == null)
                {
                    Debug.LogWarning("InteractableTeleporter requires a DialogueController to play blocked dialogue, but none was found in the scene.");
                }
            }
        }

        private void OnValidate()
        {
            if (requireGlobalFlag && flagRequirements.Count == 0)
            {
                flagRequirements.Add(new FlagRequirement());
            }
        }

        protected override bool CanInteract()
        {
            bool baseCanInteract = base.CanInteract();

            if (!string.IsNullOrEmpty(blockedDialogueID) && dialogueController != null)
            {
                return baseCanInteract && !dialogueController.IsDialogueActive();
            }

            return baseCanInteract;
        }

        protected override void PerformInteraction()
        {
            EnsureDialogueController();
            EnsureSaveManager();

            Vector3? destination = ResolveDestination();
            if (!destination.HasValue)
            {
                Debug.LogWarning($"InteractableTeleporter on {name} has no valid destination configured.", this);
                return;
            }

            if (requireGlobalFlag && !CheckFlagConditions())
            {
                HandleBlockedTeleport();
                return;
            }

            if (useFadeTransition)
            {
                StartCoroutine(TeleportWithFade(destination.Value));
            }
            else
            {
                TeleportPlayer(destination.Value);
            }
        }

        private IEnumerator TeleportWithFade(Vector3 destination)
        {
            bool fadeComplete = false;
            
            // Start fade transition
            SceneTransitionManager.FadeTransition(
                onFadeOutComplete: () =>
                {
                    // Teleport player during black screen
                    TeleportPlayer(destination);
                },
                onComplete: () =>
                {
                    fadeComplete = true;
                }
            );
            
            // Wait for fade transition to complete
            yield return new WaitUntil(() => fadeComplete);
        }

        private void TeleportPlayer(Vector3 destination)
        {
            GameObject targetPlayer = player != null ? player : FindPlayer();
            if (targetPlayer == null)
            {
                Debug.LogWarning("InteractableTeleporter could not find a player to teleport.");
                return;
            }

            Transform playerTransform = targetPlayer.transform;
            float currentZ = playerTransform.position.z;
            destination.z = currentZ;

            var rigidbody2D = targetPlayer.GetComponent<Rigidbody2D>();
            if (rigidbody2D != null)
            {
                if (resetPlayerVelocity)
                {
                    rigidbody2D.linearVelocity = Vector2.zero;
                }

                rigidbody2D.position = new Vector2(destination.x, destination.y);
            }
            else
            {
                playerTransform.position = destination;
            }
        }

        private GameObject FindPlayer()
        {
            var controller = FindFirstObjectByType<Unbound.Player.PlayerController2D>();
            if (controller != null)
            {
                return controller.gameObject;
            }

            return GameObject.FindGameObjectWithTag("Player");
        }

        private Vector3? ResolveDestination()
        {
            if (destinationPoint != null)
            {
                return destinationPoint.position;
            }

            if (!string.IsNullOrEmpty(destinationPointTag))
            {
                var taggedObject = GameObject.FindGameObjectWithTag(destinationPointTag);
                if (taggedObject != null)
                {
                    destinationPoint = taggedObject.transform;
                    return destinationPoint.position;
                }
            }

            if (useManualDestination)
            {
                Vector3 destPos = new Vector3(manualDestination.x, manualDestination.y, transform.position.z);
                
                // If relative, add to teleporter's position
                if (manualDestinationIsRelative)
                {
                    destPos = transform.position + destPos;
                }
                
                return destPos;
            }

            return null;
        }

        private void EnsureDialogueController()
        {
            if (dialogueController == null && !string.IsNullOrEmpty(blockedDialogueID))
            {
                dialogueController = FindFirstObjectByType<DialogueController>();

                if (dialogueController == null)
                {
                    Debug.LogWarning("InteractableTeleporter needs a DialogueController to play the blocked dialogue, but none were found.");
                }
            }
        }

        private void EnsureSaveManager()
        {
            if (saveManager == null)
            {
                saveManager = SaveManager.Instance;
            }
        }

        private bool CheckFlagConditions()
        {
            if (flagRequirements.Count == 0 || saveManager == null)
            {
                if (saveManager == null)
                {
                    Debug.LogWarning("Cannot check global flags: SaveManager.Instance is null");
                }
                return false;
            }

            var validRequirements = new List<FlagRequirement>();
            foreach (var requirement in flagRequirements)
            {
                if (!string.IsNullOrEmpty(requirement.flagName))
                {
                    validRequirements.Add(requirement);
                }
            }

            if (validRequirements.Count == 0)
            {
                return false;
            }

            if (flagEvaluationLogic == FlagEvaluationLogic.AllMustPass)
            {
                foreach (var requirement in validRequirements)
                {
                    if (!saveManager.EvaluateGlobalFlag(requirement.flagName, requirement.requiredValue))
                    {
                        return false;
                    }
                }
                return true;
            }

            foreach (var requirement in validRequirements)
            {
                if (saveManager.EvaluateGlobalFlag(requirement.flagName, requirement.requiredValue))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleBlockedTeleport()
        {
            if (!string.IsNullOrEmpty(blockedDialogueID))
            {
                if (dialogueController != null)
                {
                    dialogueController.StartDialogue(blockedDialogueID);
                }
                else
                {
                    Debug.LogError("Cannot play blocked dialogue: DialogueController not found in scene.");
                }
                return;
            }

            var flagNames = new List<string>();
            foreach (var requirement in flagRequirements)
            {
                if (!string.IsNullOrEmpty(requirement.flagName))
                {
                    flagNames.Add(requirement.flagName);
                }
            }

            string flagList = string.Join(", ", flagNames);
            Debug.LogWarning($"Teleport blocked: flag requirements not met ({flagList}).");
        }

        public void SetDestination(Transform newDestination)
        {
            destinationPoint = newDestination;
            useManualDestination = false;
        }

        public void SetDestination(Vector2 position)
        {
            manualDestination = position;
            useManualDestination = true;
        }

        public void SetDestinationTag(string tag)
        {
            destinationPointTag = tag;
        }

        public void ClearDestination()
        {
            destinationPoint = null;
            destinationPointTag = "";
            useManualDestination = false;
        }

        public void SetGlobalFlagRequirement(string flagName, bool requiredValue, string blockedDialogueID = null)
        {
            requireGlobalFlag = true;
            flagRequirements.Clear();
            flagRequirements.Add(new FlagRequirement { flagName = flagName, requiredValue = requiredValue });
            flagEvaluationLogic = FlagEvaluationLogic.AllMustPass;
            this.blockedDialogueID = blockedDialogueID;
        }

        [System.Obsolete("Use SetGlobalFlagRequirement(string, bool, string) instead. This method will be removed in a future version.")]
        public void SetGlobalFlagRequirement(string flagName, bool requiredValue, DialogueAsset blockedDialogue)
        {
            SetGlobalFlagRequirement(flagName, requiredValue, blockedDialogue?.dialogueID);
        }

        public void AddFlagRequirement(string flagName, bool requiredValue)
        {
            requireGlobalFlag = true;
            flagRequirements.Add(new FlagRequirement { flagName = flagName, requiredValue = requiredValue });
        }

        public void RemoveFlagRequirement(string flagName)
        {
            flagRequirements.RemoveAll(r => r.flagName == flagName);
            if (flagRequirements.Count == 0)
            {
                requireGlobalFlag = false;
            }
        }

        public void RemoveAllFlagRequirements()
        {
            requireGlobalFlag = false;
            flagRequirements.Clear();
        }

        public void SetFlagEvaluationLogic(FlagEvaluationLogic logic)
        {
            flagEvaluationLogic = logic;
        }

        public bool CanTeleport()
        {
            if (!requireGlobalFlag || flagRequirements.Count == 0)
            {
                return true;
            }

            EnsureSaveManager();
            if (saveManager == null)
            {
                return false;
            }

            return CheckFlagConditions();
        }

        public List<FlagRequirement> GetFlagRequirements()
        {
            return new List<FlagRequirement>(flagRequirements);
        }

        public FlagEvaluationLogic GetFlagEvaluationLogic()
        {
            return flagEvaluationLogic;
        }

        /// <summary>
        /// Draws gizmos in the editor to visualize the teleport destination
        /// </summary>
        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();

            // Visualize manual destination if enabled
            if (useManualDestination)
            {
                Vector3 destPos = new Vector3(manualDestination.x, manualDestination.y, transform.position.z);
                
                // If relative, add to teleporter's position
                if (manualDestinationIsRelative)
                {
                    destPos = transform.position + destPos;
                }
                
                // Draw destination point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(destPos, 0.5f);
                
                // Draw line from teleporter to destination
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, destPos);
                
                // Draw arrow pointing to destination
                Vector3 direction = (destPos - transform.position).normalized;
                if (direction.magnitude > 0.1f)
                {
                    Vector3 arrowHead = destPos - direction * 0.3f;
                    Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f) * 0.2f;
                    Gizmos.DrawLine(destPos, arrowHead + perpendicular);
                    Gizmos.DrawLine(destPos, arrowHead - perpendicular);
                }
            }
        }
    }
}

