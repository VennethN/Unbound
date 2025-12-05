using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Central audio manager singleton that coordinates all audio playback
    /// Handles music, SFX, ambient, UI sounds, and voice with volume controls
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;

        [Header("Audio Settings")]
        [SerializeField] private AudioSettings audioSettings = new AudioSettings();
        [SerializeField] private bool loadSettingsOnAwake = true;
        [SerializeField] private bool saveSettingsOnDestroy = true;

        [Header("Audio Source Pool")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int maxPoolSize = 50;

        [Header("Music Settings")]
        [SerializeField] private float defaultMusicFadeDuration = 1f;

        // Audio source pools
        private List<AudioSource> _sfxPool = new List<AudioSource>();
        private List<AudioSource> _ambientSources = new List<AudioSource>();
        private AudioSource _musicSource;
        private AudioSource _musicSourceSecondary; // For crossfading
        private AudioSource _uiSource;

        // Active audio instances
        private Dictionary<string, AudioInstance> _activeInstances = new Dictionary<string, AudioInstance>();
        private Dictionary<string, Coroutine> _fadeCoroutines = new Dictionary<string, Coroutine>();

        // Current music state
        private string _currentMusicID;
        private bool _isMusicCrossfading;

        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public AudioSettings Settings => audioSettings;
        public string CurrentMusicID => _currentMusicID;
        public bool IsMusicPlaying => _musicSource != null && _musicSource.isPlaying;

        // Events
        public event Action<string> OnMusicStarted;
        public event Action<string> OnMusicStopped;
        public event Action<string> OnSFXPlayed;
        public event Action OnSettingsChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();

            if (loadSettingsOnAwake && AudioSettings.HasSavedSettings())
            {
                audioSettings.LoadFromPlayerPrefs();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this && saveSettingsOnDestroy)
            {
                audioSettings.SaveToPlayerPrefs();
            }
        }

        /// <summary>
        /// Initializes all audio source components
        /// </summary>
        private void InitializeAudioSources()
        {
            // Create music sources
            GameObject musicObj = new GameObject("MusicSource");
            musicObj.transform.SetParent(transform);
            _musicSource = musicObj.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.priority = 0;

            GameObject musicObj2 = new GameObject("MusicSourceSecondary");
            musicObj2.transform.SetParent(transform);
            _musicSourceSecondary = musicObj2.AddComponent<AudioSource>();
            _musicSourceSecondary.playOnAwake = false;
            _musicSourceSecondary.loop = true;
            _musicSourceSecondary.priority = 0;

            // Create UI source
            GameObject uiObj = new GameObject("UIAudioSource");
            uiObj.transform.SetParent(transform);
            _uiSource = uiObj.AddComponent<AudioSource>();
            _uiSource.playOnAwake = false;
            _uiSource.loop = false;
            _uiSource.priority = 64;

            // Initialize SFX pool
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledAudioSource();
            }
        }

        /// <summary>
        /// Creates a new pooled audio source for SFX
        /// </summary>
        private AudioSource CreatePooledAudioSource()
        {
            GameObject sfxObj = new GameObject($"SFXSource_{_sfxPool.Count}");
            sfxObj.transform.SetParent(transform);
            AudioSource source = sfxObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            _sfxPool.Add(source);
            return source;
        }

        /// <summary>
        /// Gets an available audio source from the pool
        /// </summary>
        private AudioSource GetPooledAudioSource()
        {
            foreach (AudioSource source in _sfxPool)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            // Expand pool if needed
            if (_sfxPool.Count < maxPoolSize)
            {
                return CreatePooledAudioSource();
            }

            // Return the one closest to finishing
            AudioSource oldest = _sfxPool[0];
            float longestTime = 0f;
            foreach (AudioSource source in _sfxPool)
            {
                if (source.time > longestTime)
                {
                    longestTime = source.time;
                    oldest = source;
                }
            }

            oldest.Stop();
            return oldest;
        }

        #region Music Playback

        /// <summary>
        /// Plays background music with optional crossfade
        /// </summary>
        public void PlayMusic(string clipID, bool crossfade = true, float fadeDuration = -1f)
        {
            if (string.IsNullOrEmpty(clipID))
            {
                StopMusic(crossfade, fadeDuration);
                return;
            }

            if (clipID == _currentMusicID && _musicSource.isPlaying)
                return;

            AudioClipData clipData = AudioDatabase.Instance.GetClipData(clipID);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"Cannot play music: clip '{clipID}' not found");
                return;
            }

            if (fadeDuration < 0f)
                fadeDuration = defaultMusicFadeDuration;

            if (crossfade && _musicSource.isPlaying && fadeDuration > 0f)
            {
                StartCoroutine(CrossfadeMusic(clipData, fadeDuration));
            }
            else
            {
                _musicSource.clip = clipData.clip;
                _musicSource.volume = GetMusicVolume(clipData);
                _musicSource.pitch = clipData.defaultPitch;
                _musicSource.loop = clipData.loop;
                _musicSource.Play();
            }

            _currentMusicID = clipID;
            OnMusicStarted?.Invoke(clipID);
        }

        /// <summary>
        /// Crossfades between current music and new music
        /// </summary>
        private IEnumerator CrossfadeMusic(AudioClipData newClip, float duration)
        {
            _isMusicCrossfading = true;

            // Setup secondary source with new music
            _musicSourceSecondary.clip = newClip.clip;
            _musicSourceSecondary.volume = 0f;
            _musicSourceSecondary.pitch = newClip.defaultPitch;
            _musicSourceSecondary.loop = newClip.loop;
            _musicSourceSecondary.Play();

            float targetVolume = GetMusicVolume(newClip);
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                _musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                _musicSourceSecondary.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = null;

            // Swap sources
            (_musicSource, _musicSourceSecondary) = (_musicSourceSecondary, _musicSource);

            _isMusicCrossfading = false;
        }

        /// <summary>
        /// Stops the currently playing music
        /// </summary>
        public void StopMusic(bool fade = true, float fadeDuration = -1f)
        {
            if (!_musicSource.isPlaying)
                return;

            if (fadeDuration < 0f)
                fadeDuration = defaultMusicFadeDuration;

            if (fade && fadeDuration > 0f)
            {
                StartCoroutine(FadeOutMusic(fadeDuration));
            }
            else
            {
                _musicSource.Stop();
            }

            string stoppedID = _currentMusicID;
            _currentMusicID = null;
            OnMusicStopped?.Invoke(stoppedID);
        }

        /// <summary>
        /// Fades out the current music
        /// </summary>
        private IEnumerator FadeOutMusic(float duration)
        {
            float startVolume = _musicSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.volume = startVolume;
        }

        /// <summary>
        /// Pauses the current music
        /// </summary>
        public void PauseMusic()
        {
            _musicSource.Pause();
        }

        /// <summary>
        /// Resumes paused music
        /// </summary>
        public void ResumeMusic()
        {
            _musicSource.UnPause();
        }

        /// <summary>
        /// Gets the effective music volume for a clip
        /// </summary>
        private float GetMusicVolume(AudioClipData clipData)
        {
            return clipData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.Music);
        }

        #endregion

        #region SFX Playback

        /// <summary>
        /// Plays a sound effect
        /// </summary>
        public AudioInstance PlaySFX(string clipID, Vector3? position = null)
        {
            AudioClipData clipData = AudioDatabase.Instance.GetClipData(clipID);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"Cannot play SFX: clip '{clipID}' not found");
                return null;
            }

            return PlaySFX(clipData, position);
        }

        /// <summary>
        /// Plays a sound effect from clip data
        /// </summary>
        public AudioInstance PlaySFX(AudioClipData clipData, Vector3? position = null)
        {
            if (clipData == null || clipData.clip == null)
                return null;

            AudioSource source = GetPooledAudioSource();

            // Configure source
            source.clip = clipData.clip;
            source.volume = clipData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.SFX);
            source.pitch = clipData.defaultPitch + UnityEngine.Random.Range(-clipData.pitchVariation, clipData.pitchVariation);
            source.loop = clipData.loop;
            source.priority = clipData.priority;
            source.spatialBlend = clipData.spatialBlend;
            source.minDistance = clipData.minDistance;
            source.maxDistance = clipData.maxDistance;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
            }

            source.Play();

            AudioInstance instance = new AudioInstance
            {
                clipID = clipData.clipID,
                source = source,
                startTime = Time.time,
                originalVolume = source.volume
            };

            OnSFXPlayed?.Invoke(clipData.clipID);
            return instance;
        }

        /// <summary>
        /// Plays a one-shot sound effect (fire and forget, no tracking)
        /// </summary>
        public void PlaySFXOneShot(string clipID, Vector3? position = null)
        {
            AudioClipData clipData = AudioDatabase.Instance.GetClipData(clipID);
            if (clipData == null || clipData.clip == null)
                return;

            AudioSource source = GetPooledAudioSource();

            if (position.HasValue)
            {
                source.transform.position = position.Value;
            }

            float volume = clipData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.SFX);
            source.pitch = clipData.defaultPitch + UnityEngine.Random.Range(-clipData.pitchVariation, clipData.pitchVariation);
            source.spatialBlend = clipData.spatialBlend;
            source.PlayOneShot(clipData.clip, volume);

            OnSFXPlayed?.Invoke(clipData.clipID);
        }

        /// <summary>
        /// Plays a sound effect directly from an AudioClip
        /// </summary>
        public void PlaySFXDirect(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            AudioSource source = GetPooledAudioSource();
            source.pitch = pitch;
            source.PlayOneShot(clip, volume * audioSettings.GetEffectiveVolume(AudioCategory.SFX));
        }

        #endregion

        #region UI Audio

        /// <summary>
        /// Plays a UI sound
        /// </summary>
        public void PlayUI(string clipID)
        {
            AudioClipData clipData = AudioDatabase.Instance.GetClipData(clipID);
            if (clipData == null || clipData.clip == null)
                return;

            float volume = clipData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.UI);
            _uiSource.pitch = clipData.defaultPitch;
            _uiSource.PlayOneShot(clipData.clip, volume);
        }

        /// <summary>
        /// Plays a UI sound directly from an AudioClip
        /// </summary>
        public void PlayUIDirect(AudioClip clip, float volume = 1f)
        {
            if (clip == null)
                return;

            _uiSource.PlayOneShot(clip, volume * audioSettings.GetEffectiveVolume(AudioCategory.UI));
        }

        #endregion

        #region Ambient Audio

        /// <summary>
        /// Starts an ambient sound loop
        /// </summary>
        public AudioInstance StartAmbient(string clipID, float fadeInDuration = 0f)
        {
            AudioClipData clipData = AudioDatabase.Instance.GetClipData(clipID);
            if (clipData == null || clipData.clip == null)
            {
                Debug.LogWarning($"Cannot start ambient: clip '{clipID}' not found");
                return null;
            }

            // Check if already playing
            if (_activeInstances.TryGetValue(clipID, out AudioInstance existing) && existing.IsPlaying)
            {
                return existing;
            }

            // Create or reuse ambient source
            AudioSource source = GetOrCreateAmbientSource();

            source.clip = clipData.clip;
            source.loop = true;
            source.priority = clipData.priority;
            source.spatialBlend = clipData.spatialBlend;

            float targetVolume = clipData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.Ambient);

            if (fadeInDuration > 0f)
            {
                source.volume = 0f;
                source.Play();
                StartCoroutine(FadeAudioSource(source, targetVolume, fadeInDuration));
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }

            AudioInstance instance = new AudioInstance
            {
                clipID = clipID,
                source = source,
                startTime = Time.time,
                originalVolume = targetVolume
            };

            _activeInstances[clipID] = instance;
            return instance;
        }

        /// <summary>
        /// Stops an ambient sound
        /// </summary>
        public void StopAmbient(string clipID, float fadeOutDuration = 0f)
        {
            if (!_activeInstances.TryGetValue(clipID, out AudioInstance instance))
                return;

            if (fadeOutDuration > 0f && instance.source != null)
            {
                StartCoroutine(FadeOutAndStop(instance.source, fadeOutDuration));
            }
            else if (instance.source != null)
            {
                instance.source.Stop();
            }

            _activeInstances.Remove(clipID);
        }

        /// <summary>
        /// Stops all ambient sounds
        /// </summary>
        public void StopAllAmbient(float fadeOutDuration = 0f)
        {
            List<string> ambientIDs = new List<string>();
            foreach (var kvp in _activeInstances)
            {
                AudioClipData data = AudioDatabase.Instance.GetClipData(kvp.Key);
                if (data != null && data.category == AudioCategory.Ambient)
                {
                    ambientIDs.Add(kvp.Key);
                }
            }

            foreach (string id in ambientIDs)
            {
                StopAmbient(id, fadeOutDuration);
            }
        }

        /// <summary>
        /// Gets or creates an ambient audio source
        /// </summary>
        private AudioSource GetOrCreateAmbientSource()
        {
            foreach (AudioSource source in _ambientSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            GameObject ambObj = new GameObject($"AmbientSource_{_ambientSources.Count}");
            ambObj.transform.SetParent(transform);
            AudioSource newSource = ambObj.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            _ambientSources.Add(newSource);
            return newSource;
        }

        #endregion

        #region Volume Controls

        /// <summary>
        /// Sets the master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            audioSettings.masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Sets the volume for a specific category
        /// </summary>
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            audioSettings.SetCategoryVolume(category, volume);
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Mutes or unmutes all audio
        /// </summary>
        public void SetMuteAll(bool mute)
        {
            audioSettings.muteAll = mute;
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Mutes or unmutes a specific category
        /// </summary>
        public void SetCategoryMuted(AudioCategory category, bool muted)
        {
            audioSettings.SetCategoryMuted(category, muted);
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Applies volume changes to all active audio sources
        /// </summary>
        private void ApplyVolumeChanges()
        {
            // Update music volume
            if (_musicSource != null && _musicSource.isPlaying && !string.IsNullOrEmpty(_currentMusicID))
            {
                AudioClipData musicData = AudioDatabase.Instance.GetClipData(_currentMusicID);
                if (musicData != null)
                {
                    _musicSource.volume = musicData.defaultVolume * audioSettings.GetEffectiveVolume(AudioCategory.Music);
                }
            }

            // Update ambient volumes
            foreach (var kvp in _activeInstances)
            {
                AudioClipData data = AudioDatabase.Instance.GetClipData(kvp.Key);
                if (data != null && kvp.Value.source != null)
                {
                    kvp.Value.source.volume = data.defaultVolume * audioSettings.GetEffectiveVolume(data.category);
                }
            }
        }

        /// <summary>
        /// Saves current audio settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            audioSettings.SaveToPlayerPrefs();
        }

        /// <summary>
        /// Loads audio settings from PlayerPrefs
        /// </summary>
        public void LoadSettings()
        {
            audioSettings.LoadFromPlayerPrefs();
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Resets all settings to defaults
        /// </summary>
        public void ResetSettings()
        {
            audioSettings.ResetToDefaults();
            ApplyVolumeChanges();
            OnSettingsChanged?.Invoke();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Stops a specific audio instance
        /// </summary>
        public void StopInstance(AudioInstance instance, float fadeOutDuration = 0f)
        {
            if (instance == null || instance.source == null)
                return;

            if (fadeOutDuration > 0f)
            {
                StartCoroutine(FadeOutAndStop(instance.source, fadeOutDuration));
            }
            else
            {
                instance.source.Stop();
            }

            if (_activeInstances.ContainsKey(instance.clipID))
            {
                _activeInstances.Remove(instance.clipID);
            }
        }

        /// <summary>
        /// Stops all audio
        /// </summary>
        public void StopAll()
        {
            StopMusic(false);
            StopAllAmbient();

            foreach (AudioSource source in _sfxPool)
            {
                source.Stop();
            }

            _activeInstances.Clear();
        }

        /// <summary>
        /// Pauses all audio
        /// </summary>
        public void PauseAll()
        {
            _musicSource.Pause();
            _musicSourceSecondary.Pause();

            foreach (AudioSource source in _sfxPool)
            {
                source.Pause();
            }

            foreach (AudioSource source in _ambientSources)
            {
                source.Pause();
            }
        }

        /// <summary>
        /// Resumes all paused audio
        /// </summary>
        public void ResumeAll()
        {
            _musicSource.UnPause();
            _musicSourceSecondary.UnPause();

            foreach (AudioSource source in _sfxPool)
            {
                source.UnPause();
            }

            foreach (AudioSource source in _ambientSources)
            {
                source.UnPause();
            }
        }

        /// <summary>
        /// Fades an audio source to a target volume
        /// </summary>
        private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }

        /// <summary>
        /// Fades out and stops an audio source
        /// </summary>
        private IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;
        }

        /// <summary>
        /// Checks if a specific clip is currently playing
        /// </summary>
        public bool IsPlaying(string clipID)
        {
            if (_activeInstances.TryGetValue(clipID, out AudioInstance instance))
            {
                return instance.IsPlaying;
            }

            return clipID == _currentMusicID && _musicSource.isPlaying;
        }

        #endregion
    }
}

