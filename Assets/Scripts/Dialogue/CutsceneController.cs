using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Dialogue
{
    /// <summary>
    /// High-level controller for managing cutscenes and their integration with the dialogue system
    /// Provides the main API for playing cutscenes and handles system integration
    /// </summary>
    [RequireComponent(typeof(CutscenePlayer))]
    public class CutsceneController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CutscenePlayer cutscenePlayer;
        [SerializeField] private DialogueController dialogueController;

        [Header("Cutscene Management")]
        [SerializeField] private List<CutsceneAsset> availableCutscenes = new List<CutsceneAsset>();
        [SerializeField] private string defaultCutsceneID;

        [Header("Gameplay Integration")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private List<GameObject> uiElementsToHide = new List<GameObject>();

        [Header("Audio Management")]
        [SerializeField] private AudioSource backgroundMusicSource;
        [SerializeField] private AudioSource voiceOverSource;
        [SerializeField] private AudioSource soundEffectSource;

        // State management
        private CutsceneAsset currentCutscene;
        private bool gameplayPaused = false;
        private bool uiHidden = false;
        private AudioSource currentBackgroundMusic;

        // Events
        public UnityEvent<CutsceneAsset> OnCutsceneStarted;
        public UnityEvent<CutsceneAsset> OnCutsceneCompleted;
        public UnityEvent<CutsceneStep> OnStepStarted;
        public UnityEvent<CutsceneStep> OnStepCompleted;
        public UnityEvent<float> OnCutsceneProgress;

        private void Awake()
        {
            if (cutscenePlayer == null)
            {
                cutscenePlayer = GetComponent<CutscenePlayer>();
            }

            if (dialogueController == null)
            {
                dialogueController = FindFirstObjectByType<DialogueController>();
            }

            // Setup event forwarding
            if (cutscenePlayer != null)
            {
                cutscenePlayer.OnCutsceneStarted.AddListener(ForwardCutsceneStarted);
                cutscenePlayer.OnCutsceneCompleted.AddListener(ForwardCutsceneCompleted);
                cutscenePlayer.OnStepStarted.AddListener(ForwardStepStarted);
                cutscenePlayer.OnStepCompleted.AddListener(ForwardStepCompleted);
                cutscenePlayer.OnCutsceneProgress.AddListener(ForwardCutsceneProgress);
            }
        }

        private void OnDestroy()
        {
            // Cleanup event listeners
            if (cutscenePlayer != null)
            {
                cutscenePlayer.OnCutsceneStarted.RemoveListener(ForwardCutsceneStarted);
                cutscenePlayer.OnCutsceneCompleted.RemoveListener(ForwardCutsceneCompleted);
                cutscenePlayer.OnStepStarted.RemoveListener(ForwardStepStarted);
                cutscenePlayer.OnStepCompleted.RemoveListener(ForwardStepCompleted);
                cutscenePlayer.OnCutsceneProgress.RemoveListener(ForwardCutsceneProgress);
            }
        }

        #region Public API

        /// <summary>
        /// Plays a cutscene by reference
        /// </summary>
        public void PlayCutscene(CutsceneAsset cutscene)
        {
            if (cutscene == null)
            {
                Debug.LogWarning("Cannot play null cutscene");
                return;
            }

            currentCutscene = cutscene;
            cutscenePlayer.PlayCutscene(cutscene);
        }

        /// <summary>
        /// Plays a cutscene by ID from the available cutscenes list
        /// </summary>
        public void PlayCutsceneByID(string cutsceneID)
        {
            CutsceneAsset cutscene = availableCutscenes.Find(c => c.cutsceneID == cutsceneID);
            if (cutscene == null)
            {
                Debug.LogWarning($"Cutscene with ID '{cutsceneID}' not found");
                return;
            }

            PlayCutscene(cutscene);
        }

        /// <summary>
        /// Plays the default cutscene if one is set
        /// </summary>
        public void PlayDefaultCutscene()
        {
            if (!string.IsNullOrEmpty(defaultCutsceneID))
            {
                PlayCutsceneByID(defaultCutsceneID);
            }
            else
            {
                Debug.LogWarning("No default cutscene ID set");
            }
        }

        /// <summary>
        /// Stops the currently playing cutscene
        /// </summary>
        public void StopCurrentCutscene()
        {
            cutscenePlayer.StopCutscene();
            currentCutscene = null;
        }

        /// <summary>
        /// Pauses the currently playing cutscene
        /// </summary>
        public void PauseCurrentCutscene()
        {
            cutscenePlayer.PauseCutscene();
        }

        /// <summary>
        /// Resumes the currently playing cutscene
        /// </summary>
        public void ResumeCurrentCutscene()
        {
            cutscenePlayer.ResumeCutscene();
        }

        /// <summary>
        /// Skips to a specific step in the current cutscene
        /// </summary>
        public void SkipToStep(int stepIndex)
        {
            cutscenePlayer.SkipToStep(stepIndex);
        }

        /// <summary>
        /// Gets the currently playing cutscene
        /// </summary>
        public CutsceneAsset GetCurrentCutscene()
        {
            return currentCutscene ?? cutscenePlayer.GetCurrentCutscene();
        }

        /// <summary>
        /// Checks if a cutscene is currently playing
        /// </summary>
        public bool IsCutscenePlaying()
        {
            return cutscenePlayer.IsCutscenePlaying();
        }

        /// <summary>
        /// Gets the current cutscene progress (0-1)
        /// </summary>
        public float GetCutsceneProgress()
        {
            return cutscenePlayer.GetCutsceneProgress();
        }

        /// <summary>
        /// Gets the current step progress (0-1)
        /// </summary>
        public float GetCurrentStepProgress()
        {
            return cutscenePlayer.GetCurrentStepProgress();
        }

        /// <summary>
        /// Gets the current step index
        /// </summary>
        public int GetCurrentStepIndex()
        {
            return cutscenePlayer.GetCurrentStepIndex();
        }

        /// <summary>
        /// Gets the current step
        /// </summary>
        public CutsceneStep GetCurrentStep()
        {
            return cutscenePlayer.GetCurrentStep();
        }

        /// <summary>
        /// Adds a cutscene to the available cutscenes list
        /// </summary>
        public void AddCutscene(CutsceneAsset cutscene)
        {
            if (cutscene != null && !availableCutscenes.Contains(cutscene))
            {
                availableCutscenes.Add(cutscene);
            }
        }

        /// <summary>
        /// Removes a cutscene from the available cutscenes list
        /// </summary>
        public void RemoveCutscene(CutsceneAsset cutscene)
        {
            availableCutscenes.Remove(cutscene);
        }

        #endregion

        #region Dialogue Integration

        /// <summary>
        /// Starts a dialogue sequence
        /// </summary>
        public void StartDialogue(DialogueAsset dialogueAsset)
        {
            if (dialogueController != null)
            {
                dialogueController.StartDialogue(dialogueAsset);
            }
            else
            {
                Debug.LogWarning("No DialogueController found for starting dialogue");
            }
        }

        /// <summary>
        /// Starts a dialogue by ID
        /// </summary>
        public void StartDialogueByID(string dialogueID)
        {
            if (dialogueController != null)
            {
                // This would require integrating with your dialogue system's asset management
                // to find the DialogueAsset by ID. For now, we'll use a placeholder.
                Debug.Log($"Starting dialogue by ID: {dialogueID}");
                // TODO: Implement dialogue asset lookup by ID
                // Example: DialogueAsset asset = DialogueManager.GetAssetByID(dialogueID);
                // dialogueController.StartDialogue(asset);
            }
            else
            {
                Debug.LogWarning("No DialogueController found for starting dialogue by ID");
            }
        }

        /// <summary>
        /// Checks if dialogue is currently active
        /// </summary>
        public bool IsDialogueActive()
        {
            return dialogueController != null && dialogueController.IsDialogueActive();
        }

        #endregion

        #region Gameplay Control

        /// <summary>
        /// Pauses gameplay during cutscene
        /// </summary>
        public void PauseGameplay()
        {
            if (gameplayPaused) return;

            // Disable player controls
            if (playerObject != null)
            {
                // Disable player movement/input scripts
                var playerController = playerObject.GetComponent<MonoBehaviour>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }
            }

            // Pause time if needed
            // Time.timeScale = 0f; // Uncomment if you want to pause time completely

            gameplayPaused = true;
        }

        /// <summary>
        /// Resumes gameplay after cutscene
        /// </summary>
        public void ResumeGameplay()
        {
            if (!gameplayPaused) return;

            // Re-enable player controls
            if (playerObject != null)
            {
                var playerController = playerObject.GetComponent<MonoBehaviour>();
                if (playerController != null)
                {
                    playerController.enabled = true;
                }
            }

            // Resume time if it was paused
            // Time.timeScale = 1f; // Uncomment if you paused time

            gameplayPaused = false;
        }

        /// <summary>
        /// Hides UI elements during cutscene
        /// </summary>
        public void HideUI()
        {
            if (uiHidden) return;

            foreach (var uiElement in uiElementsToHide)
            {
                if (uiElement != null)
                {
                    uiElement.SetActive(false);
                }
            }

            uiHidden = true;
        }

        /// <summary>
        /// Shows UI elements after cutscene
        /// </summary>
        public void ShowUI()
        {
            if (!uiHidden) return;

            foreach (var uiElement in uiElementsToHide)
            {
                if (uiElement != null)
                {
                    uiElement.SetActive(true);
                }
            }

            uiHidden = false;
        }

        /// <summary>
        /// Enables player control after cutscene
        /// </summary>
        public void EnablePlayerControl()
        {
            ResumeGameplay();
            ShowUI();
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Loads a scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("Cannot load scene with null or empty name");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        #endregion

        #region Audio Management

        /// <summary>
        /// Plays background music for the cutscene
        /// </summary>
        public void PlayBackgroundMusic(AudioClip music, float volume = 1f, bool loop = true)
        {
            if (music == null) return;

            if (backgroundMusicSource != null)
            {
                backgroundMusicSource.clip = music;
                backgroundMusicSource.volume = volume;
                backgroundMusicSource.loop = loop;
                backgroundMusicSource.Play();
                currentBackgroundMusic = backgroundMusicSource;
            }
        }

        /// <summary>
        /// Stops background music
        /// </summary>
        public void StopBackgroundMusic()
        {
            if (currentBackgroundMusic != null && currentBackgroundMusic.isPlaying)
            {
                currentBackgroundMusic.Stop();
                currentBackgroundMusic = null;
            }
        }

        /// <summary>
        /// Plays voice over audio
        /// </summary>
        public void PlayVoiceOver(AudioClip voiceOver, float volume = 1f)
        {
            if (voiceOver == null) return;

            if (voiceOverSource != null)
            {
                voiceOverSource.clip = voiceOver;
                voiceOverSource.volume = volume;
                voiceOverSource.Play();
            }
        }

        /// <summary>
        /// Plays a sound effect
        /// </summary>
        public void PlaySoundEffect(AudioClip soundEffect, float volume = 1f)
        {
            if (soundEffect == null) return;

            if (soundEffectSource != null)
            {
                soundEffectSource.PlayOneShot(soundEffect, volume);
            }
        }

        #endregion

        #region Custom Actions

        /// <summary>
        /// Executes custom completion actions
        /// Override this method to add custom behavior when cutscenes complete
        /// </summary>
        public virtual void ExecuteCustomCompletionAction()
        {
            // Override this in derived classes for custom completion behavior
            Debug.Log("Executing custom cutscene completion action");
        }

        #endregion

        #region Event Forwarding

        private void ForwardCutsceneStarted(CutsceneAsset cutscene)
        {
            OnCutsceneStarted?.Invoke(cutscene);
        }

        private void ForwardCutsceneCompleted(CutsceneAsset cutscene)
        {
            OnCutsceneCompleted?.Invoke(cutscene);
            currentCutscene = null;
        }

        private void ForwardStepStarted(CutsceneStep step)
        {
            OnStepStarted?.Invoke(step);
        }

        private void ForwardStepCompleted(CutsceneStep step)
        {
            OnStepCompleted?.Invoke(step);
        }

        private void ForwardCutsceneProgress(float progress)
        {
            OnCutsceneProgress?.Invoke(progress);
        }

        #endregion

        #region Editor Support

        /// <summary>
        /// Validates all cutscenes in the available list
        /// </summary>
        public List<string> ValidateAllCutscenes()
        {
            var validationErrors = new List<string>();

            foreach (var cutscene in availableCutscenes)
            {
                if (cutscene != null)
                {
                    string error = cutscene.GetValidationErrors();
                    if (!string.IsNullOrEmpty(error))
                    {
                        validationErrors.Add($"{cutscene.name}: {error}");
                    }
                }
                else
                {
                    validationErrors.Add("Null cutscene found in list");
                }
            }

            return validationErrors;
        }

        #endregion
    }
}
