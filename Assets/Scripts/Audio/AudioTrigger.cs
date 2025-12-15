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
}