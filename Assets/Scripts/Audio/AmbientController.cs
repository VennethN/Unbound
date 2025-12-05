using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Specialized ambient audio controller for managing audio zones and environmental sounds
    /// </summary>
    public class AmbientController : MonoBehaviour
    {
        private static AmbientController _instance;

        [Header("Zone Settings")]
        [SerializeField] private float defaultFadeDuration = 1f;
        [SerializeField] private bool autoDetectZones = true;

        [Header("Current State")]
        [SerializeField] private string currentZoneID;
        [SerializeField] private List<string> activeAmbientClips = new List<string>();

        private Dictionary<string, AmbientZone> _zones = new Dictionary<string, AmbientZone>();
        private Dictionary<string, AudioInstance> _activeInstances = new Dictionary<string, AudioInstance>();
        private AmbientZone _currentZone;

        public static AmbientController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AmbientController>();
                }
                return _instance;
            }
        }

        public string CurrentZoneID => currentZoneID;
        public AmbientZone CurrentZone => _currentZone;

        public event Action<string, string> OnZoneChanged; // oldZone, newZone
        public event Action<string> OnAmbientStarted;
        public event Action<string> OnAmbientStopped;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            if (autoDetectZones)
            {
                FindAllZones();
            }
        }

        #region Zone Management

        /// <summary>
        /// Finds all AmbientZone components in the scene
        /// </summary>
        public void FindAllZones()
        {
            _zones.Clear();
            AmbientZone[] zones = FindObjectsByType<AmbientZone>(FindObjectsSortMode.None);

            foreach (AmbientZone zone in zones)
            {
                if (!string.IsNullOrEmpty(zone.zoneID))
                {
                    _zones[zone.zoneID] = zone;
                }
            }

            Debug.Log($"AmbientController: Found {_zones.Count} ambient zones");
        }

        /// <summary>
        /// Registers a zone manually
        /// </summary>
        public void RegisterZone(AmbientZone zone)
        {
            if (zone != null && !string.IsNullOrEmpty(zone.zoneID))
            {
                _zones[zone.zoneID] = zone;
            }
        }

        /// <summary>
        /// Unregisters a zone
        /// </summary>
        public void UnregisterZone(string zoneID)
        {
            if (_zones.ContainsKey(zoneID))
            {
                if (currentZoneID == zoneID)
                {
                    ExitZone(zoneID);
                }
                _zones.Remove(zoneID);
            }
        }

        /// <summary>
        /// Enters an ambient zone
        /// </summary>
        public void EnterZone(string zoneID)
        {
            if (zoneID == currentZoneID)
                return;

            if (!_zones.TryGetValue(zoneID, out AmbientZone zone))
            {
                Debug.LogWarning($"Ambient zone '{zoneID}' not found");
                return;
            }

            string oldZone = currentZoneID;

            // Exit current zone if any
            if (!string.IsNullOrEmpty(currentZoneID))
            {
                ExitZone(currentZoneID);
            }

            currentZoneID = zoneID;
            _currentZone = zone;

            // Start zone ambient sounds
            foreach (string clipID in zone.ambientClipIDs)
            {
                StartAmbient(clipID, zone.fadeDuration > 0f ? zone.fadeDuration : defaultFadeDuration);
            }

            OnZoneChanged?.Invoke(oldZone, zoneID);
        }

        /// <summary>
        /// Exits an ambient zone
        /// </summary>
        public void ExitZone(string zoneID)
        {
            if (zoneID != currentZoneID)
                return;

            if (_zones.TryGetValue(zoneID, out AmbientZone zone))
            {
                float fadeDuration = zone.fadeDuration > 0f ? zone.fadeDuration : defaultFadeDuration;

                foreach (string clipID in zone.ambientClipIDs)
                {
                    StopAmbient(clipID, fadeDuration);
                }
            }

            currentZoneID = null;
            _currentZone = null;
        }

        /// <summary>
        /// Transitions between two zones
        /// </summary>
        public void TransitionToZone(string newZoneID, float fadeDuration = -1f)
        {
            if (fadeDuration < 0f)
                fadeDuration = defaultFadeDuration;

            if (!string.IsNullOrEmpty(currentZoneID))
            {
                // Fade out current zone
                if (_zones.TryGetValue(currentZoneID, out AmbientZone oldZone))
                {
                    foreach (string clipID in oldZone.ambientClipIDs)
                    {
                        StopAmbient(clipID, fadeDuration);
                    }
                }
            }

            // Start new zone after fade
            StartCoroutine(DelayedZoneEnter(newZoneID, fadeDuration * 0.5f));
        }

        private IEnumerator DelayedZoneEnter(string zoneID, float delay)
        {
            yield return new WaitForSeconds(delay);
            EnterZone(zoneID);
        }

        #endregion

        #region Ambient Sound Playback

        /// <summary>
        /// Starts an ambient sound loop
        /// </summary>
        public void StartAmbient(string clipID, float fadeInDuration = 0f)
        {
            if (_activeInstances.ContainsKey(clipID))
                return;

            AudioInstance instance = AudioManager.Instance.StartAmbient(clipID, fadeInDuration);

            if (instance != null)
            {
                _activeInstances[clipID] = instance;
                activeAmbientClips.Add(clipID);
                OnAmbientStarted?.Invoke(clipID);
            }
        }

        /// <summary>
        /// Stops an ambient sound
        /// </summary>
        public void StopAmbient(string clipID, float fadeOutDuration = 0f)
        {
            if (!_activeInstances.ContainsKey(clipID))
                return;

            AudioManager.Instance.StopAmbient(clipID, fadeOutDuration);
            _activeInstances.Remove(clipID);
            activeAmbientClips.Remove(clipID);
            OnAmbientStopped?.Invoke(clipID);
        }

        /// <summary>
        /// Stops all ambient sounds
        /// </summary>
        public void StopAllAmbient(float fadeOutDuration = 0f)
        {
            List<string> toStop = new List<string>(_activeInstances.Keys);

            foreach (string clipID in toStop)
            {
                StopAmbient(clipID, fadeOutDuration);
            }

            currentZoneID = null;
            _currentZone = null;
        }

        /// <summary>
        /// Checks if an ambient clip is currently playing
        /// </summary>
        public bool IsAmbientPlaying(string clipID)
        {
            return _activeInstances.ContainsKey(clipID);
        }

        /// <summary>
        /// Gets all currently playing ambient clip IDs
        /// </summary>
        public List<string> GetActiveAmbientClips()
        {
            return new List<string>(activeAmbientClips);
        }

        #endregion

        #region One-Shot Ambient Sounds

        /// <summary>
        /// Plays a one-shot ambient sound (e.g., random bird chirp)
        /// </summary>
        public void PlayOneShotAmbient(string clipID, Vector3? position = null)
        {
            AudioManager.Instance.PlaySFXOneShot(clipID, position);
        }

        /// <summary>
        /// Starts random one-shot ambient sounds
        /// </summary>
        public Coroutine StartRandomAmbientSounds(List<string> clipIDs, float minInterval, float maxInterval, Vector3? position = null)
        {
            return StartCoroutine(RandomAmbientLoop(clipIDs, minInterval, maxInterval, position));
        }

        private IEnumerator RandomAmbientLoop(List<string> clipIDs, float minInterval, float maxInterval, Vector3? position)
        {
            while (true)
            {
                float waitTime = UnityEngine.Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);

                if (clipIDs.Count > 0)
                {
                    string clipID = clipIDs[UnityEngine.Random.Range(0, clipIDs.Count)];
                    PlayOneShotAmbient(clipID, position);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Defines an ambient audio zone that can be placed in the scene
    /// </summary>
    public class AmbientZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        public string zoneID;
        public List<string> ambientClipIDs = new List<string>();
        public float fadeDuration = 1f;

        [Header("Trigger Settings")]
        public bool useColliderTrigger = true;
        public string playerTag = "Player";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (useColliderTrigger && other.CompareTag(playerTag))
            {
                AmbientController.Instance?.EnterZone(zoneID);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (useColliderTrigger && other.CompareTag(playerTag))
            {
                AmbientController.Instance?.ExitZone(zoneID);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (useColliderTrigger && other.CompareTag(playerTag))
            {
                AmbientController.Instance?.EnterZone(zoneID);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (useColliderTrigger && other.CompareTag(playerTag))
            {
                AmbientController.Instance?.ExitZone(zoneID);
            }
        }

        private void OnEnable()
        {
            AmbientController.Instance?.RegisterZone(this);
        }

        private void OnDisable()
        {
            AmbientController.Instance?.UnregisterZone(zoneID);
        }
    }
}

