using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unbound.Utilities
{
    /// <summary>
    /// Utility class for handling scene transitions with smooth fade effects.
    /// Can be used directly from buttons via UnityEvents or through static methods.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Transition Settings")]
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private bool autoSaveOnTransition = true;
        [SerializeField] private float blackScreenHoldDuration = 0.2f;

        [Header("Transition UI")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Events")]
        public UnityEvent<string> OnTransitionStart;
        public UnityEvent<string> OnTransitionComplete;
        public UnityEvent OnTransitionBegin;
        public UnityEvent OnTransitionEnd;

        // Singleton instance for easy access
        private static SceneTransitionManager instance;

        private void Awake()
        {
            // Ensure only one instance exists
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);

                // Create transition UI if not assigned
                if (fadeImage == null)
                {
                    CreateTransitionUI();
                }
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Ensure fade image starts transparent
            if (fadeImage != null)
            {
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
                fadeImage.gameObject.SetActive(false);
            }

            // Disable canvas interaction when not transitioning
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// Loads a scene by name with transition effect
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            if (instance == null)
            {
                Debug.LogWarning("SceneTransitionManager instance not found. Loading scene without transition.");
                SceneManager.LoadScene(sceneName);
                return;
            }

            instance.StartCoroutine(instance.TransitionToScene(sceneName));
        }

        /// <summary>
        /// Loads a scene by build index with transition effect
        /// </summary>
        public static void LoadScene(int sceneIndex)
        {
            if (instance == null)
            {
                Debug.LogWarning("SceneTransitionManager instance not found. Loading scene without transition.");
                SceneManager.LoadScene(sceneIndex);
                return;
            }

            string sceneName = SceneManager.GetSceneByBuildIndex(sceneIndex).name;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"Invalid scene index: {sceneIndex}");
                return;
            }

            instance.StartCoroutine(instance.TransitionToScene(sceneName));
        }

        /// <summary>
        /// Loads the next scene in build order with transition
        /// </summary>
        public static void LoadNextScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int nextIndex = currentIndex + 1;

            if (nextIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogWarning("No next scene found in build settings.");
                return;
            }

            LoadScene(nextIndex);
        }

        /// <summary>
        /// Loads the previous scene in build order with transition
        /// </summary>
        public static void LoadPreviousScene()
        {
            int currentIndex = SceneManager.GetActiveScene().buildIndex;
            int previousIndex = currentIndex - 1;

            if (previousIndex < 0)
            {
                Debug.LogWarning("No previous scene found in build settings.");
                return;
            }

            LoadScene(previousIndex);
        }

        /// <summary>
        /// Reloads the current scene with transition
        /// </summary>
        public static void ReloadCurrentScene()
        {
            string currentScene = SceneManager.GetActiveScene().name;
            LoadScene(currentScene);
        }

        /// <summary>
        /// Loads main menu scene with transition
        /// </summary>
        public static void LoadMainMenu()
        {
            LoadScene("MainMenu");
        }

        private IEnumerator TransitionToScene(string sceneName)
        {
            // Trigger start events
            OnTransitionBegin?.Invoke();
            OnTransitionStart?.Invoke(sceneName);

            // Save game if auto-save is enabled
            if (autoSaveOnTransition)
            {
                AutoSave();
            }

            // Fade out
            yield return StartCoroutine(FadeOut());

            // Hold black screen for a short duration to mask the transition
            yield return new WaitForSeconds(blackScreenHoldDuration);

            // Load the new scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Find the new SceneTransitionManager instance (if any)
            SceneTransitionManager newInstance = FindFirstObjectByType<SceneTransitionManager>();
            if (newInstance != null && newInstance != this)
            {
                instance = newInstance;
            }

            // Fade in
            yield return StartCoroutine(FadeIn());

            // Trigger complete events
            OnTransitionEnd?.Invoke();
            OnTransitionComplete?.Invoke(sceneName);
        }

        private IEnumerator FadeOut()
        {
            if (fadeImage == null) yield break;

            // Enable canvas blocking during transition
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            fadeImage.gameObject.SetActive(true);
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 1f);
        }

        private IEnumerator FadeIn()
        {
            if (fadeImage == null) yield break;

            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Clamp01(1f - (elapsed / fadeDuration));
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
                yield return null;
            }

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.gameObject.SetActive(false);

            // Disable canvas blocking after transition
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        /// <summary>
        /// Performs a fade out transition (without scene loading)
        /// Returns a coroutine that can be started by the caller
        /// </summary>
        public static Coroutine FadeOutOnly()
        {
            if (instance == null)
            {
                Debug.LogWarning("SceneTransitionManager instance not found. Cannot perform fade.");
                return null;
            }

            return instance.StartCoroutine(instance.FadeOut());
        }

        /// <summary>
        /// Performs a fade in transition (without scene loading)
        /// Returns a coroutine that can be started by the caller
        /// </summary>
        public static Coroutine FadeInOnly()
        {
            if (instance == null)
            {
                Debug.LogWarning("SceneTransitionManager instance not found. Cannot perform fade.");
                return null;
            }

            return instance.StartCoroutine(instance.FadeIn());
        }

        /// <summary>
        /// Performs a complete fade out then fade in transition (without scene loading)
        /// Useful for teleportation effects within the same scene
        /// </summary>
        public static void FadeTransition(System.Action onFadeOutComplete = null, System.Action onComplete = null)
        {
            if (instance == null)
            {
                Debug.LogWarning("SceneTransitionManager instance not found. Cannot perform fade transition.");
                onFadeOutComplete?.Invoke();
                onComplete?.Invoke();
                return;
            }

            instance.StartCoroutine(instance.FadeTransitionCoroutine(onFadeOutComplete, onComplete));
        }

        private IEnumerator FadeTransitionCoroutine(System.Action onFadeOutComplete, System.Action onComplete)
        {
            yield return StartCoroutine(FadeOut());
            onFadeOutComplete?.Invoke();
            // Hold black screen for a short duration to mask the transition
            yield return new WaitForSeconds(blackScreenHoldDuration);
            yield return StartCoroutine(FadeIn());
            onComplete?.Invoke();
        }

        private void AutoSave()
        {
            // Try to find and use the SaveManager
            SaveManager saveManager = FindFirstObjectByType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.Save(saveManager.GetCurrentSaveData());
            }
        }

        private void CreateTransitionUI()
        {
            // Create canvas if it doesn't exist
            if (transitionCanvas == null)
            {
                GameObject canvasObject = new GameObject("TransitionCanvas");
                canvasObject.transform.SetParent(transform);
                transitionCanvas = canvasObject.AddComponent<Canvas>();
                canvasObject.AddComponent<CanvasScaler>();
                canvasObject.AddComponent<GraphicRaycaster>();
                
                // Add CanvasGroup for controlling raycasting
                canvasGroup = canvasObject.AddComponent<CanvasGroup>();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;

                // Set canvas properties
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 1000; // Ensure it's on top
            }

            // Create fade image
            GameObject imageObject = new GameObject("FadeImage");
            imageObject.transform.SetParent(transitionCanvas.transform);
            fadeImage = imageObject.AddComponent<Image>();

            // Set up image properties
            RectTransform rectTransform = fadeImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false; // Prevent image from blocking raycasts
            fadeImage.gameObject.SetActive(false);
        }

        // Button-friendly methods for UnityEvents
        public void LoadSceneByName(string sceneName) => LoadScene(sceneName);
        public void LoadSceneByIndex(int sceneIndex) => LoadScene(sceneIndex);
        public void LoadNextSceneButton() => LoadNextScene();
        public void LoadPreviousSceneButton() => LoadPreviousScene();
        public void ReloadCurrentSceneButton() => ReloadCurrentScene();
        public void LoadMainMenuButton() => LoadMainMenu();

        #region Utility Methods

        /// <summary>
        /// Gets the current scene name
        /// </summary>
        public static string GetCurrentSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }

        /// <summary>
        /// Gets the current scene build index
        /// </summary>
        public static int GetCurrentSceneIndex()
        {
            return SceneManager.GetActiveScene().buildIndex;
        }

        /// <summary>
        /// Checks if a scene exists by name
        /// </summary>
        public static bool SceneExists(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (sceneNameFromPath == sceneName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the total number of scenes in build settings
        /// </summary>
        public static int GetSceneCount()
        {
            return SceneManager.sceneCountInBuildSettings;
        }

        #endregion
    }
}
