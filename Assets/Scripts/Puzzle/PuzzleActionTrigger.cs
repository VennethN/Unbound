using UnityEngine;

namespace Unbound.Puzzle
{
    /// <summary>
    /// Component with callable functions for puzzle actions.
    /// Assign to objects and call these functions via Unity Events from PuzzleManager.
    /// </summary>
    public class PuzzleActionTrigger : MonoBehaviour
    {
        [Header("Target Objects")]
        [Tooltip("GameObjects to affect (if empty, uses this GameObject)")]
        [SerializeField] private GameObject[] targetObjects;

        [Header("Color Settings")]
        [SerializeField] private Color targetColor = Color.white;
        [Tooltip("Renderer components to change color (if empty, uses all Renderers)")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private bool useMaterialInstance = true;

        [Header("Component Settings")]
        [SerializeField] private Behaviour[] componentsToEnable;
        [SerializeField] private Behaviour[] componentsToDisable;

        [Header("Transform Settings")]
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private Vector3 targetRotation;
        [SerializeField] private Vector3 targetScale = Vector3.one;
        [SerializeField] private bool relativeToStart = false;

        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [SerializeField] private string animationTriggerName = "Activate";
        [SerializeField] private string animationStateName = "";

        [Header("Audio Settings")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip audioClip;

        private Vector3 initialPosition;
        private Vector3 initialRotation;
        private Vector3 initialScale;
        private bool hasInitialValues = false;

        private void Awake()
        {
            // Store initial transform values
            if (!hasInitialValues)
            {
                initialPosition = transform.localPosition;
                initialRotation = transform.localEulerAngles;
                initialScale = transform.localScale;
                hasInitialValues = true;
            }

            // Auto-find components if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Shows the target object(s)
        /// </summary>
        public void Show()
        {
            SetVisibility(true);
        }

        /// <summary>
        /// Shows the specified GameObject
        /// </summary>
        public void Show(GameObject target)
        {
            if (target != null)
            {
                target.SetActive(true);
            }
        }

        /// <summary>
        /// Hides the target object(s)
        /// </summary>
        public void Hide()
        {
            SetVisibility(false);
        }

        /// <summary>
        /// Hides the specified GameObject
        /// </summary>
        public void Hide(GameObject target)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }

        /// <summary>
        /// Toggles visibility of the target object(s)
        /// </summary>
        public void ToggleVisibility()
        {
            GameObject[] objectsToChange = GetTargetObjects();
            foreach (var obj in objectsToChange)
            {
                if (obj != null)
                {
                    obj.SetActive(!obj.activeSelf);
                }
            }
        }

        /// <summary>
        /// Toggles visibility of the specified GameObject
        /// </summary>
        public void ToggleVisibility(GameObject target)
        {
            if (target != null)
            {
                target.SetActive(!target.activeSelf);
            }
        }

        /// <summary>
        /// Sets visibility of the target object(s)
        /// </summary>
        public void SetVisibility(bool visible)
        {
            GameObject[] objectsToChange = GetTargetObjects();
            foreach (var obj in objectsToChange)
            {
                if (obj != null)
                {
                    obj.SetActive(visible);
                }
            }
        }

        /// <summary>
        /// Sets visibility of the specified GameObject
        /// </summary>
        public void SetVisibility(GameObject target, bool visible)
        {
            if (target != null)
            {
                target.SetActive(visible);
            }
        }

        /// <summary>
        /// Changes the color of renderer components
        /// </summary>
        public void ChangeColor()
        {
            ChangeColor(targetColor);
        }

        /// <summary>
        /// Changes the color of renderer components to specified color
        /// </summary>
        public void ChangeColor(Color color)
        {
            Renderer[] renderersToChange = GetTargetRenderers();

            foreach (var renderer in renderersToChange)
            {
                if (renderer != null)
                {
                    if (useMaterialInstance && renderer.material != null)
                    {
                        renderer.material = new Material(renderer.material);
                    }

                    if (renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }

        /// <summary>
        /// Changes the color of renderer components on the specified GameObject
        /// </summary>
        public void ChangeColor(GameObject target)
        {
            ChangeColor(target, targetColor);
        }

        /// <summary>
        /// Changes the color of renderer components on the specified GameObject to specified color
        /// </summary>
        public void ChangeColor(GameObject target, Color color)
        {
            if (target == null) return;

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    if (useMaterialInstance && renderer.material != null)
                    {
                        renderer.material = new Material(renderer.material);
                    }

                    if (renderer.material != null)
                    {
                        renderer.material.color = color;
                    }
                }
            }
        }

        /// <summary>
        /// Enables specified components
        /// </summary>
        public void EnableComponents()
        {
            foreach (var component in componentsToEnable)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }
        }

        /// <summary>
        /// Disables specified components
        /// </summary>
        public void DisableComponents()
        {
            foreach (var component in componentsToDisable)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }
        }

        /// <summary>
        /// Changes position to target position
        /// </summary>
        public void ChangePosition()
        {
            Vector3 finalPosition = relativeToStart ? initialPosition + targetPosition : targetPosition;
            transform.localPosition = finalPosition;
        }

        /// <summary>
        /// Changes position of the specified GameObject to target position
        /// </summary>
        public void ChangePosition(GameObject target)
        {
            if (target == null) return;
            Vector3 finalPosition = relativeToStart ? initialPosition + targetPosition : targetPosition;
            target.transform.localPosition = finalPosition;
        }

        /// <summary>
        /// Changes position of the specified GameObject to specified position
        /// </summary>
        public void ChangePosition(GameObject target, Vector3 position)
        {
            if (target != null)
            {
                target.transform.localPosition = position;
            }
        }

        /// <summary>
        /// Changes rotation to target rotation
        /// </summary>
        public void ChangeRotation()
        {
            Vector3 finalRotation = relativeToStart ? initialRotation + targetRotation : targetRotation;
            transform.localEulerAngles = finalRotation;
        }

        /// <summary>
        /// Changes rotation of the specified GameObject to target rotation
        /// </summary>
        public void ChangeRotation(GameObject target)
        {
            if (target == null) return;
            Vector3 finalRotation = relativeToStart ? initialRotation + targetRotation : targetRotation;
            target.transform.localEulerAngles = finalRotation;
        }

        /// <summary>
        /// Changes rotation of the specified GameObject to specified rotation
        /// </summary>
        public void ChangeRotation(GameObject target, Vector3 rotation)
        {
            if (target != null)
            {
                target.transform.localEulerAngles = rotation;
            }
        }

        /// <summary>
        /// Changes scale to target scale
        /// </summary>
        public void ChangeScale()
        {
            Vector3 finalScale = relativeToStart ? Vector3.Scale(initialScale, targetScale) : targetScale;
            transform.localScale = finalScale;
        }

        /// <summary>
        /// Changes scale of the specified GameObject to target scale
        /// </summary>
        public void ChangeScale(GameObject target)
        {
            if (target == null) return;
            Vector3 finalScale = relativeToStart ? Vector3.Scale(initialScale, targetScale) : targetScale;
            target.transform.localScale = finalScale;
        }

        /// <summary>
        /// Changes scale of the specified GameObject to specified scale
        /// </summary>
        public void ChangeScale(GameObject target, Vector3 scale)
        {
            if (target != null)
            {
                target.transform.localScale = scale;
            }
        }

        /// <summary>
        /// Changes all transform properties (position, rotation, scale)
        /// </summary>
        public void ChangeTransform()
        {
            ChangePosition();
            ChangeRotation();
            ChangeScale();
        }

        /// <summary>
        /// Changes all transform properties of the specified GameObject
        /// </summary>
        public void ChangeTransform(GameObject target)
        {
            if (target == null) return;
            ChangePosition(target);
            ChangeRotation(target);
            ChangeScale(target);
        }

        /// <summary>
        /// Triggers an animation trigger
        /// </summary>
        public void TriggerAnimation()
        {
            if (animator == null) return;

            if (!string.IsNullOrEmpty(animationTriggerName))
            {
                animator.SetTrigger(animationTriggerName);
            }
            else if (!string.IsNullOrEmpty(animationStateName))
            {
                animator.Play(animationStateName);
            }
        }

        /// <summary>
        /// Triggers an animation on the specified GameObject
        /// </summary>
        public void TriggerAnimation(GameObject target)
        {
            if (target == null) return;

            Animator targetAnimator = target.GetComponent<Animator>();
            if (targetAnimator == null) return;

            if (!string.IsNullOrEmpty(animationTriggerName))
            {
                targetAnimator.SetTrigger(animationTriggerName);
            }
            else if (!string.IsNullOrEmpty(animationStateName))
            {
                targetAnimator.Play(animationStateName);
            }
        }

        /// <summary>
        /// Triggers an animation with specified trigger name on the specified GameObject
        /// </summary>
        public void TriggerAnimation(GameObject target, string triggerName)
        {
            if (target == null) return;

            Animator targetAnimator = target.GetComponent<Animator>();
            if (targetAnimator != null && !string.IsNullOrEmpty(triggerName))
            {
                targetAnimator.SetTrigger(triggerName);
            }
        }

        /// <summary>
        /// Plays an audio clip
        /// </summary>
        public void PlayAudio()
        {
            if (audioSource == null) return;

            if (audioClip != null)
            {
                audioSource.PlayOneShot(audioClip);
            }
            else
            {
                audioSource.Play();
            }
        }

        /// <summary>
        /// Plays audio on the specified GameObject
        /// </summary>
        public void PlayAudio(GameObject target)
        {
            if (target == null) return;

            AudioSource targetAudioSource = target.GetComponent<AudioSource>();
            if (targetAudioSource == null) return;

            if (audioClip != null)
            {
                targetAudioSource.PlayOneShot(audioClip);
            }
            else
            {
                targetAudioSource.Play();
            }
        }

        /// <summary>
        /// Plays specified audio clip on the specified GameObject
        /// </summary>
        public void PlayAudio(GameObject target, AudioClip clip)
        {
            if (target == null) return;

            AudioSource targetAudioSource = target.GetComponent<AudioSource>();
            if (targetAudioSource != null && clip != null)
            {
                targetAudioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Resets transform to initial values
        /// </summary>
        public void ResetTransform()
        {
            if (hasInitialValues)
            {
                transform.localPosition = initialPosition;
                transform.localEulerAngles = initialRotation;
                transform.localScale = initialScale;
            }
        }

        private GameObject[] GetTargetObjects()
        {
            return targetObjects.Length > 0 ? targetObjects : new GameObject[] { gameObject };
        }

        private Renderer[] GetTargetRenderers()
        {
            if (targetRenderers.Length > 0)
            {
                return targetRenderers;
            }
            else
            {
                return GetComponentsInChildren<Renderer>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-find components if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }
#endif
    }
}

