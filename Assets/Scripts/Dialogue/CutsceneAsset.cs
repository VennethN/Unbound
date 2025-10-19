using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// ScriptableObject representing a complete cutscene sequence.
    /// Contains metadata and a sequence of cutscene steps that define the visual narrative.
    /// </summary>
    [CreateAssetMenu(menuName = "Unbound/Dialogue/Cutscene Asset")]
    public class CutsceneAsset : ScriptableObject
    {
        [Header("Cutscene Metadata")]
        public string cutsceneID;
        public string displayName = "New Cutscene";
        public string description = "";

        [Header("Scene Settings")]
        public bool pauseGameplayDuringCutscene = true;
        public bool hideUIDuringCutscene = true;
        public float fadeInDuration = 0.5f;
        public float fadeOutDuration = 0.5f;

        [Header("Cutscene Steps")]
        [SerializeReference] public List<CutsceneStep> steps = new List<CutsceneStep>();

        [Header("Camera Settings")]
        public CameraSettings defaultCameraSettings = new CameraSettings();

        [Header("Audio Settings")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 1f;
        public bool loopBackgroundMusic = true;

        [Header("Post-Cutscene Actions")]
        public CutsceneCompletionAction completionAction = CutsceneCompletionAction.None;
        public string targetDialogueID; // For transitioning to dialogue
        public string targetSceneName; // For scene transitions
        public Vector3 playerSpawnPosition; // For position resets

        /// <summary>
        /// Validates this cutscene asset for errors
        /// </summary>
        public bool IsValid()
        {
            return string.IsNullOrEmpty(GetValidationErrors());
        }

        /// <summary>
        /// Gets detailed validation errors for this cutscene
        /// </summary>
        public string GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(cutsceneID))
            {
                errors.Add("Cutscene ID is null or empty");
            }

            if (steps.Count == 0)
            {
                errors.Add("Cutscene has no steps");
            }

            // Validate each step
            for (int i = 0; i < steps.Count; i++)
            {
                string stepError = steps[i].GetValidationErrors();
                if (!string.IsNullOrEmpty(stepError))
                {
                    errors.Add($"Step {i}: {stepError}");
                }
            }

            return errors.Count > 0 ? string.Join("; ", errors) : string.Empty;
        }

        /// <summary>
        /// Gets the total estimated duration of the cutscene
        /// </summary>
        public float GetEstimatedDuration()
        {
            float totalDuration = 0f;

            if (steps != null)
            {
                foreach (var step in steps)
                {
                    if (step != null)
                    {
                        totalDuration += step.GetDuration();
                    }
                }
            }

            return totalDuration + fadeInDuration + fadeOutDuration;
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(cutsceneID))
            {
                cutsceneID = $"cutscene_{GetHashCode()}";
            }
        }
    }

    /// <summary>
    /// Represents a single step in a cutscene sequence
    /// </summary>
    [System.Serializable]
    public class CutsceneStep
    {
        public string stepID;
        public string displayName = "New Step";
        public string description = "";

        [Header("Timing")]
        [Min(0f)] public float duration = 2f;
        [Min(0f)] public float delay = 0f;

        [Header("Actions")]
        [SerializeReference] public List<CutsceneAction> actions = new List<CutsceneAction>();

        [Header("Camera")]
        public CameraSettings cameraSettings = new CameraSettings();

        [Header("Audio")]
        public AudioClip voiceOverAudio;
        public AudioClip soundEffect;
        [Range(0f, 1f)] public float voiceOverVolume = 1f;
        [Range(0f, 1f)] public float soundEffectVolume = 1f;

        [Header("Scene Effects")]
        public Color fadeColor = Color.black;
        [Range(0f, 1f)] public float fadeAlpha = 0f;

        /// <summary>
        /// Validates this step for errors
        /// </summary>
        public string GetValidationErrors()
        {
            var errors = new List<string>();

            if (actions.Count == 0)
            {
                errors.Add("Step has no actions");
            }

            // Validate each action
            for (int i = 0; i < actions.Count; i++)
            {
                string actionError = actions[i].GetValidationErrors();
                if (!string.IsNullOrEmpty(actionError))
                {
                    errors.Add($"Action {i}: {actionError}");
                }
            }

            return errors.Count > 0 ? string.Join("; ", errors) : string.Empty;
        }

        /// <summary>
        /// Gets the total duration of this step including delay
        /// </summary>
        public float GetDuration()
        {
            return duration + delay;
        }

        private void OnValidate()
        {
            // Only auto-generate stepID if it's completely empty and we have a meaningful hash
            if (string.IsNullOrEmpty(stepID) && GetHashCode() != 0)
            {
                stepID = $"step_{GetHashCode()}";
            }
        }
    }

    /// <summary>
    /// Base class for all cutscene actions
    /// </summary>
    [System.Serializable]
    public abstract class CutsceneAction
    {
        public string actionID;
        public string displayName = "New Action";
        [TextArea] public string description = "";

        [Header("Timing")]
        [Range(0f, 1f)] public float normalizedStartTime = 0f; // When to start this action within the step (0-1)
        [Range(0f, 1f)] public float normalizedEndTime = 1f; // When to end this action within the step (0-1)

        /// <summary>
        /// Executes this action
        /// </summary>
        public abstract void Execute(CutscenePlayer player, float progress);

        /// <summary>
        /// Validates this action for errors
        /// </summary>
        public virtual string GetValidationErrors()
        {
            return string.Empty;
        }

        /// <summary>
        /// Gets the duration of this action within the step
        /// </summary>
        public float GetActionDuration(float stepDuration)
        {
            return stepDuration * (normalizedEndTime - normalizedStartTime);
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(actionID))
            {
                actionID = $"action_{GetHashCode()}";
            }
        }
    }

    /// <summary>
    /// Camera settings for a cutscene step
    /// </summary>
    [System.Serializable]
    public class CameraSettings
    {
        public Transform targetTransform;
        public Vector3 offset = new Vector3(0f, 1f, -5f);
        public float fieldOfView = 60f;
        public float damping = 5f;
        public bool useCustomSettings = false;

        /// <summary>
        /// Gets the target camera position based on these settings
        /// </summary>
        public Vector3 GetTargetPosition()
        {
            if (targetTransform == null)
                return offset;

            return targetTransform.position + offset;
        }
    }

    /// <summary>
    /// Actions that can be taken when a cutscene completes
    /// </summary>
    public enum CutsceneCompletionAction
    {
        None,
        LoadScene,
        StartDialogue,
        EnablePlayerControl,
        ExecuteCustomAction
    }
}
