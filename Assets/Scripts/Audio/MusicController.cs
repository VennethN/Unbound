using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Audio
{
    /// <summary>
    /// Specialized music controller for managing playlists, dynamic music, and transitions
    /// Use AudioManager for simple music playback, use this for advanced features
    /// </summary>
    public class MusicController : MonoBehaviour
    {
        private static MusicController _instance;

        [Header("Playlist Settings")]
        [SerializeField] private List<string> playlist = new List<string>();
        [SerializeField] private bool shufflePlaylist = false;
        [SerializeField] private bool loopPlaylist = true;
        [SerializeField] private float trackGapDuration = 0f;
        [SerializeField] private float crossfadeDuration = 2f;

        [Header("Auto Play")]
        [SerializeField] private bool autoPlayOnStart = false;
        [SerializeField] private string startingTrackID;

        private int _currentTrackIndex = -1;
        private List<int> _shuffledIndices = new List<int>();
        private Coroutine _playlistCoroutine;
        private bool _isPlayingPlaylist;

        public static MusicController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<MusicController>();
                }
                return _instance;
            }
        }

        public bool IsPlayingPlaylist => _isPlayingPlaylist;
        public int CurrentTrackIndex => _currentTrackIndex;
        public string CurrentTrackID => _currentTrackIndex >= 0 && _currentTrackIndex < playlist.Count 
            ? GetTrackAtIndex(_currentTrackIndex) 
            : null;

        public event Action<string, int> OnTrackChanged;
        public event Action OnPlaylistEnded;

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
            if (autoPlayOnStart)
            {
                if (!string.IsNullOrEmpty(startingTrackID))
                {
                    PlayTrack(startingTrackID);
                }
                else if (playlist.Count > 0)
                {
                    StartPlaylist();
                }
            }
        }

        #region Playlist Management

        /// <summary>
        /// Sets the playlist from a list of clip IDs
        /// </summary>
        public void SetPlaylist(List<string> clipIDs)
        {
            playlist = new List<string>(clipIDs);
            _currentTrackIndex = -1;
            GenerateShuffledIndices();
        }

        /// <summary>
        /// Adds a track to the playlist
        /// </summary>
        public void AddToPlaylist(string clipID)
        {
            playlist.Add(clipID);
            GenerateShuffledIndices();
        }

        /// <summary>
        /// Removes a track from the playlist
        /// </summary>
        public void RemoveFromPlaylist(string clipID)
        {
            int index = playlist.IndexOf(clipID);
            if (index >= 0)
            {
                playlist.RemoveAt(index);
                GenerateShuffledIndices();

                if (index <= _currentTrackIndex)
                {
                    _currentTrackIndex--;
                }
            }
        }

        /// <summary>
        /// Clears the playlist
        /// </summary>
        public void ClearPlaylist()
        {
            StopPlaylist();
            playlist.Clear();
            _currentTrackIndex = -1;
            _shuffledIndices.Clear();
        }

        /// <summary>
        /// Gets the track ID at a given index (respects shuffle)
        /// </summary>
        private string GetTrackAtIndex(int index)
        {
            if (index < 0 || index >= playlist.Count)
                return null;

            if (shufflePlaylist && _shuffledIndices.Count == playlist.Count)
            {
                return playlist[_shuffledIndices[index]];
            }

            return playlist[index];
        }

        /// <summary>
        /// Generates shuffled indices for shuffle mode
        /// </summary>
        private void GenerateShuffledIndices()
        {
            _shuffledIndices.Clear();
            for (int i = 0; i < playlist.Count; i++)
            {
                _shuffledIndices.Add(i);
            }

            // Fisher-Yates shuffle
            for (int i = _shuffledIndices.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (_shuffledIndices[i], _shuffledIndices[j]) = (_shuffledIndices[j], _shuffledIndices[i]);
            }
        }

        #endregion

        #region Playback Control

        /// <summary>
        /// Starts playing the playlist from the beginning
        /// </summary>
        public void StartPlaylist()
        {
            if (playlist.Count == 0)
            {
                Debug.LogWarning("Cannot start playlist: playlist is empty");
                return;
            }

            StopPlaylist();

            if (shufflePlaylist)
            {
                GenerateShuffledIndices();
            }

            _currentTrackIndex = -1;
            _isPlayingPlaylist = true;
            _playlistCoroutine = StartCoroutine(PlaylistLoop());
        }

        /// <summary>
        /// Stops the playlist
        /// </summary>
        public void StopPlaylist()
        {
            _isPlayingPlaylist = false;

            if (_playlistCoroutine != null)
            {
                StopCoroutine(_playlistCoroutine);
                _playlistCoroutine = null;
            }

            AudioManager.Instance.StopMusic(true, crossfadeDuration);
        }

        /// <summary>
        /// Pauses the playlist
        /// </summary>
        public void PausePlaylist()
        {
            _isPlayingPlaylist = false;
            AudioManager.Instance.PauseMusic();
        }

        /// <summary>
        /// Resumes the playlist
        /// </summary>
        public void ResumePlaylist()
        {
            if (_currentTrackIndex >= 0)
            {
                _isPlayingPlaylist = true;
                AudioManager.Instance.ResumeMusic();
            }
        }

        /// <summary>
        /// Skips to the next track
        /// </summary>
        public void NextTrack()
        {
            if (playlist.Count == 0)
                return;

            _currentTrackIndex++;

            if (_currentTrackIndex >= playlist.Count)
            {
                if (loopPlaylist)
                {
                    _currentTrackIndex = 0;
                    if (shufflePlaylist)
                    {
                        GenerateShuffledIndices();
                    }
                }
                else
                {
                    StopPlaylist();
                    OnPlaylistEnded?.Invoke();
                    return;
                }
            }

            string trackID = GetTrackAtIndex(_currentTrackIndex);
            AudioManager.Instance.PlayMusic(trackID, true, crossfadeDuration);
            OnTrackChanged?.Invoke(trackID, _currentTrackIndex);
        }

        /// <summary>
        /// Goes to the previous track
        /// </summary>
        public void PreviousTrack()
        {
            if (playlist.Count == 0)
                return;

            _currentTrackIndex--;

            if (_currentTrackIndex < 0)
            {
                _currentTrackIndex = loopPlaylist ? playlist.Count - 1 : 0;
            }

            string trackID = GetTrackAtIndex(_currentTrackIndex);
            AudioManager.Instance.PlayMusic(trackID, true, crossfadeDuration);
            OnTrackChanged?.Invoke(trackID, _currentTrackIndex);
        }

        /// <summary>
        /// Plays a specific track by ID
        /// </summary>
        public void PlayTrack(string clipID)
        {
            int index = playlist.IndexOf(clipID);
            if (index >= 0)
            {
                _currentTrackIndex = index;
            }

            AudioManager.Instance.PlayMusic(clipID, true, crossfadeDuration);
            OnTrackChanged?.Invoke(clipID, _currentTrackIndex);
        }

        /// <summary>
        /// Plays a track at a specific index
        /// </summary>
        public void PlayTrackAtIndex(int index)
        {
            if (index < 0 || index >= playlist.Count)
            {
                Debug.LogWarning($"Track index {index} out of range");
                return;
            }

            _currentTrackIndex = index;
            string trackID = GetTrackAtIndex(index);
            AudioManager.Instance.PlayMusic(trackID, true, crossfadeDuration);
            OnTrackChanged?.Invoke(trackID, _currentTrackIndex);
        }

        /// <summary>
        /// The main playlist playback loop
        /// </summary>
        private IEnumerator PlaylistLoop()
        {
            while (_isPlayingPlaylist)
            {
                NextTrack();

                // Wait for track to finish
                while (AudioManager.Instance.IsMusicPlaying && _isPlayingPlaylist)
                {
                    yield return null;
                }

                // Gap between tracks
                if (trackGapDuration > 0f && _isPlayingPlaylist)
                {
                    yield return new WaitForSeconds(trackGapDuration);
                }
            }
        }

        #endregion

        #region Settings

        /// <summary>
        /// Sets shuffle mode
        /// </summary>
        public void SetShuffle(bool shuffle)
        {
            shufflePlaylist = shuffle;
            if (shuffle)
            {
                GenerateShuffledIndices();
            }
        }

        /// <summary>
        /// Sets loop mode
        /// </summary>
        public void SetLoop(bool loop)
        {
            loopPlaylist = loop;
        }

        /// <summary>
        /// Sets the crossfade duration
        /// </summary>
        public void SetCrossfadeDuration(float duration)
        {
            crossfadeDuration = Mathf.Max(0f, duration);
        }

        /// <summary>
        /// Sets the gap between tracks
        /// </summary>
        public void SetTrackGap(float duration)
        {
            trackGapDuration = Mathf.Max(0f, duration);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets all tracks in the playlist
        /// </summary>
        public List<string> GetPlaylist()
        {
            return new List<string>(playlist);
        }

        /// <summary>
        /// Gets the number of tracks in the playlist
        /// </summary>
        public int GetPlaylistCount()
        {
            return playlist.Count;
        }

        #endregion
    }
}

