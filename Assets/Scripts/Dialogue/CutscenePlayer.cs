using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Component responsible for executing cutscene actions and managing cutscene playback state
    /// </summary>
    [RequireComponent(typeof(CutsceneController))]
    public class CutscenePlayer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CutsceneController controller;
        [SerializeField] private UnityEngine.Camera mainCamera;

        [Header("Settings")]
        [SerializeField] private bool autoFindCamera = true;

        // Playback state
        private CutsceneAsset currentCutscene;
        private int currentStepIndex = 0;
        private float stepStartTime = 0f;
        private bool isPlaying = false;
        private bool isPaused = false;

        // Audio cleanup tracking
        private List<AudioCleanupData> audioCleanupQueue = new List<AudioCleanupData>();

        // Events
        public UnityEvent<CutsceneAsset> OnCutsceneStarted;
        public UnityEvent<CutsceneAsset> OnCutsceneCompleted;
        public UnityEvent<CutsceneStep> OnStepStarted;
        public UnityEvent<CutsceneStep> OnStepCompleted;
        public UnityEvent<float> OnCutsceneProgress;

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<CutsceneController>();
            }

            if (autoFindCamera && mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }
        }

        private void Update()
        {
            if (!isPlaying || isPaused || currentCutscene == null)
                return;

            UpdateCutscene();
        }

        private void LateUpdate()
        {
            if (!isPlaying || isPaused || currentCutscene == null)
                return;

            UpdateAudioCleanup();
        }

        /// <summary>
        /// Starts playing a cutscene
        /// </summary>
        public void PlayCutscene(CutsceneAsset cutscene)
        {
            if (cutscene == null || !cutscene.IsValid())
            {
                Debug.LogError($"Cannot play cutscene: {(cutscene == null ? "null" : cutscene.GetValidationErrors())}");
                return;
            }

            StopCutscene(); // Stop any currently playing cutscene

            currentCutscene = cutscene;
            currentStepIndex = 0;
            stepStartTime = Time.time;
            isPlaying = true;
            isPaused = false;

            // Setup cutscene environment
            SetupCutsceneEnvironment();

            // Start first step
            StartStep(0);

            OnCutsceneStarted?.Invoke(cutscene);
        }

        /// <summary>
        /// Stops the current cutscene
        /// </summary>
        public void StopCutscene()
        {
            if (!isPlaying) return;

            isPlaying = false;
            isPaused = false;

            // Clean up any pending audio
            CleanupAllAudio();

            // Restore environment
            RestoreCutsceneEnvironment();

            currentCutscene = null;
            currentStepIndex = 0;

            OnCutsceneCompleted?.Invoke(currentCutscene);
        }

        /// <summary>
        /// Pauses the current cutscene
        /// </summary>
        public void PauseCutscene()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resumes the current cutscene
        /// </summary>
        public void ResumeCutscene()
        {
            isPaused = false;
        }

        /// <summary>
        /// Skips to a specific step in the cutscene
        /// </summary>
        public void SkipToStep(int stepIndex)
        {
            if (currentCutscene == null || stepIndex < 0 || stepIndex >= currentCutscene.steps.Count)
                return;

            // Complete current step
            if (currentStepIndex >= 0 && currentStepIndex < currentCutscene.steps.Count)
            {
                OnStepCompleted?.Invoke(currentCutscene.steps[currentStepIndex]);
            }

            currentStepIndex = stepIndex;
            stepStartTime = Time.time;

            StartStep(stepIndex);
        }

        /// <summary>
        /// Gets the current playback progress (0-1)
        /// </summary>
        public float GetCutsceneProgress()
        {
            if (currentCutscene == null) return 0f;

            float totalDuration = currentCutscene.GetEstimatedDuration();
            float elapsedTime = GetElapsedTime();

            return Mathf.Clamp01(elapsedTime / totalDuration);
        }

        /// <summary>
        /// Gets the progress of the current step (0-1)
        /// </summary>
        public float GetCurrentStepProgress()
        {
            if (currentCutscene == null || currentStepIndex >= currentCutscene.steps.Count)
                return 0f;

            float elapsedStepTime = Time.time - stepStartTime - currentCutscene.steps[currentStepIndex].delay;
            float stepDuration = currentCutscene.steps[currentStepIndex].duration;

            return Mathf.Clamp01(elapsedStepTime / stepDuration);
        }

        /// <summary>
        /// Checks if a cutscene is currently playing
        /// </summary>
        public bool IsCutscenePlaying()
        {
            return isPlaying && !isPaused;
        }

        /// <summary>
        /// Gets the currently playing cutscene
        /// </summary>
        public CutsceneAsset GetCurrentCutscene()
        {
            return currentCutscene;
        }

        /// <summary>
        /// Gets the current step index
        /// </summary>
        public int GetCurrentStepIndex()
        {
            return currentStepIndex;
        }

        /// <summary>
        /// Gets the current step
        /// </summary>
        public CutsceneStep GetCurrentStep()
        {
            if (currentCutscene == null || currentStepIndex >= currentCutscene.steps.Count)
                return null;

            return currentCutscene.steps[currentStepIndex];
        }

        /// <summary>
        /// Starts a dialogue sequence (used by PlayDialogueAction)
        /// </summary>
        public void StartDialogue(DialogueAsset dialogueAsset)
        {
            if (controller != null)
            {
                controller.StartDialogue(dialogueAsset);
            }
        }

        /// <summary>
        /// Checks if dialogue is currently active (used by PlayDialogueAction)
        /// </summary>
        public bool IsDialogueActive()
        {
            return controller != null && controller.IsDialogueActive();
        }

        /// <summary>
        /// Schedules an audio source for cleanup after it finishes playing
        /// </summary>
        public void ScheduleAudioCleanup(AudioSource audioSource, float delay)
        {
            audioCleanupQueue.Add(new AudioCleanupData
            {
                audioSource = audioSource,
                cleanupTime = Time.time + delay
            });
        }

        private void UpdateCutscene()
        {
            if (currentCutscene == null) return;

            float elapsedTime = GetElapsedTime();

            // Check if we should move to the next step
            if (ShouldAdvanceToNextStep(elapsedTime))
            {
                AdvanceToNextStep();
                return;
            }

            // Update current step
            UpdateCurrentStep();
        }

        private float GetElapsedTime()
        {
            return Time.time - stepStartTime;
        }

        private bool ShouldAdvanceToNextStep(float elapsedTime)
        {
            if (currentStepIndex >= currentCutscene.steps.Count - 1)
                return false; // Last step

            float currentStepEndTime = currentCutscene.steps[currentStepIndex].GetDuration();
            return elapsedTime >= currentStepEndTime;
        }

        private void AdvanceToNextStep()
        {
            // Complete current step
            if (currentStepIndex >= 0 && currentStepIndex < currentCutscene.steps.Count)
            {
                OnStepCompleted?.Invoke(currentCutscene.steps[currentStepIndex]);
            }

            currentStepIndex++;

            // Check if cutscene is complete
            if (currentStepIndex >= currentCutscene.steps.Count)
            {
                CompleteCutscene();
                return;
            }

            // Start next step
            stepStartTime = Time.time;
            StartStep(currentStepIndex);
        }

        private void StartStep(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= currentCutscene.steps.Count)
                return;

            CutsceneStep step = currentCutscene.steps[stepIndex];

            // Apply camera settings
            ApplyCameraSettings(step);

            // Apply scene effects
            ApplySceneEffects(step);

            // Play step audio
            PlayStepAudio(step);

            OnStepStarted?.Invoke(step);
        }

        private void UpdateCurrentStep()
        {
            if (currentStepIndex >= currentCutscene.steps.Count)
                return;

            CutsceneStep currentStep = currentCutscene.steps[currentStepIndex];

            // Calculate step progress (excluding delay)
            float stepElapsedTime = Time.time - stepStartTime - currentStep.delay;
            if (stepElapsedTime < 0f) return; // Still in delay period

            float stepProgress = Mathf.Clamp01(stepElapsedTime / currentStep.duration);

            // Execute all actions in the step
            foreach (var action in currentStep.actions)
            {
                float actionStartTime = currentStep.duration * action.normalizedStartTime;
                float actionEndTime = currentStep.duration * action.normalizedEndTime;

                // Check if this action should be executing now
                if (stepElapsedTime >= actionStartTime && stepElapsedTime <= actionEndTime)
                {
                    float actionProgress = Mathf.Clamp01((stepElapsedTime - actionStartTime) / (actionEndTime - actionStartTime));
                    action.Execute(this, actionProgress);
                }
            }

            // Update overall cutscene progress
            OnCutsceneProgress?.Invoke(GetCutsceneProgress());
        }

        private void CompleteCutscene()
        {
            isPlaying = false;

            // Execute completion action
            ExecuteCompletionAction();

            // Restore environment
            RestoreCutsceneEnvironment();

            OnCutsceneCompleted?.Invoke(currentCutscene);

            currentCutscene = null;
            currentStepIndex = 0;
        }

        private void SetupCutsceneEnvironment()
        {
            if (currentCutscene == null) return;

            // Pause gameplay if requested
            if (currentCutscene.pauseGameplayDuringCutscene && controller != null)
            {
                controller.PauseGameplay();
            }

            // Hide UI if requested
            if (currentCutscene.hideUIDuringCutscene && controller != null)
            {
                controller.HideUI();
            }

            // Setup camera
            if (mainCamera != null && currentCutscene.defaultCameraSettings.useCustomSettings)
            {
                ApplyCameraSettings(currentCutscene.defaultCameraSettings);
            }

            // Start background music
            if (currentCutscene.backgroundMusic != null && controller != null)
            {
                controller.PlayBackgroundMusic(currentCutscene.backgroundMusic, currentCutscene.musicVolume, currentCutscene.loopBackgroundMusic);
            }
        }

        private void RestoreCutsceneEnvironment()
        {
            // Resume gameplay if it was paused
            if (controller != null)
            {
                controller.ResumeGameplay();
                controller.ShowUI();
                controller.StopBackgroundMusic();
            }
        }

        private void ApplyCameraSettings(CutsceneStep step)
        {
            if (mainCamera == null || !step.cameraSettings.useCustomSettings) return;

            ApplyCameraSettings(step.cameraSettings);
        }

        private void ApplyCameraSettings(CameraSettings settings)
        {
            if (mainCamera == null) return;

            if (settings.targetTransform != null)
            {
                Vector3 targetPosition = settings.GetTargetPosition();
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * settings.damping);
                mainCamera.transform.LookAt(settings.targetTransform);
            }

            if (settings.fieldOfView != mainCamera.fieldOfView)
            {
                mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, settings.fieldOfView, Time.deltaTime * settings.damping);
            }
        }

        private void ApplySceneEffects(CutsceneStep step)
        {
            // Apply fade effects, screen overlays, etc.
            // This would integrate with your post-processing system
        }

        private void PlayStepAudio(CutsceneStep step)
        {
            // Play voice over
            if (step.voiceOverAudio != null && controller != null)
            {
                controller.PlayVoiceOver(step.voiceOverAudio, step.voiceOverVolume);
            }

            // Play sound effect
            if (step.soundEffect != null && controller != null)
            {
                controller.PlaySoundEffect(step.soundEffect, step.soundEffectVolume);
            }
        }

        private void ExecuteCompletionAction()
        {
            if (currentCutscene == null || controller == null) return;

            switch (currentCutscene.completionAction)
            {
                case CutsceneCompletionAction.StartDialogue:
                    if (!string.IsNullOrEmpty(currentCutscene.targetDialogueID))
                    {
                        controller.StartDialogueByID(currentCutscene.targetDialogueID);
                    }
                    break;

                case CutsceneCompletionAction.LoadScene:
                    if (!string.IsNullOrEmpty(currentCutscene.targetSceneName))
                    {
                        controller.LoadScene(currentCutscene.targetSceneName);
                    }
                    break;

                case CutsceneCompletionAction.EnablePlayerControl:
                    controller.EnablePlayerControl();
                    break;

                case CutsceneCompletionAction.ExecuteCustomAction:
                    // Custom actions would be handled by the controller
                    controller.ExecuteCustomCompletionAction();
                    break;
            }
        }

        private void UpdateAudioCleanup()
        {
            for (int i = audioCleanupQueue.Count - 1; i >= 0; i--)
            {
                if (Time.time >= audioCleanupQueue[i].cleanupTime)
                {
                    if (audioCleanupQueue[i].audioSource != null)
                    {
                        Destroy(audioCleanupQueue[i].audioSource.gameObject);
                    }
                    audioCleanupQueue.RemoveAt(i);
                }
            }
        }

        private void CleanupAllAudio()
        {
            foreach (var cleanupData in audioCleanupQueue)
            {
                if (cleanupData.audioSource != null)
                {
                    Destroy(cleanupData.audioSource.gameObject);
                }
            }
            audioCleanupQueue.Clear();
        }

        private struct AudioCleanupData
        {
            public AudioSource audioSource;
            public float cleanupTime;
        }
    }
}
