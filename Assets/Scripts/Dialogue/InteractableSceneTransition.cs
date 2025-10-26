using UnityEngine;
using UnityEngine.InputSystem;
using Unbound.Utilities;

namespace Unbound.Dialogue
{
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

        protected override void PerformInteraction()
        {
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
    }
}
