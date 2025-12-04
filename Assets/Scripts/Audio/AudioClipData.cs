using System;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Defines the category of an audio clip
    /// </summary>
    public enum AudioCategory
    {
        Music,
        SFX,
        Ambient,
        UI,
        Voice
    }

    /// <summary>
    /// Data structure representing a single audio clip configuration
    /// </summary>
    [Serializable]
    public class AudioClipData
    {
        public string clipID;
        public string displayName;
        public string clipPath;
        public AudioCategory category;
        
        [Range(0f, 1f)]
        public float defaultVolume = 1f;
        
        [Range(-3f, 3f)]
        public float defaultPitch = 1f;
        
        [Range(0f, 1f)]
        public float pitchVariation = 0f;
        
        public bool loop = false;
        
        [Range(0f, 1f)]
        public float spatialBlend = 0f; // 0 = 2D, 1 = 3D
        
        public float minDistance = 1f;
        public float maxDistance = 500f;
        
        // Priority (0 = highest, 256 = lowest)
        [Range(0, 256)]
        public int priority = 128;
        
        // Cached AudioClip reference
        [NonSerialized]
        public AudioClip clip;

        public AudioClipData()
        {
            defaultVolume = 1f;
            defaultPitch = 1f;
            pitchVariation = 0f;
            loop = false;
            spatialBlend = 0f;
            minDistance = 1f;
            maxDistance = 500f;
            priority = 128;
        }

        /// <summary>
        /// Creates a copy of this AudioClipData
        /// </summary>
        public AudioClipData Clone()
        {
            return new AudioClipData
            {
                clipID = clipID,
                displayName = displayName,
                clipPath = clipPath,
                category = category,
                defaultVolume = defaultVolume,
                defaultPitch = defaultPitch,
                pitchVariation = pitchVariation,
                loop = loop,
                spatialBlend = spatialBlend,
                minDistance = minDistance,
                maxDistance = maxDistance,
                priority = priority,
                clip = clip
            };
        }
    }

    /// <summary>
    /// Wrapper for JSON array deserialization
    /// </summary>
    [Serializable]
    public class AudioClipDataList
    {
        public AudioClipData[] clips;
    }

    /// <summary>
    /// Represents a playing audio instance for tracking
    /// </summary>
    public class AudioInstance
    {
        public string clipID;
        public AudioSource source;
        public float startTime;
        public bool isPaused;
        public float originalVolume;

        public bool IsPlaying => source != null && source.isPlaying;
        public float ElapsedTime => Time.time - startTime;
    }
}

