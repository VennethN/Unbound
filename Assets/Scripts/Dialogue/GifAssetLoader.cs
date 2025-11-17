using UnityEngine;
using System.Collections.Generic;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Utility class for loading GifAsset ScriptableObjects from Resources
    /// </summary>
    public static class GifAssetLoader
    {
        private static Dictionary<string, GifAsset> _cache = new Dictionary<string, GifAsset>();

        /// <summary>
        /// Loads a GifAsset from Resources folder
        /// Path should be relative to Resources folder (e.g., "Gifs/RafaIdle" for Assets/Resources/Gifs/RafaIdle.asset)
        /// </summary>
        public static GifAsset Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Check cache first
            if (_cache.TryGetValue(path, out GifAsset cached))
            {
                return cached;
            }

            // Load from Resources
            GifAsset gifAsset = Resources.Load<GifAsset>(path);
            if (gifAsset != null)
            {
                _cache[path] = gifAsset;
            }
            else
            {
                Debug.LogWarning($"Failed to load GifAsset from path: {path}. Make sure the asset is in a Resources folder.");
            }

            return gifAsset;
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Preloads a GifAsset into the cache
        /// </summary>
        public static void Preload(string path)
        {
            if (!string.IsNullOrEmpty(path) && !_cache.ContainsKey(path))
            {
                Load(path);
            }
        }
    }
}

