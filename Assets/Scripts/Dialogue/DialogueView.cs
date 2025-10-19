using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Unbound.Dialogue
{
    /// <summary>
    /// UI component responsible for displaying dialogue and handling user interaction
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TextMeshProUGUI speakerNameText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private Image portraitImage;
        [SerializeField] private GifPlayer gifPlayer;
        [SerializeField] private DialogueGifController gifController;
        [SerializeField] private GameObject choicesPanel;
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button clickToContinueButton;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float textTypeSpeed = 30f; // Characters per second
        [SerializeField] private bool useTextAnimation = true;

        [Header("Audio")]
        [SerializeField] private AudioSource textAudioSource;
        [SerializeField] private AudioClip textTypeSound;

        // Runtime state
        private List<Button> activeChoiceButtons = new List<Button>();
        private Coroutine currentTextAnimation;
        private CanvasGroup canvasGroup;
        private string fullDialogueText;
        private bool isTextComplete;
        private float textSpeedMultiplier = 1f;

        // Events
        public UnityEvent<DialogueChoice> OnChoiceSelected;
        public UnityEvent OnContinuePressed;

        private void Awake()
        {
            canvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }

            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (choicesPanel != null)
            {
                choicesPanel.SetActive(false);
            }

            // Ensure text speed multiplier is properly initialized
            textSpeedMultiplier = 1f;
        }

        /// <summary>
        /// Shows a dialogue node
        /// </summary>
        public void ShowNode(DialogueNode node, IDialogueConditionEvaluator conditionEvaluator)
        {
            if (dialoguePanel == null)
                return;

            // Setup GIF controller for this node
            if (gifController != null)
            {
                gifController.SetupForNode(node);
            }

            dialoguePanel.SetActive(true);
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());

            // Set speaker name
            if (speakerNameText != null)
            {
                speakerNameText.text = node.speakerID; // TODO: Localize this
            }

            // Set portrait
            if (portraitImage != null || gifPlayer != null)
            {
                if (node.IsGifPortrait() && gifPlayer != null)
                {
                    // Use GIF player for GIF portraits
                    gifPlayer.GifAsset = node.portraitGif;
                    gifPlayer.gameObject.SetActive(true);

                    // Hide portrait image if it exists
                    if (portraitImage != null)
                    {
                        portraitImage.gameObject.SetActive(false);
                    }
                }
                else if (node.IsSpritePortrait() && portraitImage != null)
                {
                    // Use regular image for sprite portraits
                    portraitImage.sprite = node.portraitSprite;
                    portraitImage.gameObject.SetActive(true);

                    // Hide GIF player if it exists
                    if (gifPlayer != null)
                    {
                        gifPlayer.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // No portrait - hide both
                    if (portraitImage != null)
                    {
                        portraitImage.gameObject.SetActive(false);
                    }
                    if (gifPlayer != null)
                    {
                        gifPlayer.gameObject.SetActive(false);
                    }
                }
            }

            // Store full text for typing animation
            fullDialogueText = node.dialogueTextKey; // TODO: Localize this

            // Reset text state and speed
            isTextComplete = false;
            ResetTextSpeed();

            // Start typing animation
            if (useTextAnimation && dialogueText != null)
            {
                if (currentTextAnimation != null)
                {
                    StopCoroutine(currentTextAnimation);
                }

                // Notify GIF controller that text animation is starting
                if (gifController != null)
                {
                    gifController.OnTextAnimationStart();
                }

                currentTextAnimation = StartCoroutine(TypeText(node, conditionEvaluator));
            }
            else
            {
                // Show text immediately
                if (dialogueText != null)
                {
                    dialogueText.text = fullDialogueText;
                }
                isTextComplete = true;
                ShowContinueButton();
            }

            // Hide choices initially
            if (choicesPanel != null)
            {
                choicesPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows dialogue choices
        /// </summary>
        public void ShowChoices(List<DialogueChoice> choices, IDialogueConditionEvaluator conditionEvaluator)
        {
            if (choicesPanel == null || choiceButtonPrefab == null)
                return;

            // Clear existing choices
            ClearChoiceButtons();

            if (choices.Count == 0)
            {
                // Show continue button if no choices
                ShowContinueButton();
                return;
            }

            choicesPanel.SetActive(true);

            // Create choice buttons
            foreach (var choice in choices)
            {
                var choiceButton = Instantiate(choiceButtonPrefab, choicesPanel.transform);
                var choiceText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();

                if (choiceText != null)
                {
                    choiceText.text = choice.choiceTextKey; // TODO: Localize this
                }

                choiceButton.onClick.AddListener(() => SelectChoice(choice));
                activeChoiceButtons.Add(choiceButton);
            }

            // Hide continue button when showing choices
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Coroutine for typing text animation
        /// </summary>
        public IEnumerator TypeText(DialogueNode node, IDialogueConditionEvaluator conditionEvaluator)
        {
            if (dialogueText == null)
                yield break;

            // Clear any existing text to prevent duplication
            dialogueText.text = "";
            isTextComplete = false;
            textSpeedMultiplier = 1f; // Reset speed multiplier

            string displayText = fullDialogueText;
            float baseTypeSpeed = node.textSpeed * textTypeSpeed / 60f; // Convert to characters per frame

            for (int i = 0; i < displayText.Length; i++)
            {
                // Build the text up to the current character
                dialogueText.text = displayText.Substring(0, i + 1);

                // Play typing sound
                if (textAudioSource != null && textTypeSound != null && i % 3 == 0)
                {
                    textAudioSource.PlayOneShot(textTypeSound, 0.5f);
                }

                // Wait for next character, applying speed multiplier
                float currentTypeSpeed = baseTypeSpeed * textSpeedMultiplier;
                if (currentTypeSpeed > 0)
                {
                    yield return new WaitForSeconds(1f / currentTypeSpeed);
                }
            }

            isTextComplete = true;

            // Notify GIF controller that text animation is complete
            if (gifController != null)
            {
                gifController.OnTextAnimationComplete();
            }

            ShowContinueButton();
        }

        /// <summary>
        /// Hides the dialogue UI
        /// </summary>
        public void Hide()
        {
            if (dialoguePanel == null)
                return;

            if (currentTextAnimation != null)
            {
                StopCoroutine(currentTextAnimation);
                currentTextAnimation = null;
            }

            // Hide click-to-continue button
            if (clickToContinueButton != null)
            {
                clickToContinueButton.gameObject.SetActive(false);
            }

            StartCoroutine(FadeOut());
        }

        /// <summary>
        /// Shows the continue button
        /// </summary>
        private void ShowContinueButton()
        {
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(() => OnContinuePressed?.Invoke());
            }

            // Also enable click-to-continue if available
            if (clickToContinueButton != null)
            {
                clickToContinueButton.gameObject.SetActive(true);
                clickToContinueButton.onClick.RemoveAllListeners();
                clickToContinueButton.onClick.AddListener(() => OnContinuePressed?.Invoke());
            }
        }

        /// <summary>
        /// Clears all choice buttons
        /// </summary>
        private void ClearChoiceButtons()
        {
            foreach (var button in activeChoiceButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            activeChoiceButtons.Clear();
        }

        /// <summary>
        /// Handles choice selection
        /// </summary>
        private void SelectChoice(DialogueChoice choice)
        {
            ClearChoiceButtons();

            if (choicesPanel != null)
            {
                choicesPanel.SetActive(false);
            }

            ShowContinueButton();
            OnChoiceSelected?.Invoke(choice);
        }

        /// <summary>
        /// Fade in animation
        /// </summary>
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Fade out animation
        /// </summary>
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(startAlpha - (elapsed / fadeInDuration));
                yield return null;
            }

            canvasGroup.alpha = 0f;
            dialoguePanel.SetActive(false);
        }

        /// <summary>
        /// Skips the current text animation
        /// </summary>
        public void SkipTextAnimation()
        {
            if (currentTextAnimation != null)
            {
                StopCoroutine(currentTextAnimation);
                currentTextAnimation = null;
            }

            if (dialogueText != null)
            {
                dialogueText.text = fullDialogueText;
            }

            isTextComplete = true;

            // Notify GIF controller that text animation is complete (skipped)
            if (gifController != null)
            {
                gifController.OnTextAnimationComplete();
            }

            // Show continue button if not showing choices
            if (continueButton != null && choicesPanel != null && !choicesPanel.activeSelf)
            {
                ShowContinueButton();
            }
        }

        /// <summary>
        /// Speeds up the current text animation (reduces typing delay)
        /// Each call increases speed by 2x (up to 10x max)
        /// </summary>
        public void SpeedUpTextAnimation()
        {
            // Increase speed multiplier for faster typing (max 10x speed to prevent issues)
            textSpeedMultiplier = Mathf.Min(textSpeedMultiplier + 2f, 10f);

            // If we're at max speed, just complete the text
            if (textSpeedMultiplier >= 10f && !isTextComplete)
            {
                SkipTextAnimation();
            }
        }

        /// <summary>
        /// Resets the text speed multiplier to normal speed
        /// </summary>
        public void ResetTextSpeed()
        {
            textSpeedMultiplier = 1f;
        }

        /// <summary>
        /// Checks if the current text animation is complete
        /// </summary>
        public bool IsTextComplete()
        {
            return isTextComplete;
        }

        private void OnDestroy()
        {
            ClearChoiceButtons();
        }
    }
}
