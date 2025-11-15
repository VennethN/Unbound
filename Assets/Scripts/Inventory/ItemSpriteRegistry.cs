using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Component that allows registering sprites by ID in the Unity Inspector.
    /// Items can then reference these sprite IDs in their JSON files.
    /// </summary>
    public class ItemSpriteRegistry : MonoBehaviour
    {
        [System.Serializable]
        public class SpriteMapping
        {
            [Tooltip("Sprite ID that items can reference in their JSON (e.g., 'sword_icon')")]
            public string spriteID;
            
            [Tooltip("Sprite to use for this sprite ID")]
            public Sprite sprite;
        }
        
        [Header("Sprite Registry")]
        [Tooltip("List of sprite ID to sprite mappings. Items reference these IDs in their JSON spriteID field.")]
        [SerializeField] private List<SpriteMapping> spriteMappings = new List<SpriteMapping>();
        
        private void Awake()
        {
            RegisterSprites();
        }
        
        /// <summary>
        /// Registers all sprites in the registry with ItemDatabase
        /// </summary>
        public void RegisterSprites()
        {
            if (ItemDatabase.Instance == null)
            {
                Debug.LogWarning("ItemSpriteRegistry: ItemDatabase.Instance is null. Cannot register sprites.");
                return;
            }
            
            int registeredCount = 0;
            foreach (var mapping in spriteMappings)
            {
                if (!string.IsNullOrEmpty(mapping.spriteID) && mapping.sprite != null)
                {
                    ItemDatabase.Instance.RegisterSprite(mapping.spriteID, mapping.sprite);
                    registeredCount++;
                }
            }
            
            Debug.Log($"ItemSpriteRegistry: Registered {registeredCount} sprites");
        }
        
        /// <summary>
        /// Adds a sprite mapping at runtime
        /// </summary>
        public void AddSpriteMapping(string spriteID, Sprite sprite)
        {
            if (string.IsNullOrEmpty(spriteID) || sprite == null)
                return;
            
            // Check if already exists
            var existing = spriteMappings.Find(m => m.spriteID == spriteID);
            if (existing != null)
            {
                existing.sprite = sprite;
            }
            else
            {
                spriteMappings.Add(new SpriteMapping { spriteID = spriteID, sprite = sprite });
            }
            
            // Register immediately
            if (ItemDatabase.Instance != null)
            {
                ItemDatabase.Instance.RegisterSprite(spriteID, sprite);
            }
        }
        
        /// <summary>
        /// Removes a sprite mapping
        /// </summary>
        public void RemoveSpriteMapping(string spriteID)
        {
            spriteMappings.RemoveAll(m => m.spriteID == spriteID);
        }
        
        /// <summary>
        /// Gets a sprite mapping by sprite ID
        /// </summary>
        public Sprite GetSprite(string spriteID)
        {
            var mapping = spriteMappings.Find(m => m.spriteID == spriteID);
            return mapping?.sprite;
        }
    }
}

