using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Singleton database that loads and manages audio clip data from JSON files
    /// </summary>
    public class AudioDatabase : MonoBehaviour
    {
        private static AudioDatabase _instance;

        [Header("Database Settings")]
        [SerializeField] private string audioDataPath = "Data/Audio";
        [SerializeField] private bool loadOnAwake = true;

        private Dictionary<string, AudioClipData> _audioClips = new Dictionary<string, AudioClipData>();
        private Dictionary<AudioCategory, List<AudioClipData>> _clipsByCategory = new Dictionary<AudioCategory, List<AudioClipData>>();

        public static AudioDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioDatabase>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioDatabase");
                        _instance = go.AddComponent<AudioDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public event Action OnDatabaseLoaded;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize category dictionary
            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
            {
                _clipsByCategory[category] = new List<AudioClipData>();
            }

            if (loadOnAwake)
            {
                LoadAllAudioData();
            }
        }

        /// <summary>
        /// Loads all audio data from JSON files in the configured path
        /// </summary>
        public void LoadAllAudioData()
        {
            _audioClips.Clear();
            foreach (var list in _clipsByCategory.Values)
            {
                list.Clear();
            }

            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(audioDataPath);

            if (jsonFiles.Length == 0)
            {
                Debug.LogWarning($"No audio JSON files found in Resources/{audioDataPath}");
                return;
            }

            foreach (TextAsset jsonFile in jsonFiles)
            {
                try
                {
                    LoadAudioDataFromJson(jsonFile.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse audio JSON file '{jsonFile.name}': {e.Message}");
                }
            }

            Debug.Log($"AudioDatabase: Loaded {_audioClips.Count} audio clips from {jsonFiles.Length} files");
            OnDatabaseLoaded?.Invoke();
        }

        /// <summary>
        /// Loads audio data from a JSON string
        /// </summary>
        private void LoadAudioDataFromJson(string json)
        {
            // Try parsing as array wrapper first
            AudioClipDataList dataList = JsonUtility.FromJson<AudioClipDataList>(json);

            if (dataList != null && dataList.clips != null)
            {
                foreach (AudioClipData clipData in dataList.clips)
                {
                    RegisterAudioClip(clipData);
                }
            }
            else
            {
                // Try parsing as single clip
                AudioClipData singleClip = JsonUtility.FromJson<AudioClipData>(json);
                if (singleClip != null && !string.IsNullOrEmpty(singleClip.clipID))
                {
                    RegisterAudioClip(singleClip);
                }
            }
        }

        /// <summary>
        /// Registers an audio clip in the database
        /// </summary>
        public void RegisterAudioClip(AudioClipData clipData)
        {
            if (clipData == null || string.IsNullOrEmpty(clipData.clipID))
            {
                Debug.LogWarning("Cannot register audio clip: invalid data");
                return;
            }

            if (_audioClips.ContainsKey(clipData.clipID))
            {
                Debug.LogWarning($"Audio clip '{clipData.clipID}' already exists, overwriting");
            }

            // Load the actual AudioClip from Resources
            if (!string.IsNullOrEmpty(clipData.clipPath))
            {
                clipData.clip = Resources.Load<AudioClip>(clipData.clipPath);
                if (clipData.clip == null)
                {
                    Debug.LogWarning($"AudioClip not found at path: {clipData.clipPath} for clip ID: {clipData.clipID}");
                }
            }

            _audioClips[clipData.clipID] = clipData;
            _clipsByCategory[clipData.category].Add(clipData);
        }

        /// <summary>
        /// Registers an AudioClip directly (for runtime registration)
        /// </summary>
        public void RegisterAudioClip(string clipID, AudioClip clip, AudioCategory category, float volume = 1f, bool loop = false)
        {
            AudioClipData data = new AudioClipData
            {
                clipID = clipID,
                displayName = clipID,
                category = category,
                defaultVolume = volume,
                loop = loop,
                clip = clip
            };

            RegisterAudioClip(data);
        }

        /// <summary>
        /// Gets audio clip data by ID
        /// </summary>
        public AudioClipData GetClipData(string clipID)
        {
            if (string.IsNullOrEmpty(clipID))
                return null;

            if (_audioClips.TryGetValue(clipID, out AudioClipData data))
            {
                return data;
            }

            Debug.LogWarning($"Audio clip '{clipID}' not found in database");
            return null;
        }

        /// <summary>
        /// Gets the AudioClip by ID
        /// </summary>
        public AudioClip GetClip(string clipID)
        {
            AudioClipData data = GetClipData(clipID);
            return data?.clip;
        }

        /// <summary>
        /// Checks if an audio clip exists in the database
        /// </summary>
        public bool HasClip(string clipID)
        {
            return !string.IsNullOrEmpty(clipID) && _audioClips.ContainsKey(clipID);
        }

        /// <summary>
        /// Gets all clips of a specific category
        /// </summary>
        public List<AudioClipData> GetClipsByCategory(AudioCategory category)
        {
            return new List<AudioClipData>(_clipsByCategory[category]);
        }

        /// <summary>
        /// Gets all clip IDs
        /// </summary>
        public IEnumerable<string> GetAllClipIDs()
        {
            return _audioClips.Keys;
        }

        /// <summary>
        /// Gets all audio clip data
        /// </summary>
        public IEnumerable<AudioClipData> GetAllClips()
        {
            return _audioClips.Values;
        }

        /// <summary>
        /// Gets a random clip from a category
        /// </summary>
        public AudioClipData GetRandomClipFromCategory(AudioCategory category)
        {
            List<AudioClipData> clips = _clipsByCategory[category];
            if (clips.Count == 0)
                return null;

            return clips[UnityEngine.Random.Range(0, clips.Count)];
        }

        /// <summary>
        /// Unloads all cached AudioClips to free memory
        /// </summary>
        public void UnloadAllClips()
        {
            foreach (var clipData in _audioClips.Values)
            {
                if (clipData.clip != null)
                {
                    Resources.UnloadAsset(clipData.clip);
                    clipData.clip = null;
                }
            }
        }

        /// <summary>
        /// Preloads all AudioClips into memory
        /// </summary>
        public void PreloadAllClips()
        {
            foreach (var clipData in _audioClips.Values)
            {
                if (clipData.clip == null && !string.IsNullOrEmpty(clipData.clipPath))
                {
                    clipData.clip = Resources.Load<AudioClip>(clipData.clipPath);
                }
            }
        }

        /// <summary>
        /// Preloads clips of a specific category
        /// </summary>
        public void PreloadCategory(AudioCategory category)
        {
            foreach (var clipData in _clipsByCategory[category])
            {
                if (clipData.clip == null && !string.IsNullOrEmpty(clipData.clipPath))
                {
                    clipData.clip = Resources.Load<AudioClip>(clipData.clipPath);
                }
            }
        }
    }
}

