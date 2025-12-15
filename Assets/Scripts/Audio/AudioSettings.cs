using System;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Manages audio settings and volume levels with save system integration
    /// </summary>
    [Serializable]
    public class AudioSettings
    {
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        [Range(0f, 1f)]
        public float musicVolume = 1f;

        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        [Range(0f, 1f)]
        public float ambientVolume = 1f;

        [Range(0f, 1f)]
        public float uiVolume = 1f;

        [Range(0f, 1f)]
        public float voiceVolume = 1f;

        public bool muteAll = false;
        public bool muteMusic = false;
        public bool muteSFX = false;
        public bool muteAmbient = false;
        public bool muteUI = false;
        public bool muteVoice = false;

        // PlayerPrefs keys
        private const string MASTER_VOLUME_KEY = "Audio_MasterVolume";
        private const string MUSIC_VOLUME_KEY = "Audio_MusicVolume";
        private const string SFX_VOLUME_KEY = "Audio_SFXVolume";
        private const string AMBIENT_VOLUME_KEY = "Audio_AmbientVolume";
        private const string UI_VOLUME_KEY = "Audio_UIVolume";
        private const string VOICE_VOLUME_KEY = "Audio_VoiceVolume";
        private const string MUTE_ALL_KEY = "Audio_MuteAll";

        public AudioSettings()
        {
            ResetToDefaults();
        }

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            masterVolume = 1f;
            musicVolume = 1f;
            sfxVolume = 1f;
            ambientVolume = 1f;
            uiVolume = 1f;
            voiceVolume = 1f;
            muteAll = false;
            muteMusic = false;
            muteSFX = false;
            muteAmbient = false;
            muteUI = false;
            muteVoice = false;
        }

        /// <summary>
        /// Gets the effective volume for a category (considering master and mute states)
        /// </summary>
        public float GetEffectiveVolume(AudioCategory category)
        {
            if (muteAll)
                return 0f;

            float categoryVolume = GetCategoryVolume(category);
            bool categoryMuted = IsCategoryMuted(category);

            if (categoryMuted)
                return 0f;

            return masterVolume * categoryVolume;
        }

        /// <summary>
        /// Gets the raw volume for a category (not affected by master)
        /// </summary>
        public float GetCategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music:
                    return musicVolume;
                case AudioCategory.SFX:
                    return sfxVolume;
                case AudioCategory.Ambient:
                    return ambientVolume;
                case AudioCategory.UI:
                    return uiVolume;
                case AudioCategory.Voice:
                    return voiceVolume;
                default:
                    return 1f;
            }
        }

        /// <summary>
        /// Sets the volume for a category
        /// </summary>
        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);

            switch (category)
            {
                case AudioCategory.Music:
                    musicVolume = volume;
                    break;
                case AudioCategory.SFX:
                    sfxVolume = volume;
                    break;
                case AudioCategory.Ambient:
                    ambientVolume = volume;
                    break;
                case AudioCategory.UI:
                    uiVolume = volume;
                    break;
                case AudioCategory.Voice:
                    voiceVolume = volume;
                    break;
            }
        }

        /// <summary>
        /// Checks if a category is muted
        /// </summary>
        public bool IsCategoryMuted(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Music:
                    return muteMusic;
                case AudioCategory.SFX:
                    return muteSFX;
                case AudioCategory.Ambient:
                    return muteAmbient;
                case AudioCategory.UI:
                    return muteUI;
                case AudioCategory.Voice:
                    return muteVoice;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Sets the mute state for a category
        /// </summary>
        public void SetCategoryMuted(AudioCategory category, bool muted)
        {
            switch (category)
            {
                case AudioCategory.Music:
                    muteMusic = muted;
                    break;
                case AudioCategory.SFX:
                    muteSFX = muted;
                    break;
                case AudioCategory.Ambient:
                    muteAmbient = muted;
                    break;
                case AudioCategory.UI:
                    muteUI = muted;
                    break;
                case AudioCategory.Voice:
                    muteVoice = muted;
                    break;
            }
        }

        /// <summary>
        /// Saves settings to PlayerPrefs
        /// </summary>
        public void SaveToPlayerPrefs()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
            PlayerPrefs.SetFloat(AMBIENT_VOLUME_KEY, ambientVolume);
            PlayerPrefs.SetFloat(UI_VOLUME_KEY, uiVolume);
            PlayerPrefs.SetFloat(VOICE_VOLUME_KEY, voiceVolume);
            PlayerPrefs.SetInt(MUTE_ALL_KEY, muteAll ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads settings from PlayerPrefs
        /// </summary>
        public void LoadFromPlayerPrefs()
        {
            masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 1f);
            sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            ambientVolume = PlayerPrefs.GetFloat(AMBIENT_VOLUME_KEY, 1f);
            uiVolume = PlayerPrefs.GetFloat(UI_VOLUME_KEY, 1f);
            voiceVolume = PlayerPrefs.GetFloat(VOICE_VOLUME_KEY, 1f);
            muteAll = PlayerPrefs.GetInt(MUTE_ALL_KEY, 0) == 1;
        }

        /// <summary>
        /// Checks if any saved settings exist
        /// </summary>
        public static bool HasSavedSettings()
        {
            return PlayerPrefs.HasKey(MASTER_VOLUME_KEY);
        }

        /// <summary>
        /// Clears all saved settings from PlayerPrefs
        /// </summary>
        public static void ClearSavedSettings()
        {
            PlayerPrefs.DeleteKey(MASTER_VOLUME_KEY);
            PlayerPrefs.DeleteKey(MUSIC_VOLUME_KEY);
            PlayerPrefs.DeleteKey(SFX_VOLUME_KEY);
            PlayerPrefs.DeleteKey(AMBIENT_VOLUME_KEY);
            PlayerPrefs.DeleteKey(UI_VOLUME_KEY);
            PlayerPrefs.DeleteKey(VOICE_VOLUME_KEY);
            PlayerPrefs.DeleteKey(MUTE_ALL_KEY);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Creates a copy of these settings
        /// </summary>
        public AudioSettings Clone()
        {
            return new AudioSettings
            {
                masterVolume = masterVolume,
                musicVolume = musicVolume,
                sfxVolume = sfxVolume,
                ambientVolume = ambientVolume,
                uiVolume = uiVolume,
                voiceVolume = voiceVolume,
                muteAll = muteAll,
                muteMusic = muteMusic,
                muteSFX = muteSFX,
                muteAmbient = muteAmbient,
                muteUI = muteUI,
                muteVoice = muteVoice
            };
        }
    }
}

