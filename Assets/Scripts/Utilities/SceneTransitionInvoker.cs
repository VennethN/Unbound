using UnityEngine;

namespace Unbound.Utilities
{
    /// <summary>
    /// Small helper you can place on any GameObject to call scene transitions from UnityEvents.
    /// This avoids wiring directly to the singleton and gives button-friendly, no-arg methods.
    /// </summary>
    public class SceneTransitionInvoker : MonoBehaviour
    {
        [Header("Targets (optional)")]
        [SerializeField] private string sceneName;
        [SerializeField] private int sceneIndex = -1;

        public void LoadSceneByName()
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"{nameof(SceneTransitionInvoker)}: sceneName is empty.");
                return;
            }
            SceneTransitionManager.LoadScene(sceneName);
        }

        public void LoadSceneByIndex()
        {
            if (sceneIndex < 0)
            {
                Debug.LogWarning($"{nameof(SceneTransitionInvoker)}: sceneIndex not set.");
                return;
            }
            SceneTransitionManager.LoadScene(sceneIndex);
        }

        public void LoadNextScene() => SceneTransitionManager.LoadNextScene();
        public void LoadPreviousScene() => SceneTransitionManager.LoadPreviousScene();
        public void ReloadCurrentScene() => SceneTransitionManager.ReloadCurrentScene();
        public void LoadMainMenu() => SceneTransitionManager.LoadMainMenu();
    }
}
