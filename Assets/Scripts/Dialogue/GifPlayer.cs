using UnityEngine;
using UnityEngine.UI;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Component for playing GIF animations in Unity UI
    /// Integrates with GifAsset ScriptableObjects for configurable GIF animations
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class GifPlayer : MonoBehaviour
    {
        [Header("GIF Settings")]
        [SerializeField] private GifAsset gifAsset;
        [SerializeField] private bool playOnAwake = true;
        [SerializeField] private bool autoUpdate = true;

        [Header("Transition Settings")]
        [SerializeField] private bool enableTransitions = true;
        [SerializeField] private float defaultTransitionSpeed = 1f;

        [Header("State Control")]
        [SerializeField] private bool startWithIdle = true;

        // References
        private Image targetImage;
        private GifAsset currentGif;

        // State
        private bool isInitialized = false;

        /// <summary>
        /// Gets or sets the GIF asset to play
        /// </summary>
        public GifAsset GifAsset
        {
            get => gifAsset;
            set
            {
                if (gifAsset != value)
                {
                    gifAsset = value;
                    InitializeGif();
                }
            }
        }

        /// <summary>
        /// Gets whether the GIF is currently playing
        /// </summary>
        public bool IsPlaying => gifAsset != null && gifAsset.IsPlaying;

        /// <summary>
        /// Gets the current frame index
        /// </summary>
        public int CurrentFrameIndex => gifAsset?.GetCurrentFrameIndex() ?? 0;

        /// <summary>
        /// Gets the total frame count
        /// </summary>
        public int FrameCount => gifAsset?.FrameCount ?? 0;

        /// <summary>
        /// Gets the current frame sprite
        /// </summary>
        public Sprite CurrentFrame => gifAsset?.CurrentFrame;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null)
            {
                Debug.LogError("GifPlayer requires an Image component!");
                return;
            }

            isInitialized = true;

            if (playOnAwake && gifAsset != null)
            {
                InitializeGif();
            }
        }

        private void Start()
        {
            if (startWithIdle && gifAsset != null && gifAsset.IdleTransition != null)
            {
                SwitchToGif(gifAsset.IdleTransition);
            }
        }

        private void Update()
        {
            if (!isInitialized || !autoUpdate)
                return;

            UpdateGif();
        }

        private void OnDestroy()
        {
            if (gifAsset != null)
            {
                gifAsset.Stop();
            }
        }

        /// <summary>
        /// Initialize the GIF player with the current GIF asset
        /// </summary>
        private void InitializeGif()
        {
            if (gifAsset == null || targetImage == null)
                return;

            currentGif = gifAsset;

            if (playOnAwake)
            {
                gifAsset.Play();
            }

            UpdateDisplay();
        }

        /// <summary>
        /// Updates the GIF animation and handles transitions
        /// </summary>
        private void UpdateGif()
        {
            if (gifAsset == null)
                return;

            // Update transitions first
            if (enableTransitions)
            {
                gifAsset.UpdateTransition();
            }

            // Update animation
            gifAsset.UpdateAnimation();

            // Update display
            UpdateDisplay();
        }

        /// <summary>
        /// Updates the displayed sprite
        /// </summary>
        private void UpdateDisplay()
        {
            if (targetImage == null || gifAsset == null)
                return;

            var displaySprite = gifAsset.GetInterpolatedFrame();
            if (displaySprite != null && targetImage.sprite != displaySprite)
            {
                targetImage.sprite = displaySprite;
            }
        }

        /// <summary>
        /// Start playing the GIF
        /// </summary>
        public void Play()
        {
            if (gifAsset == null) return;
            gifAsset.Play();
        }

        /// <summary>
        /// Stop playing the GIF
        /// </summary>
        public void Stop()
        {
            if (gifAsset == null) return;
            gifAsset.Stop();
        }

        /// <summary>
        /// Pause the GIF animation
        /// </summary>
        public void Pause()
        {
            if (gifAsset == null) return;
            gifAsset.Pause();
        }

        /// <summary>
        /// Resume the GIF animation
        /// </summary>
        public void Resume()
        {
            if (gifAsset == null) return;
            gifAsset.Resume();
        }

        /// <summary>
        /// Switch to a different GIF asset with optional transition
        /// </summary>
        public void SwitchToGif(GifAsset newGif, float transitionSpeed = -1f)
        {
            if (newGif == null || newGif == gifAsset)
                return;

            if (enableTransitions && gifAsset != null && gifAsset.CanTransitionTo(newGif))
            {
                // Use smooth transition
                float speed = transitionSpeed > 0f ? transitionSpeed : defaultTransitionSpeed;
                gifAsset.TransitionTo(newGif, speed);
            }
            else
            {
                // Immediate switch
                gifAsset = newGif;
                InitializeGif();
            }
        }

        /// <summary>
        /// Switch to the idle animation
        /// </summary>
        public void SwitchToIdle()
        {
            if (gifAsset?.IdleTransition != null)
            {
                SwitchToGif(gifAsset.IdleTransition);
            }
        }

        /// <summary>
        /// Switch to the talking animation
        /// </summary>
        public void SwitchToTalking()
        {
            if (gifAsset?.TalkingTransition != null)
            {
                SwitchToGif(gifAsset.TalkingTransition);
            }
        }

        /// <summary>
        /// Set the current frame index
        /// </summary>
        public void SetCurrentFrameIndex(int frameIndex)
        {
            if (gifAsset == null) return;
            gifAsset.SetCurrentFrameIndex(frameIndex);
            UpdateDisplay();
        }

        /// <summary>
        /// Go to the next frame
        /// </summary>
        public void NextFrame()
        {
            if (gifAsset == null) return;
            int nextFrame = (gifAsset.GetCurrentFrameIndex() + 1) % gifAsset.FrameCount;
            SetCurrentFrameIndex(nextFrame);
        }

        /// <summary>
        /// Go to the previous frame
        /// </summary>
        public void PreviousFrame()
        {
            if (gifAsset == null) return;
            int prevFrame = gifAsset.GetCurrentFrameIndex() - 1;
            if (prevFrame < 0) prevFrame = gifAsset.FrameCount - 1;
            SetCurrentFrameIndex(prevFrame);
        }

        /// <summary>
        /// Gets the duration of the current GIF
        /// </summary>
        public float GetDuration()
        {
            return gifAsset?.GetDuration() ?? 0f;
        }

        /// <summary>
        /// Creates a new GifAsset from an array of sprites
        /// </summary>
        public static GifAsset CreateGifAssetFromSprites(Sprite[] sprites, float frameRate = 12f, string assetName = "NewGifAsset")
        {
            var gifAsset = ScriptableObject.CreateInstance<GifAsset>();
            gifAsset.SetFrames(new System.Collections.Generic.List<Sprite>(sprites));
            gifAsset.name = assetName;
            return gifAsset;
        }

        /// <summary>
        /// Creates a new GifAsset from texture files
        /// </summary>
        public static GifAsset CreateGifAssetFromTextures(Texture2D[] textures, float frameRate = 12f, string assetName = "NewGifAsset")
        {
            var sprites = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                sprites[i] = Sprite.Create(textures[i], new Rect(0, 0, textures[i].width, textures[i].height), Vector2.one * 0.5f);
            }

            return CreateGifAssetFromSprites(sprites, frameRate, assetName);
        }
    }
}