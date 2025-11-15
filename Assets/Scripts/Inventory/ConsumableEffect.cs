using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Effect that a consumable item can have when used
    /// </summary>
    [Serializable]
    public class ConsumableEffect
    {
        [Tooltip("Type of effect")]
        public ConsumableEffectType effectType;
        
        [Tooltip("Amount for health restoration")]
        public float healthAmount = 0f;
        
        [Tooltip("Items to give (itemID, quantity pairs)")]
        public List<ItemReward> itemsToGive = new List<ItemReward>();
    }
    
    /// <summary>
    /// Types of consumable effects
    /// </summary>
    public enum ConsumableEffectType
    {
        RestoreHealth,
        GiveItems,
        Both
    }
    
    /// <summary>
    /// Represents an item reward (itemID and quantity)
    /// </summary>
    [Serializable]
    public class ItemReward
    {
        public string itemID;
        public int quantity = 1;
    }
}

