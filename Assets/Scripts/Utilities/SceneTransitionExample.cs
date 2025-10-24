using UnityEngine;
using UnityEngine.UI;

namespace Unbound.Utilities
{
    /// <summary>
    /// Example script showing how to use SceneTransitionManager with buttons and UI elements.
    /// Attach this to any GameObject in your scene or use the static methods directly.
    /// </summary>
    public class SceneTransitionExample : MonoBehaviour
    {
        [Header("Button References (Optional)")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button nextSceneButton;
        [SerializeField] private Button previousSceneButton;
        [SerializeField] private Button reloadButton;

        private void Start()
        {
            // Option 1: Assign button listeners in code
            SetupButtonListeners();

            // Option 2: You can also assign these in the Unity Editor via UnityEvents
            // Just drag the GameObject with this script to the button's OnClick event
            // and select the desired method from the dropdown
        }

        private void SetupButtonListeners()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(() => SceneTransitionManager.LoadScene("TestScene"));
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() => SceneTransitionManager.LoadMainMenu());
            }

            if (nextSceneButton != null)
            {
                nextSceneButton.onClick.AddListener(() => SceneTransitionManager.LoadNextScene());
            }

            if (previousSceneButton != null)
            {
                previousSceneButton.onClick.AddListener(() => SceneTransitionManager.LoadPreviousScene());
            }

            if (reloadButton != null)
            {
                reloadButton.onClick.AddListener(() => SceneTransitionManager.ReloadCurrentScene());
            }
        }

        // These methods can be called directly from UnityEvents in the Inspector
        public void LoadTestScene()
        {
            SceneTransitionManager.LoadScene("TestScene");
        }

        public void LoadMainMenuFromButton()
        {
            SceneTransitionManager.LoadMainMenu();
        }

        public void LoadNextSceneFromButton()
        {
            SceneTransitionManager.LoadNextScene();
        }

        public void LoadPreviousSceneFromButton()
        {
            SceneTransitionManager.LoadPreviousScene();
        }

        public void ReloadSceneFromButton()
        {
            SceneTransitionManager.ReloadCurrentScene();
        }

        // Example of loading by build index
        public void LoadSceneByIndex(int index)
        {
            SceneTransitionManager.LoadScene(index);
        }

        // Example of conditional scene loading
        public void LoadSceneBasedOnCondition()
        {
            string targetScene = (UnityEngine.Random.value > 0.5f) ? "TestScene" : "MainMenu";
            SceneTransitionManager.LoadScene(targetScene);
        }

        // Example of using events to trigger actions before/after transition
        public void SetupTransitionEvents()
        {
            // Find the SceneTransitionManager instance
            SceneTransitionManager transitionManager = FindFirstObjectByType<SceneTransitionManager>();

            if (transitionManager != null)
            {
                // Subscribe to events
                transitionManager.OnTransitionStart.AddListener(OnTransitionStarted);
                transitionManager.OnTransitionComplete.AddListener(OnTransitionCompleted);
                transitionManager.OnTransitionBegin.AddListener(OnTransitionBegin);
                transitionManager.OnTransitionEnd.AddListener(OnTransitionEnd);
            }
        }

        private void OnTransitionStarted(string sceneName)
        {
            Debug.Log($"Transition started to: {sceneName}");
        }

        private void OnTransitionCompleted(string sceneName)
        {
            Debug.Log($"Transition completed to: {sceneName}");
        }

        private void OnTransitionBegin()
        {
            Debug.Log("Transition begin - fade out starting");
        }

        private void OnTransitionEnd()
        {
            Debug.Log("Transition end - fade in completed");
        }

        // Example of using utility methods
        public void PrintSceneInfo()
        {
            Debug.Log($"Current Scene: {SceneTransitionManager.GetCurrentSceneName()}");
            Debug.Log($"Current Scene Index: {SceneTransitionManager.GetCurrentSceneIndex()}");
            Debug.Log($"Total Scenes: {SceneTransitionManager.GetSceneCount()}");
            Debug.Log($"MainMenu exists: {SceneTransitionManager.SceneExists("MainMenu")}");
            Debug.Log($"TestScene exists: {SceneTransitionManager.SceneExists("TestScene")}");
        }
    }
}
