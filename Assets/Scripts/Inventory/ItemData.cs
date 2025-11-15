using System;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Base item data structure that can be serialized to/from JSON
    /// </summary>
    [Serializable]
    public class ItemData
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for this item")]
        public string itemID;
        
        [Tooltip("Display name of the item")]
        public string name;
        
        [Tooltip("Description of the item")]
        public string description;
        
        [Tooltip("Type of item (Equipment, Collectable, Consumable)")]
        public ItemType itemType;
        
        [Header("Visual")]
        [Tooltip("Path to the icon sprite (relative to Resources folder) or sprite ID from registry")]
        public string iconPath;
        
        [Tooltip("Sprite ID to reference from sprite registry (alternative to iconPath)")]
        public string spriteID;
        
        [Header("Equipment Properties")]
        [Tooltip("Equipment slot type (only for Equipment items)")]
        public EquipmentType equipmentType;
        
        [Tooltip("Stats provided by this equipment (only for Equipment items)")]
        public ItemStats stats;
        
        [Header("Consumable Properties")]
        [Tooltip("Effects when consumed (only for Consumable items)")]
        public ConsumableEffect consumableEffect;
        
        [Header("Stacking")]
        [Tooltip("Maximum stack size (0 = unlimited)")]
        public int maxStackSize = 1;
        
        /// <summary>
        /// Validates that the item data is correct
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning($"ItemData validation failed: itemID is null or empty");
                return false;
            }
            
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"ItemData validation failed: name is null or empty for item {itemID}");
                return false;
            }
            
            if (itemType == ItemType.Equipment && stats == null)
            {
                Debug.LogWarning($"ItemData validation failed: Equipment item {itemID} has no stats");
                return false;
            }
            
            if (itemType == ItemType.Consumable && consumableEffect == null)
            {
                Debug.LogWarning($"ItemData validation failed: Consumable item {itemID} has no consumable effect");
                return false;
            }
            
            return true;
        }
    }
}

