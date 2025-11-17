using UnityEngine;
using System.Collections;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Controller for managing GIF portrait animations and transitions in dialogue
    /// Handles idle/talking state transitions and provides smooth animation blending
    /// </summary>
    public class DialogueGifController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GifPlayer gifPlayer;
        [SerializeField] private DialogueView dialogueView;

        [Header("Animation Settings")]
        [SerializeField] private bool autoTransition = true;

        [Header("Audio Settings")]
        [SerializeField] private bool syncWithAudio = false;
        [SerializeField] private AudioSource characterAudioSource;

        // State management
        private GifAsset currentGifAsset;
        private GifAsset idleGif;
        private GifAsset talkingGif;
        private DialogueNode currentNode;

        private bool isTalking = false;
        private float lastTextSpeedChange = 0f;

        private void Awake()
        {
            if (gifPlayer == null)
            {
                gifPlayer = GetComponent<GifPlayer>();
            }

            if (dialogueView == null)
            {
                dialogueView = FindFirstObjectByType<DialogueView>();
            }

            if (dialogueView != null)
            {
                dialogueView.OnContinuePressed.AddListener(OnContinuePressed);
            }
        }

        private void OnDestroy()
        {
            if (dialogueView != null)
            {
                dialogueView.OnContinuePressed.RemoveListener(OnContinuePressed);
            }
        }

        /// <summary>
        /// Sets up the GIF controller for a specific dialogue node
        /// </summary>
        public void SetupForNode(DialogueNode node)
        {
            currentNode = node;

            if (node == null)
            {
                SetIdleState();
                return;
            }

            // Get the GIF asset from the node
            GifAsset nodeGif = node.GetPortraitGif();
            if (node.IsGifPortrait() && nodeGif != null)
            {
                currentGifAsset = nodeGif;
                idleGif = nodeGif.IdleTransition;
                talkingGif = nodeGif.TalkingTransition;

                // Set the main GIF asset
                if (gifPlayer != null)
                {
                    gifPlayer.GifAsset = currentGifAsset;
                }

                // Start in idle state if available, otherwise use the main asset
                if (idleGif != null && autoTransition)
                {
                    gifPlayer.SwitchToGif(idleGif);
                }
            }
            else
            {
                // No GIF portrait, disable the player
                if (gifPlayer != null)
                {
                    gifPlayer.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Called when text animation starts - immediately transition to talking state
        /// </summary>
        public void OnTextAnimationStart()
        {
            if (!autoTransition || currentNode == null || !currentNode.IsGifPortrait())
                return;

            SetTalkingState();
        }

        /// <summary>
        /// Called when text animation completes - immediately transition back to idle state
        /// </summary>
        public void OnTextAnimationComplete()
        {
            if (!autoTransition || currentNode == null || !currentNode.IsGifPortrait())
                return;

            SetIdleState();
        }

        /// <summary>
        /// Called when dialogue continues to next node
        /// </summary>
        private void OnContinuePressed()
        {
            // Reset to idle state when continuing
            SetIdleState();
        }

        /// <summary>
        /// Immediately sets the idle state
        /// </summary>
        public void SetIdleState()
        {
            if (gifPlayer == null || currentGifAsset == null)
                return;

            if (idleGif != null)
            {
                gifPlayer.SwitchToGif(idleGif);
            }
            else
            {
                gifPlayer.SwitchToGif(currentGifAsset);
            }

            isTalking = false;
        }

        /// <summary>
        /// Immediately sets the talking state
        /// </summary>
        public void SetTalkingState()
        {
            if (gifPlayer == null || talkingGif == null)
                return;

            gifPlayer.SwitchToGif(talkingGif);
            isTalking = true;
        }


        /// <summary>
        /// Called when text speed changes (for audio-synced animations)
        /// </summary>
        public void OnTextSpeedChanged(float newSpeed)
        {
            lastTextSpeedChange = Time.time;

            if (syncWithAudio && characterAudioSource != null)
            {
                // Adjust GIF playback speed based on text speed for lip-sync effect
                float speedMultiplier = newSpeed / 30f; // Assuming 30 is base speed
                // Note: GifPlayer would need a SetPlaybackSpeed method for this
            }
        }

        /// <summary>
        /// Manually trigger a transition between idle and talking states
        /// </summary>
        public void ToggleTalkingState()
        {
            if (isTalking)
            {
                SetIdleState();
            }
            else
            {
                SetTalkingState();
            }
        }

        /// <summary>
        /// Check if currently in talking state
        /// </summary>
        public bool IsInTalkingState()
        {
            return isTalking;
        }

        /// <summary>
        /// Get the current GIF asset being used
        /// </summary>
        public GifAsset GetCurrentGifAsset()
        {
            return gifPlayer?.GifAsset;
        }

        /// <summary>
        /// Force a specific GIF to play
        /// </summary>
        public void SetGifAsset(GifAsset gifAsset)
        {
            if (gifPlayer != null)
            {
                gifPlayer.GifAsset = gifAsset;
                currentGifAsset = gifAsset;
                idleGif = gifAsset?.IdleTransition;
                talkingGif = gifAsset?.TalkingTransition;
            }
        }

        /// <summary>
        /// Enable or disable auto-transitions
        /// </summary>
        public void SetAutoTransition(bool enabled)
        {
            autoTransition = enabled;
        }

        /// <summary>
        /// Get animation state information for debugging
        /// </summary>
        public string GetDebugInfo()
        {
            return $"GifController - Asset: {currentGifAsset?.name ?? "None"}, " +
                   $"Idle: {idleGif?.name ?? "None"}, " +
                   $"Talking: {talkingGif?.name ?? "None"}, " +
                   $"State: {(isTalking ? "Talking" : "Idle")}, " +
                   $"Auto: {autoTransition}";
        }
    }
}
