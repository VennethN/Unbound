using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Specialized SFX controller for managing sound effect groups, cooldowns, and advanced playback
    /// Use AudioManager for simple SFX playback, use this for advanced features
    /// </summary>
    public class SFXController : MonoBehaviour
    {
        private static SFXController _instance;

        [Header("SFX Groups")]
        [SerializeField] private List<SFXGroup> sfxGroups = new List<SFXGroup>();

        [Header("Cooldown Settings")]
        [SerializeField] private float defaultCooldown = 0.05f;

        private Dictionary<string, SFXGroup> _groupsLookup = new Dictionary<string, SFXGroup>();
        private Dictionary<string, float> _cooldowns = new Dictionary<string, float>();
        private Dictionary<string, int> _variantIndices = new Dictionary<string, int>();

        public static SFXController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SFXController>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            BuildGroupsLookup();
        }

        private void Update()
        {
            // Update cooldowns
            List<string> toRemove = new List<string>();
            List<string> keys = new List<string>(_cooldowns.Keys);

            foreach (string key in keys)
            {
                _cooldowns[key] -= Time.deltaTime;
                if (_cooldowns[key] <= 0f)
                {
                    toRemove.Add(key);
                }
            }

            foreach (string key in toRemove)
            {
                _cooldowns.Remove(key);
            }
        }

        /// <summary>
        /// Builds the lookup dictionary for SFX groups
        /// </summary>
        private void BuildGroupsLookup()
        {
            _groupsLookup.Clear();
            foreach (SFXGroup group in sfxGroups)
            {
                if (!string.IsNullOrEmpty(group.groupID))
                {
                    _groupsLookup[group.groupID] = group;
                }
            }
        }

        #region Basic Playback

        /// <summary>
        /// Plays a sound effect with cooldown checking
        /// </summary>
        public AudioInstance PlaySFX(string clipID, Vector3? position = null)
        {
            if (IsOnCooldown(clipID))
                return null;

            AudioInstance instance = AudioManager.Instance.PlaySFX(clipID, position);

            if (instance != null)
            {
                SetCooldown(clipID);
            }

            return instance;
        }

        /// <summary>
        /// Plays a sound effect, ignoring cooldown
        /// </summary>
        public AudioInstance PlaySFXForceCooldown(string clipID, Vector3? position = null)
        {
            return AudioManager.Instance.PlaySFX(clipID, position);
        }

        /// <summary>
        /// Plays a one-shot sound effect with cooldown checking
        /// </summary>
        public void PlaySFXOneShot(string clipID, Vector3? position = null)
        {
            if (IsOnCooldown(clipID))
                return;

            AudioManager.Instance.PlaySFXOneShot(clipID, position);
            SetCooldown(clipID);
        }

        #endregion

        #region SFX Groups

        /// <summary>
        /// Plays a random sound from a group
        /// </summary>
        public AudioInstance PlayFromGroup(string groupID, Vector3? position = null)
        {
            if (!_groupsLookup.TryGetValue(groupID, out SFXGroup group))
            {
                Debug.LogWarning($"SFX group '{groupID}' not found");
                return null;
            }

            if (group.clipIDs == null || group.clipIDs.Count == 0)
            {
                Debug.LogWarning($"SFX group '{groupID}' is empty");
                return null;
            }

            string clipID = GetClipFromGroup(group);
            return PlaySFX(clipID, position);
        }

        /// <summary>
        /// Plays the next sound in sequence from a group
        /// </summary>
        public AudioInstance PlaySequenceFromGroup(string groupID, Vector3? position = null)
        {
            if (!_groupsLookup.TryGetValue(groupID, out SFXGroup group))
            {
                Debug.LogWarning($"SFX group '{groupID}' not found");
                return null;
            }

            if (group.clipIDs == null || group.clipIDs.Count == 0)
                return null;

            if (!_variantIndices.ContainsKey(groupID))
            {
                _variantIndices[groupID] = 0;
            }

            int index = _variantIndices[groupID];
            string clipID = group.clipIDs[index];

            _variantIndices[groupID] = (index + 1) % group.clipIDs.Count;

            return PlaySFX(clipID, position);
        }

        /// <summary>
        /// Gets a clip ID from a group based on its selection mode
        /// </summary>
        private string GetClipFromGroup(SFXGroup group)
        {
            if (group.clipIDs.Count == 1)
                return group.clipIDs[0];

            switch (group.selectionMode)
            {
                case SFXSelectionMode.Random:
                    return group.clipIDs[UnityEngine.Random.Range(0, group.clipIDs.Count)];

                case SFXSelectionMode.RandomNoRepeat:
                    if (!_variantIndices.ContainsKey(group.groupID))
                    {
                        _variantIndices[group.groupID] = -1;
                    }

                    int lastIndex = _variantIndices[group.groupID];
                    int newIndex;
                    do
                    {
                        newIndex = UnityEngine.Random.Range(0, group.clipIDs.Count);
                    } while (newIndex == lastIndex && group.clipIDs.Count > 1);

                    _variantIndices[group.groupID] = newIndex;
                    return group.clipIDs[newIndex];

                case SFXSelectionMode.Sequential:
                    if (!_variantIndices.ContainsKey(group.groupID))
                    {
                        _variantIndices[group.groupID] = 0;
                    }

                    int seqIndex = _variantIndices[group.groupID];
                    _variantIndices[group.groupID] = (seqIndex + 1) % group.clipIDs.Count;
                    return group.clipIDs[seqIndex];

                default:
                    return group.clipIDs[0];
            }
        }

        /// <summary>
        /// Registers a new SFX group at runtime
        /// </summary>
        public void RegisterGroup(string groupID, List<string> clipIDs, SFXSelectionMode mode = SFXSelectionMode.Random)
        {
            SFXGroup group = new SFXGroup
            {
                groupID = groupID,
                clipIDs = new List<string>(clipIDs),
                selectionMode = mode
            };

            sfxGroups.Add(group);
            _groupsLookup[groupID] = group;
        }

        /// <summary>
        /// Removes an SFX group
        /// </summary>
        public void UnregisterGroup(string groupID)
        {
            if (_groupsLookup.TryGetValue(groupID, out SFXGroup group))
            {
                sfxGroups.Remove(group);
                _groupsLookup.Remove(groupID);
                _variantIndices.Remove(groupID);
            }
        }

        #endregion

        #region Cooldown Management

        /// <summary>
        /// Checks if a clip is on cooldown
        /// </summary>
        public bool IsOnCooldown(string clipID)
        {
            return _cooldowns.ContainsKey(clipID);
        }

        /// <summary>
        /// Sets cooldown for a clip
        /// </summary>
        public void SetCooldown(string clipID, float duration = -1f)
        {
            if (duration < 0f)
                duration = defaultCooldown;

            if (duration > 0f)
            {
                _cooldowns[clipID] = duration;
            }
        }

        /// <summary>
        /// Clears cooldown for a clip
        /// </summary>
        public void ClearCooldown(string clipID)
        {
            _cooldowns.Remove(clipID);
        }

        /// <summary>
        /// Clears all cooldowns
        /// </summary>
        public void ClearAllCooldowns()
        {
            _cooldowns.Clear();
        }

        /// <summary>
        /// Sets the default cooldown duration
        /// </summary>
        public void SetDefaultCooldown(float duration)
        {
            defaultCooldown = Mathf.Max(0f, duration);
        }

        #endregion

        #region Spatial Audio Helpers

        /// <summary>
        /// Plays a 3D positioned sound at a transform
        /// </summary>
        public AudioInstance PlayAtTransform(string clipID, Transform target)
        {
            if (target == null)
                return PlaySFX(clipID);

            return PlaySFX(clipID, target.position);
        }

        /// <summary>
        /// Plays a 3D positioned sound from a group at a transform
        /// </summary>
        public AudioInstance PlayGroupAtTransform(string groupID, Transform target)
        {
            if (target == null)
                return PlayFromGroup(groupID);

            return PlayFromGroup(groupID, target.position);
        }

        #endregion
    }

    /// <summary>
    /// Defines how clips are selected from a group
    /// </summary>
    public enum SFXSelectionMode
    {
        Random,
        RandomNoRepeat,
        Sequential
    }

    /// <summary>
    /// A group of related sound effects
    /// </summary>
    [Serializable]
    public class SFXGroup
    {
        public string groupID;
        public List<string> clipIDs = new List<string>();
        public SFXSelectionMode selectionMode = SFXSelectionMode.Random;
        public float cooldown = 0.05f;
    }
}

