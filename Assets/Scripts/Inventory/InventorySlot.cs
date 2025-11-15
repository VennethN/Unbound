using System;

namespace Unbound.Inventory
{
    /// <summary>
    /// Represents a single inventory slot containing an item and quantity
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public string itemID;
        public int quantity;
        
        public InventorySlot()
        {
            itemID = string.Empty;
            quantity = 0;
        }
        
        public InventorySlot(string itemID, int quantity)
        {
            this.itemID = itemID;
            this.quantity = quantity;
        }
        
        public bool IsEmpty => string.IsNullOrEmpty(itemID) || quantity <= 0;
        
        public void Clear()
        {
            itemID = string.Empty;
            quantity = 0;
        }
        
        public InventorySlot Clone()
        {
            return new InventorySlot(itemID, quantity);
        }
    }
}

