using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Utility class for loading item icons from various sources
    /// </summary>
    public static class ItemIconLoader
    {
        private static Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();
        
        /// <summary>
        /// Loads an icon sprite for an item, checking multiple sources:
        /// 1. Sprite ID from registry (if spriteID is set)
        /// 2. Cached sprite from iconPath
        /// 3. Resources folder (if iconPath is set)
        /// </summary>
        public static Sprite LoadIcon(ItemData itemData)
        {
            if (itemData == null)
                return null;
            
            // Check spriteID first (from registry)
            if (!string.IsNullOrEmpty(itemData.spriteID) && ItemDatabase.Instance != null)
            {
                Sprite sprite = ItemDatabase.Instance.GetSprite(itemData.spriteID);
                if (sprite != null)
                    return sprite;
            }
            
            // Check cache first for iconPath
            if (!string.IsNullOrEmpty(itemData.iconPath) && _iconCache.TryGetValue(itemData.iconPath, out Sprite cachedSprite))
            {
                return cachedSprite;
            }
            
            // Try loading from Resources
            if (!string.IsNullOrEmpty(itemData.iconPath))
            {
                Sprite sprite = Resources.Load<Sprite>(itemData.iconPath);
                if (sprite != null)
                {
                    _iconCache[itemData.iconPath] = sprite;
                    return sprite;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Loads an icon sprite by path, checking cache first
        /// </summary>
        public static Sprite LoadIcon(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
                return null;
            
            // Check cache first
            if (_iconCache.TryGetValue(iconPath, out Sprite cachedSprite))
            {
                return cachedSprite;
            }
            
            // Try loading from Resources
            Sprite sprite = Resources.Load<Sprite>(iconPath);
            if (sprite != null)
            {
                _iconCache[iconPath] = sprite;
                return sprite;
            }
            
            return null;
        }
        
        /// <summary>
        /// Registers a sprite in the cache with a specific path/key
        /// Useful for loading sprites from other sources (like direct references)
        /// </summary>
        public static void RegisterIcon(string key, Sprite sprite)
        {
            if (sprite != null && !string.IsNullOrEmpty(key))
            {
                _iconCache[key] = sprite;
            }
        }
        
        /// <summary>
        /// Clears the icon cache
        /// </summary>
        public static void ClearCache()
        {
            _iconCache.Clear();
        }
        
        /// <summary>
        /// Preloads an icon into the cache
        /// </summary>
        public static void PreloadIcon(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath))
                return;
            
            if (!_iconCache.ContainsKey(iconPath))
            {
                Sprite sprite = Resources.Load<Sprite>(iconPath);
                if (sprite != null)
                {
                    _iconCache[iconPath] = sprite;
                }
            }
        }
    }
}

