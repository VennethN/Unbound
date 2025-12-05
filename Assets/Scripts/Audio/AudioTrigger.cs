using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Plays audio when triggered by collision or manually
    /// Useful for environmental sounds, footsteps, etc.
    /// </summary>
    public class AudioTrigger : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private string clipID;
        [SerializeField] private AudioClip clip;
        [SerializeField] private AudioCategory category = AudioCategory.SFX;

        [Header("Playback Settings")]
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private bool playOnEnable = false;
        [SerializeField] private bool playOnTriggerEnter = false;
        [SerializeField] private bool playOnCollisionEnter = false;
        [SerializeField] private bool use3DPositioning = true;
        [SerializeField] private bool loop = false;

        [Header("Trigger Settings")]
        [SerializeField] private string requiredTag = "Player";
        [SerializeField] private bool onlyPlayOnce = false;
        [SerializeField] private float cooldown = 0f;

        private bool _hasPlayed;
        private float _lastPlayTime;
        private AudioInstance _loopingInstance;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable && !playOnStart)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            StopLoop();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (playOnTriggerEnter && IsValidTrigger(other.gameObject))
            {
                Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playOnTriggerEnter && IsValidTrigger(other.gameObject))
            {
                Play();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (playOnCollisionEnter && IsValidTrigger(collision.gameObject))
            {
                Play();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (playOnCollisionEnter && IsValidTrigger(collision.gameObject))
            {
                Play();
            }
        }

        /// <summary>
        /// Checks if this is a valid trigger source
        /// </summary>
        private bool IsValidTrigger(GameObject other)
        {
            if (onlyPlayOnce && _hasPlayed)
                return false;

            if (cooldown > 0f && Time.time - _lastPlayTime < cooldown)
                return false;

            if (!string.IsNullOrEmpty(requiredTag) && !other.CompareTag(requiredTag))
                return false;

            return true;
        }

        /// <summary>
        /// Plays the audio
        /// </summary>
        public void Play()
        {
            if (onlyPlayOnce && _hasPlayed)
                return;

            if (cooldown > 0f && Time.time - _lastPlayTime < cooldown)
                return;

            _hasPlayed = true;
            _lastPlayTime = Time.time;

            Vector3? position = use3DPositioning ? transform.position : null;

            if (loop)
            {
                PlayLoop(position);
            }
            else
            {
                PlayOneShot(position);
            }
        }

        /// <summary>
        /// Plays a one-shot sound
        /// </summary>
        private void PlayOneShot(Vector3? position)
        {
            if (clip != null)
            {
                AudioManager.Instance.PlaySFXDirect(clip);
            }
            else if (!string.IsNullOrEmpty(clipID))
            {
                switch (category)
                {
                    case AudioCategory.Music:
                        AudioManager.Instance.PlayMusic(clipID);
                        break;
                    case AudioCategory.SFX:
                        AudioManager.Instance.PlaySFXOneShot(clipID, position);
                        break;
                    case AudioCategory.Ambient:
                        AudioManager.Instance.StartAmbient(clipID);
                        break;
                    case AudioCategory.UI:
                        AudioManager.Instance.PlayUI(clipID);
                        break;
                    default:
                        AudioManager.Instance.PlaySFXOneShot(clipID, position);
                        break;
                }
            }
        }

        /// <summary>
        /// Starts a looping sound
        /// </summary>
        private void PlayLoop(Vector3? position)
        {
            StopLoop();

            if (!string.IsNullOrEmpty(clipID))
            {
                _loopingInstance = AudioManager.Instance.StartAmbient(clipID);
            }
        }

        /// <summary>
        /// Stops a looping sound
        /// </summary>
        public void StopLoop()
        {
            if (_loopingInstance != null)
            {
                AudioManager.Instance.StopInstance(_loopingInstance, 0.5f);
                _loopingInstance = null;
            }
        }

        /// <summary>
        /// Stops the audio and resets state
        /// </summary>
        public void Stop()
        {
            StopLoop();
        }

        /// <summary>
        /// Resets the trigger to allow playing again
        /// </summary>
        public void ResetTrigger()
        {
            _hasPlayed = false;
            _lastPlayTime = 0f;
        }

        /// <summary>
        /// Sets the clip ID at runtime
        /// </summary>
        public void SetClipID(string newClipID)
        {
            clipID = newClipID;
        }
    }

    /// <summary>
    /// Component for playing footstep sounds based on player movement
    /// </summary>
    public class FootstepAudio : MonoBehaviour
    {
        [Header("Footstep Settings")]
        [SerializeField] private string footstepGroupID = "footsteps";
        [SerializeField] private float stepInterval = 0.4f;
        [SerializeField] private float runningMultiplier = 0.6f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.1f;

        [Header("Surface Types")]
        [SerializeField] private SurfaceType defaultSurface = SurfaceType.Stone;
        [SerializeField] private bool detectSurfaceType = false;

        private float _stepTimer;
        private bool _isMoving;
        private bool _isRunning;
        private Vector3 _lastPosition;

        public enum SurfaceType
        {
            Stone,
            Wood,
            Grass,
            Water,
            Metal,
            Dirt
        }

        private void Update()
        {
            // Check if moving
            Vector3 movement = transform.position - _lastPosition;
            movement.y = 0f;
            _isMoving = movement.sqrMagnitude > 0.0001f;
            _lastPosition = transform.position;

            if (_isMoving && IsGrounded())
            {
                float interval = _isRunning ? stepInterval * runningMultiplier : stepInterval;

                _stepTimer += Time.deltaTime;
                if (_stepTimer >= interval)
                {
                    _stepTimer = 0f;
                    PlayFootstep();
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        /// <summary>
        /// Checks if the character is grounded
        /// </summary>
        private bool IsGrounded()
        {
            return Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer) ||
                   Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
        }

        /// <summary>
        /// Plays a footstep sound
        /// </summary>
        private void PlayFootstep()
        {
            string groupID = footstepGroupID;

            if (detectSurfaceType)
            {
                SurfaceType surface = DetectSurface();
                groupID = $"{footstepGroupID}_{surface.ToString().ToLower()}";
            }

            if (SFXController.Instance != null)
            {
                SFXController.Instance.PlayFromGroup(groupID, transform.position);
            }
            else
            {
                AudioManager.Instance?.PlaySFXOneShot(groupID, transform.position);
            }
        }

        /// <summary>
        /// Detects the current surface type
        /// </summary>
        private SurfaceType DetectSurface()
        {
            // Try 2D raycast first
            RaycastHit2D hit2D = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance * 2f, groundLayer);
            if (hit2D.collider != null)
            {
                SurfaceTag tag = hit2D.collider.GetComponent<SurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }
            }

            // Try 3D raycast
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance * 2f, groundLayer))
            {
                SurfaceTag tag = hit.collider.GetComponent<SurfaceTag>();
                if (tag != null)
                {
                    return tag.surfaceType;
                }
            }

            return defaultSurface;
        }

        /// <summary>
        /// Sets the running state
        /// </summary>
        public void SetRunning(bool running)
        {
            _isRunning = running;
        }

        /// <summary>
        /// Manually triggers a footstep sound
        /// </summary>
        public void TriggerFootstep()
        {
            PlayFootstep();
        }
    }

    /// <summary>
    /// Tag component to identify surface types for footstep sounds
    /// </summary>
    public class SurfaceTag : MonoBehaviour
    {
        public FootstepAudio.SurfaceType surfaceType = FootstepAudio.SurfaceType.Stone;
    }
}

