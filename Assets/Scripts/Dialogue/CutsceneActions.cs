using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Moves a transform from one position/rotation to another over time
    /// </summary>
    [System.Serializable]
    public class MoveTransformAction : CutsceneAction
    {
        public Transform targetTransform;
        public Vector3 startPosition;
        public Vector3 endPosition;
        public Vector3 startRotation;
        public Vector3 endRotation;
        public bool useLocalCoordinates = true;
        public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (targetTransform == null) return;

            // Calculate eased progress
            float easedProgress = positionCurve.Evaluate(progress);

            // Interpolate position
            Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, easedProgress);
            if (useLocalCoordinates)
            {
                targetTransform.localPosition = currentPosition;
            }
            else
            {
                targetTransform.position = currentPosition;
            }

            // Interpolate rotation
            float rotationProgress = rotationCurve.Evaluate(progress);
            Vector3 currentRotation = Vector3.Lerp(startRotation, endRotation, rotationProgress);
            if (useLocalCoordinates)
            {
                targetTransform.localRotation = Quaternion.Euler(currentRotation);
            }
            else
            {
                targetTransform.rotation = Quaternion.Euler(currentRotation);
            }
        }

        public override string GetValidationErrors()
        {
            if (targetTransform == null)
                return "Target transform is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Fades a CanvasGroup or SpriteRenderer in/out
    /// </summary>
    [System.Serializable]
    public class FadeAction : CutsceneAction
    {
        public enum FadeTargetType { CanvasGroup, SpriteRenderer, Image }

        public FadeTargetType fadeTargetType;
        public Object fadeTarget; // CanvasGroup, SpriteRenderer, or Image
        public float startAlpha = 0f;
        public float endAlpha = 1f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (fadeTarget == null) return;

            float easedProgress = fadeCurve.Evaluate(progress);
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, easedProgress);

            switch (fadeTargetType)
            {
                case FadeTargetType.CanvasGroup:
                    CanvasGroup canvasGroup = fadeTarget as CanvasGroup;
                    if (canvasGroup != null)
                        canvasGroup.alpha = currentAlpha;
                    break;

                case FadeTargetType.SpriteRenderer:
                    SpriteRenderer spriteRenderer = fadeTarget as SpriteRenderer;
                    if (spriteRenderer != null)
                    {
                        Color color = spriteRenderer.color;
                        color.a = currentAlpha;
                        spriteRenderer.color = color;
                    }
                    break;

                case FadeTargetType.Image:
                    UnityEngine.UI.Image image = fadeTarget as UnityEngine.UI.Image;
                    if (image != null)
                    {
                        Color color = image.color;
                        color.a = currentAlpha;
                        image.color = color;
                    }
                    break;
            }
        }

        public override string GetValidationErrors()
        {
            if (fadeTarget == null)
                return "Fade target is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Plays an animation on an Animator component
    /// </summary>
    [System.Serializable]
    public class PlayAnimationAction : CutsceneAction
    {
        public Animator targetAnimator;
        public string animationStateName;
        public float normalizedTransitionTime = 0.25f;
        public int layerIndex = 0;

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (targetAnimator == null || string.IsNullOrEmpty(animationStateName)) return;

            // For now, we'll trigger the animation at the start
            // In a more sophisticated system, you might want to blend animations over time
            if (progress == 0f)
            {
                targetAnimator.CrossFade(animationStateName, normalizedTransitionTime, layerIndex);
            }
        }

        public override string GetValidationErrors()
        {
            if (targetAnimator == null)
                return "Target animator is null";
            if (string.IsNullOrEmpty(animationStateName))
                return "Animation state name is empty";
            return string.Empty;
        }
    }

    /// <summary>
    /// Changes the active state of a GameObject
    /// </summary>
    [System.Serializable]
    public class SetActiveAction : CutsceneAction
    {
        public GameObject targetGameObject;
        public bool activate = true;
        public bool setAtStart = true; // If true, set active state at action start, otherwise at end

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (targetGameObject == null) return;

            // Set active state based on timing preference
            if ((setAtStart && progress == 0f) || (!setAtStart && progress == 1f))
            {
                targetGameObject.SetActive(activate);
            }
        }

        public override string GetValidationErrors()
        {
            if (targetGameObject == null)
                return "Target GameObject is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Plays a dialogue sequence as part of the cutscene
    /// </summary>
    [System.Serializable]
    public class PlayDialogueAction : CutsceneAction
    {
        public DialogueAsset dialogueAsset;
        public bool waitForDialogueCompletion = true;

        private bool dialogueStarted = false;

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (dialogueAsset == null) return;

            // Start dialogue at the beginning of the action
            if (!dialogueStarted && progress == 0f)
            {
                player.StartDialogue(dialogueAsset);
                dialogueStarted = true;
            }

            // If we need to wait for completion and dialogue is still running,
            // we should pause the cutscene progression
            if (waitForDialogueCompletion && player.IsDialogueActive())
            {
                player.PauseCutscene();
            }
        }

        public override string GetValidationErrors()
        {
            if (dialogueAsset == null)
                return "Dialogue asset is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Controls GIF animation states during cutscene
    /// </summary>
    [System.Serializable]
    public class ControlGifAction : CutsceneAction
    {
        public GifPlayer targetGifPlayer;
        public enum GifCommand { Play, Pause, Stop, SwitchToIdle, SwitchToTalking, SetGif }
        public GifCommand command;
        public GifAsset targetGifAsset;

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (targetGifPlayer == null) return;

            // Execute command at the start of the action
            if (progress == 0f)
            {
                switch (command)
                {
                    case GifCommand.Play:
                        targetGifPlayer.Play();
                        break;
                    case GifCommand.Pause:
                        targetGifPlayer.Pause();
                        break;
                    case GifCommand.Stop:
                        targetGifPlayer.Stop();
                        break;
                    case GifCommand.SwitchToIdle:
                        targetGifPlayer.SwitchToIdle();
                        break;
                    case GifCommand.SwitchToTalking:
                        targetGifPlayer.SwitchToTalking();
                        break;
                    case GifCommand.SetGif:
                        if (targetGifAsset != null)
                            targetGifPlayer.GifAsset = targetGifAsset;
                        break;
                }
            }
        }

        public override string GetValidationErrors()
        {
            if (targetGifPlayer == null)
                return "Target GIF player is null";

            if (command == GifCommand.SetGif && targetGifAsset == null)
                return "Target GIF asset is null for SetGif command";

            return string.Empty;
        }
    }

    /// <summary>
    /// Moves the camera to follow a target or to a specific position
    /// </summary>
    [System.Serializable]
    public class CameraMovementAction : CutsceneAction
    {
        public UnityEngine.Camera targetCamera;
        public Transform followTarget;
        public Vector3 targetPosition;
        public Vector3 targetRotation;
        public float fieldOfView = 60f;
        public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 startPosition;
        private Vector3 startRotation;
        private float startFOV;

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (targetCamera == null) return;

            // Initialize start values on first execution
            if (progress == 0f)
            {
                startPosition = targetCamera.transform.position;
                startRotation = targetCamera.transform.rotation.eulerAngles;
                startFOV = targetCamera.fieldOfView;
            }

            // Calculate eased progress for each component
            float positionProgress = positionCurve.Evaluate(progress);
            float rotationProgress = rotationCurve.Evaluate(progress);
            float fovProgress = fovCurve.Evaluate(progress);

            // Interpolate position
            Vector3 currentPosition;
            if (followTarget != null)
            {
                currentPosition = Vector3.Lerp(startPosition, followTarget.position + targetPosition, positionProgress);
            }
            else
            {
                currentPosition = Vector3.Lerp(startPosition, targetPosition, positionProgress);
            }
            targetCamera.transform.position = currentPosition;

            // Interpolate rotation
            Vector3 currentRotation = Vector3.Lerp(startRotation, targetRotation, rotationProgress);
            targetCamera.transform.rotation = Quaternion.Euler(currentRotation);

            // Interpolate FOV
            float currentFOV = Mathf.Lerp(startFOV, fieldOfView, fovProgress);
            targetCamera.fieldOfView = currentFOV;
        }

        public override string GetValidationErrors()
        {
            if (targetCamera == null)
                return "Target camera is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Plays audio with optional spatial audio settings
    /// </summary>
    [System.Serializable]
    public class PlayAudioAction : CutsceneAction
    {
        public AudioClip audioClip;
        public AudioSource audioSource;
        public Vector3 audioPosition = Vector3.zero;
        public bool useSpatialAudio = false;
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = false;

        private AudioSource createdAudioSource;

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (audioClip == null) return;

            // Start audio at the beginning of the action
            if (progress == 0f)
            {
                if (audioSource != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.volume = volume;
                    audioSource.loop = loop;
                    if (useSpatialAudio)
                    {
                        audioSource.spatialBlend = 1f;
                        audioSource.transform.position = audioPosition;
                    }
                    audioSource.Play();
                }
                else
                {
                    // Create temporary audio source if none provided
                    GameObject tempAudio = new GameObject("TempAudio");
                    createdAudioSource = tempAudio.AddComponent<AudioSource>();
                    createdAudioSource.clip = audioClip;
                    createdAudioSource.volume = volume;
                    createdAudioSource.loop = loop;
                    if (useSpatialAudio)
                    {
                        createdAudioSource.spatialBlend = 1f;
                        tempAudio.transform.position = audioPosition;
                    }
                    createdAudioSource.Play();

                    // Schedule cleanup
                    player.ScheduleAudioCleanup(createdAudioSource, audioClip.length);
                }
            }
        }

        public override string GetValidationErrors()
        {
            if (audioClip == null)
                return "Audio clip is null";
            return string.Empty;
        }
    }

    /// <summary>
    /// Applies a post-processing effect or screen overlay
    /// </summary>
    [System.Serializable]
    public class ScreenEffectAction : CutsceneAction
    {
        public Material effectMaterial;
        public Texture overlayTexture;
        [Range(0f, 1f)] public float effectIntensity = 1f;
        public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override void Execute(CutscenePlayer player, float progress)
        {
            if (effectMaterial == null) return;

            float easedProgress = intensityCurve.Evaluate(progress);
            float currentIntensity = Mathf.Lerp(0f, effectIntensity, easedProgress);

            // Apply the effect intensity to the material
            effectMaterial.SetFloat("_Intensity", currentIntensity);

            if (overlayTexture != null)
            {
                effectMaterial.SetTexture("_OverlayTex", overlayTexture);
            }
        }

        public override string GetValidationErrors()
        {
            if (effectMaterial == null)
                return "Effect material is null";
            return string.Empty;
        }
    }
}
