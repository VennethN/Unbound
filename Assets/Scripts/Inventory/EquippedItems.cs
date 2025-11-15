using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Tracks currently equipped items per equipment slot
    /// </summary>
    [Serializable]
    public class EquippedItems
    {
        [SerializeField] private Dictionary<EquipmentType, string> equippedItems = new Dictionary<EquipmentType, string>();
        
        public EquippedItems()
        {
            // Initialize all slots as empty
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                equippedItems[type] = string.Empty;
            }
        }
        
        /// <summary>
        /// Equips an item to a slot
        /// </summary>
        public void Equip(EquipmentType slot, string itemID)
        {
            if (equippedItems.ContainsKey(slot))
            {
                equippedItems[slot] = itemID ?? string.Empty;
            }
        }
        
        /// <summary>
        /// Unequips an item from a slot
        /// </summary>
        public void Unequip(EquipmentType slot)
        {
            if (equippedItems.ContainsKey(slot))
            {
                equippedItems[slot] = string.Empty;
            }
        }
        
        /// <summary>
        /// Gets the item ID equipped in a slot
        /// </summary>
        public string GetEquippedItem(EquipmentType slot)
        {
            if (equippedItems.TryGetValue(slot, out string itemID))
            {
                return string.IsNullOrEmpty(itemID) ? null : itemID;
            }
            return null;
        }
        
        /// <summary>
        /// Checks if a slot has an item equipped
        /// </summary>
        public bool IsEquipped(EquipmentType slot)
        {
            return !string.IsNullOrEmpty(GetEquippedItem(slot));
        }
        
        /// <summary>
        /// Gets all equipped items
        /// </summary>
        public Dictionary<EquipmentType, string> GetAllEquipped()
        {
            return new Dictionary<EquipmentType, string>(equippedItems);
        }
        
        /// <summary>
        /// Clears all equipped items
        /// </summary>
        public void ClearAll()
        {
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                equippedItems[type] = string.Empty;
            }
        }
    }
}

