using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unbound.Dialogue
{
    /// <summary>
    /// ScriptableObject for storing and managing GIF animation data
    /// Provides frame-based animation with configurable playback settings
    /// </summary>
    [CreateAssetMenu(fileName = "NewGifAsset", menuName = "Unbound/Dialogue/GifAsset")]
    public class GifAsset : ScriptableObject
    {
        [Header("Animation Settings")]
        [SerializeField] private List<Sprite> frames = new List<Sprite>();
        [SerializeField] private float frameRate = 12f; // Frames per second
        [SerializeField] private bool loop = true;
        [SerializeField] private bool playOnAwake = true;

        [Header("Transition Settings")]
        [SerializeField] private GifAsset idleTransition;
        [SerializeField] private GifAsset talkingTransition;
        [SerializeField] private float transitionSpeed = 1f;

        // Runtime state
        private int currentFrameIndex = 0;
        private float frameTimer = 0f;
        private bool isPlaying = false;
        private bool isPaused = false;

        // Transition state
        private GifAsset transitionTarget;
        private float transitionProgress = 0f;
        private bool isTransitioning = false;

        public List<Sprite> Frames => frames;
        public int FrameCount => frames.Count;

        public float FrameRate
        {
            get => frameRate;
            set => frameRate = value;
        }

        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        public bool PlayOnAwake
        {
            get => playOnAwake;
            set => playOnAwake = value;
        }

        public GifAsset IdleTransition
        {
            get => idleTransition;
            set => idleTransition = value;
        }

        public GifAsset TalkingTransition
        {
            get => talkingTransition;
            set => talkingTransition = value;
        }

        public float TransitionSpeed
        {
            get => transitionSpeed;
            set => transitionSpeed = value;
        }

        public Sprite CurrentFrame => frames.Count > 0 ? frames[currentFrameIndex] : null;
        public bool IsPlaying => isPlaying;
        public bool IsPaused => isPaused;
        public bool IsTransitioning => isTransitioning;

        private void OnEnable()
        {
            Reset();
        }

        /// <summary>
        /// Resets the animation to the first frame
        /// </summary>
        public void Reset()
        {
            currentFrameIndex = 0;
            frameTimer = 0f;
            isPlaying = false;
            isPaused = false;
            transitionProgress = 0f;
            isTransitioning = false;
            transitionTarget = null;
        }

        /// <summary>
        /// Starts playing the GIF animation
        /// </summary>
        public void Play()
        {
            isPlaying = true;
            isPaused = false;
        }

        /// <summary>
        /// Stops the GIF animation and resets to the first frame
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
            isPaused = false;
            Reset();
        }

        /// <summary>
        /// Pauses the GIF animation
        /// </summary>
        public void Pause()
        {
            isPaused = true;
        }

        /// <summary>
        /// Resumes the GIF animation from where it was paused
        /// </summary>
        public void Resume()
        {
            isPaused = false;
        }

        /// <summary>
        /// Updates the animation frame based on delta time
        /// Call this in Update() or a coroutine
        /// </summary>
        public void UpdateAnimation()
        {
            if (!isPlaying || isPaused || frames.Count == 0)
                return;

            frameTimer += Time.deltaTime;

            // Calculate how many frames we should advance
            float frameDuration = 1f / frameRate;
            int framesToAdvance = Mathf.FloorToInt(frameTimer / frameDuration);

            if (framesToAdvance > 0)
            {
                currentFrameIndex += framesToAdvance;

                if (loop)
                {
                    currentFrameIndex %= frames.Count;
                }
                else if (currentFrameIndex >= frames.Count)
                {
                    currentFrameIndex = frames.Count - 1;
                    isPlaying = false; // Stop at the last frame if not looping
                }

                frameTimer = 0f;
            }
        }

        /// <summary>
        /// Gets the current frame index
        /// </summary>
        public int GetCurrentFrameIndex()
        {
            return currentFrameIndex;
        }

        /// <summary>
        /// Sets the current frame index (for seeking)
        /// </summary>
        public void SetCurrentFrameIndex(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                currentFrameIndex = index;
                frameTimer = 0f;
            }
        }

        /// <summary>
        /// Sets the frames for this GIF asset
        /// </summary>
        public void SetFrames(List<Sprite> newFrames)
        {
            frames = new List<Sprite>(newFrames);
            Reset();
        }

        /// <summary>
        /// Adds a frame to the GIF
        /// </summary>
        public void AddFrame(Sprite frame)
        {
            frames.Add(frame);
        }

        /// <summary>
        /// Removes a frame from the GIF
        /// </summary>
        public void RemoveFrame(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                frames.RemoveAt(index);
                if (currentFrameIndex >= frames.Count && frames.Count > 0)
                {
                    currentFrameIndex = frames.Count - 1;
                }
            }
        }

        /// <summary>
        /// Gets a frame at the specified index
        /// </summary>
        public Sprite GetFrame(int index)
        {
            if (index >= 0 && index < frames.Count)
            {
                return frames[index];
            }
            return null;
        }

        /// <summary>
        /// Transitions to another GIF asset with smooth interpolation
        /// </summary>
        public void TransitionTo(GifAsset targetGif, float speed = 1f)
        {
            if (targetGif == null || targetGif == this)
                return;

            transitionTarget = targetGif;
            transitionProgress = 0f;
            isTransitioning = true;
            transitionSpeed = speed;
        }

        /// <summary>
        /// Updates the transition progress
        /// Call this in Update() along with UpdateAnimation()
        /// </summary>
        public void UpdateTransition()
        {
            if (!isTransitioning || transitionTarget == null)
                return;

            transitionProgress += Time.deltaTime * transitionSpeed;

            if (transitionProgress >= 1f)
            {
                // Transition complete - switch to target GIF
                transitionProgress = 1f;
                isTransitioning = false;

                // Copy the target's current state
                frames = new List<Sprite>(transitionTarget.frames);
                currentFrameIndex = transitionTarget.currentFrameIndex;
                frameRate = transitionTarget.frameRate;
                loop = transitionTarget.loop;
            }
        }

        /// <summary>
        /// Gets the interpolated frame during transition
        /// This should be called by the GifPlayer to get the current display frame
        /// </summary>
        public Sprite GetInterpolatedFrame()
        {
            if (!isTransitioning || transitionTarget == null)
            {
                return CurrentFrame;
            }

            // For now, return the current frame - in a full implementation,
            // you might want to blend between frames of the two GIFs
            return CurrentFrame;
        }

        /// <summary>
        /// Gets the total duration of the GIF in seconds
        /// </summary>
        public float GetDuration()
        {
            if (frames.Count == 0) return 0f;
            return frames.Count / frameRate;
        }

        /// <summary>
        /// Checks if this GIF can transition to another GIF
        /// </summary>
        public bool CanTransitionTo(GifAsset target)
        {
            return target != null && target != this && target.frames.Count > 0;
        }

        /// <summary>
        /// Creates a copy of this GifAsset with the same settings
        /// </summary>
        public GifAsset Clone()
        {
            var clone = CreateInstance<GifAsset>();
            clone.frames = new List<Sprite>(frames);
            clone.frameRate = frameRate;
            clone.loop = loop;
            clone.playOnAwake = playOnAwake;
            clone.idleTransition = idleTransition;
            clone.talkingTransition = talkingTransition;
            clone.transitionSpeed = transitionSpeed;
            return clone;
        }
    }
}
