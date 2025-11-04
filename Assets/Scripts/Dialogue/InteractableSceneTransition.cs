using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unbound.Utilities;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Represents a single flag condition requirement
    /// </summary>
    [System.Serializable]
    public class FlagRequirement
    {
        [Tooltip("Name of the global flag to check")]
        public string flagName = "";

        [Tooltip("Required value for this flag (true/false)")]
        public bool requiredValue = true;
    }

    /// <summary>
    /// Component that transports the player to another scene when they interact with this object
    /// </summary>
    public class InteractableSceneTransition : BaseInteractable
    {
        public enum TransitionType
        {
            ByName,
            ByBuildIndex,
            NextScene,
            PreviousScene,
            MainMenu
        }

        [Header("Scene Transition Settings")]
        [SerializeField] private TransitionType transitionType = TransitionType.ByName;
        [SerializeField] private string sceneName;
        [SerializeField] private int sceneBuildIndex = 0;

        [Header("Player Positioning")]
        [SerializeField] private bool setPlayerPosition = false;
        [SerializeField] private Vector2 playerSpawnPosition = Vector2.zero;
        [SerializeField] private string playerSpawnPointTag = "";

        [Header("Global Flag Requirement")]
        [SerializeField] private bool requireGlobalFlag = false;

        public enum FlagEvaluationLogic
        {
            AllMustPass,  // AND logic - all flags must meet their requirements
            AnyCanPass    // OR logic - at least one flag must meet its requirement
        }

        [Tooltip("How to evaluate multiple flags: All Must Pass (AND) or Any Can Pass (OR)")]
        [SerializeField] private FlagEvaluationLogic flagEvaluationLogic = FlagEvaluationLogic.AllMustPass;

        [Tooltip("List of flag requirements that must be checked")]
        [SerializeField] private List<FlagRequirement> flagRequirements = new List<FlagRequirement>();

        [Tooltip("Dialogue to play when flag requirements are not met")]
        [SerializeField] private DialogueAsset blockedDialogueAsset;

        // Runtime state
        private DialogueController dialogueController;

        protected override void Awake()
        {
            base.Awake();

            // Find DialogueController if blocked dialogue is set
            if (blockedDialogueAsset != null)
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
                if (dialogueController == null)
                {
                    Debug.LogWarning("InteractableSceneTransition requires DialogueController when blocked dialogue is set, but none found in scene.");
                }
            }
        }

        private void OnValidate()
        {
            // Ensure at least one flag requirement exists if flag requirement is enabled
            if (requireGlobalFlag && flagRequirements.Count == 0)
            {
                flagRequirements.Add(new FlagRequirement());
            }
        }

        /// <summary>
        /// Override to check if dialogue is not currently active
        /// </summary>
        protected override bool CanInteract()
        {
            bool baseCanInteract = base.CanInteract();

            // If blocked dialogue is set, make sure dialogue controller exists and dialogue isn't active
            if (blockedDialogueAsset != null && dialogueController != null)
            {
                return baseCanInteract && !dialogueController.IsDialogueActive();
            }

            return baseCanInteract;
        }

        /// <summary>
        /// Ensures DialogueController is found if needed
        /// </summary>
        private void EnsureDialogueController()
        {
            if (dialogueController == null && blockedDialogueAsset != null)
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
            }
        }

        /// <summary>
        /// Checks if all required global flag conditions are met
        /// </summary>
        private bool CheckFlagConditions()
        {
            if (flagRequirements.Count == 0)
            {
                return false;
            }

            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogWarning("Cannot check global flags: SaveManager.Instance is null");
                return false;
            }

            // Filter out empty flag requirements
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

            // Evaluate based on logic type
            if (flagEvaluationLogic == FlagEvaluationLogic.AllMustPass)
            {
                // AND logic - all flags must pass
                foreach (var requirement in validRequirements)
                {
                    if (!saveManager.EvaluateGlobalFlag(requirement.flagName, requirement.requiredValue))
                    {
                        return false;
                    }
                }
                return true;
            }
            else // FlagEvaluationLogic.AnyCanPass
            {
                // OR logic - at least one flag must pass
                foreach (var requirement in validRequirements)
                {
                    if (saveManager.EvaluateGlobalFlag(requirement.flagName, requirement.requiredValue))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        protected override void PerformInteraction()
        {
            // Ensure DialogueController is found if needed
            EnsureDialogueController();

            // Check if flag requirements are met
            bool canTransition = true;
            if (requireGlobalFlag)
            {
                canTransition = CheckFlagConditions();
            }

            // If flag requirements are not met, play dialogue instead of transitioning
            if (!canTransition && blockedDialogueAsset != null)
            {
                if (dialogueController != null)
                {
                    dialogueController.StartDialogue(blockedDialogueAsset);
                }
                else
                {
                    Debug.LogError("Cannot play blocked dialogue: DialogueController not found in scene.");
                }
                return;
            }

            // If flag requirements are not met and no dialogue provided, just return
            if (!canTransition)
            {
                var flagNames = new List<string>();
                foreach (var req in flagRequirements)
                {
                    if (!string.IsNullOrEmpty(req.flagName))
                    {
                        flagNames.Add(req.flagName);
                    }
                }
                string flagList = string.Join(", ", flagNames);
                Debug.LogWarning($"Scene transition blocked: flag requirements not met ({flagList}) and no dialogue provided.");
                return;
            }

            // Handle player positioning before transition if needed
            if (setPlayerPosition)
            {
                SetupPlayerPosition();
            }

            // Perform the scene transition
            switch (transitionType)
            {
                case TransitionType.ByName:
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        SceneTransitionManager.LoadScene(sceneName);
                    }
                    else
                    {
                        Debug.LogWarning("Scene name is empty. Cannot transition.");
                    }
                    break;

                case TransitionType.ByBuildIndex:
                    SceneTransitionManager.LoadScene(sceneBuildIndex);
                    break;

                case TransitionType.NextScene:
                    SceneTransitionManager.LoadNextScene();
                    break;

                case TransitionType.PreviousScene:
                    SceneTransitionManager.LoadPreviousScene();
                    break;

                case TransitionType.MainMenu:
                    SceneTransitionManager.LoadMainMenu();
                    break;

                default:
                    Debug.LogWarning($"Unknown transition type: {transitionType}");
                    break;
            }
        }

        private void SetupPlayerPosition()
        {
            var player = FindFirstObjectByType<Unbound.Player.PlayerController2D>()?.gameObject ??
                        GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                if (!string.IsNullOrEmpty(playerSpawnPointTag))
                {
                    // Try to find a spawn point by tag
                    var spawnPoint = GameObject.FindGameObjectWithTag(playerSpawnPointTag);
                    if (spawnPoint != null)
                    {
                        player.transform.position = spawnPoint.transform.position;
                        return;
                    }
                }

                // Use the specified position
                player.transform.position = playerSpawnPosition;
            }
            else
            {
                Debug.LogWarning("No player found to position for scene transition.");
            }
        }

        /// <summary>
        /// Sets the target scene by name
        /// </summary>
        public void SetTargetScene(string newSceneName)
        {
            sceneName = newSceneName;
            transitionType = TransitionType.ByName;
        }

        /// <summary>
        /// Sets the target scene by build index
        /// </summary>
        public void SetTargetScene(int buildIndex)
        {
            sceneBuildIndex = buildIndex;
            transitionType = TransitionType.ByBuildIndex;
        }

        /// <summary>
        /// Sets the transition to go to the next scene in build order
        /// </summary>
        public void SetNextScene()
        {
            transitionType = TransitionType.NextScene;
        }

        /// <summary>
        /// Sets the transition to go to the previous scene in build order
        /// </summary>
        public void SetPreviousScene()
        {
            transitionType = TransitionType.PreviousScene;
        }

        /// <summary>
        /// Sets the transition to go to the main menu
        /// </summary>
        public void SetMainMenu()
        {
            transitionType = TransitionType.MainMenu;
        }

        /// <summary>
        /// Sets the player spawn position for the next scene
        /// </summary>
        public void SetPlayerSpawnPosition(Vector2 position)
        {
            playerSpawnPosition = position;
            setPlayerPosition = true;
        }

        /// <summary>
        /// Sets the player spawn point by tag for the next scene
        /// </summary>
        public void SetPlayerSpawnPoint(string spawnPointTag)
        {
            playerSpawnPointTag = spawnPointTag;
            setPlayerPosition = true;
        }

        /// <summary>
        /// Disables player positioning for the transition
        /// </summary>
        public void DisablePlayerPositioning()
        {
            setPlayerPosition = false;
        }

        /// <summary>
        /// Gets the current target scene name
        /// </summary>
        public string GetTargetSceneName()
        {
            return sceneName;
        }

        /// <summary>
        /// Gets the current target scene build index
        /// </summary>
        public int GetTargetSceneIndex()
        {
            return sceneBuildIndex;
        }

        /// <summary>
        /// Gets the current transition type
        /// </summary>
        public TransitionType GetTransitionType()
        {
            return transitionType;
        }

        /// <summary>
        /// Sets a single dialogue flag requirement for this transition (backward compatibility)
        /// </summary>
        public void SetGlobalFlagRequirement(string flagName, bool requiredValue, DialogueAsset blockedDialogue = null)
        {
            requireGlobalFlag = true;
            flagRequirements.Clear();
            flagRequirements.Add(new FlagRequirement { flagName = flagName, requiredValue = requiredValue });
            flagEvaluationLogic = FlagEvaluationLogic.AllMustPass;
            blockedDialogueAsset = blockedDialogue;
        }

        /// <summary>
        /// Adds a global flag requirement to the list
        /// </summary>
        public void AddFlagRequirement(string flagName, bool requiredValue)
        {
            requireGlobalFlag = true;
            flagRequirements.Add(new FlagRequirement { flagName = flagName, requiredValue = requiredValue });
        }

        /// <summary>
        /// Removes a specific flag requirement by name
        /// </summary>
        public void RemoveFlagRequirement(string flagName)
        {
            flagRequirements.RemoveAll(r => r.flagName == flagName);
            if (flagRequirements.Count == 0)
            {
                requireGlobalFlag = false;
            }
        }

        /// <summary>
        /// Removes all global flag requirements
        /// </summary>
        public void RemoveAllFlagRequirements()
        {
            requireGlobalFlag = false;
            flagRequirements.Clear();
        }

        /// <summary>
        /// Sets the evaluation logic for multiple flags
        /// </summary>
        public void SetFlagEvaluationLogic(FlagEvaluationLogic logic)
        {
            flagEvaluationLogic = logic;
        }

        /// <summary>
        /// Sets the dialogue to play when the transition is blocked
        /// </summary>
        public void SetBlockedDialogue(DialogueAsset dialogue)
        {
            blockedDialogueAsset = dialogue;
        }

        /// <summary>
        /// Gets whether the transition can currently proceed (flag conditions are met)
        /// </summary>
        public bool CanTransition()
        {
            if (!requireGlobalFlag || flagRequirements.Count == 0)
            {
                return true;
            }

            return CheckFlagConditions();
        }

        /// <summary>
        /// Gets the list of flag requirements
        /// </summary>
        public List<FlagRequirement> GetFlagRequirements()
        {
            return new List<FlagRequirement>(flagRequirements);
        }

        /// <summary>
        /// Gets the current flag evaluation logic
        /// </summary>
        public FlagEvaluationLogic GetFlagEvaluationLogic()
        {
            return flagEvaluationLogic;
        }
    }
}
