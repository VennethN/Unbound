using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Core inventory system managing items, equipment slots, and stat calculations
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        private static InventoryManager _instance;
        
        [Header("Inventory Settings")]
        [SerializeField] private int inventoryWidth = 9;
        [SerializeField] private int inventoryHeight = 3;
        
        private List<InventorySlot> _inventory = new List<InventorySlot>();
        private EquippedItems _equippedItems = new EquippedItems();
        
        public static InventoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<InventoryManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("InventoryManager");
                        _instance = go.AddComponent<InventoryManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        public int InventorySize => inventoryWidth * inventoryHeight;
        public EquippedItems EquippedItems => _equippedItems;
        
        public event Action<InventorySlot, int> OnItemAdded;
        public event Action<InventorySlot, int> OnItemRemoved;
        public event Action<EquipmentType, string> OnItemEquipped;
        public event Action<EquipmentType> OnItemUnequipped;
        public event Action OnInventoryChanged;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeInventory();
        }
        
        private void InitializeInventory()
        {
            _inventory.Clear();
            for (int i = 0; i < InventorySize; i++)
            {
                _inventory.Add(new InventorySlot());
            }
        }
        
        /// <summary>
        /// Adds an item to the inventory
        /// </summary>
        public bool AddItem(string itemID, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemID) || quantity <= 0)
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"Item {itemID} not found in database");
                return false;
            }
            
            // Try to stack with existing items first
            if (itemData.maxStackSize > 1)
            {
                for (int i = 0; i < _inventory.Count && quantity > 0; i++)
                {
                    InventorySlot slot = _inventory[i];
                    if (!slot.IsEmpty && slot.itemID == itemID)
                    {
                        int spaceAvailable = itemData.maxStackSize - slot.quantity;
                        if (spaceAvailable > 0)
                        {
                            int addAmount = Mathf.Min(quantity, spaceAvailable);
                            slot.quantity += addAmount;
                            quantity -= addAmount;
                            
                            OnItemAdded?.Invoke(slot, i);
                        }
                    }
                }
            }
            
            // Add remaining quantity to empty slots
            while (quantity > 0)
            {
                int emptySlotIndex = FindEmptySlot();
                if (emptySlotIndex == -1)
                {
                    Debug.LogWarning($"Inventory is full, could not add {quantity} more of {itemID}");
                    OnInventoryChanged?.Invoke();
                    return false;
                }
                
                int addAmount = Mathf.Min(quantity, itemData.maxStackSize > 0 ? itemData.maxStackSize : quantity);
                _inventory[emptySlotIndex] = new InventorySlot(itemID, addAmount);
                quantity -= addAmount;
                
                OnItemAdded?.Invoke(_inventory[emptySlotIndex], emptySlotIndex);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public bool RemoveItem(string itemID, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemID) || quantity <= 0)
                return false;
            
            int remainingToRemove = quantity;
            
            for (int i = _inventory.Count - 1; i >= 0 && remainingToRemove > 0; i--)
            {
                InventorySlot slot = _inventory[i];
                if (!slot.IsEmpty && slot.itemID == itemID)
                {
                    int removeAmount = Mathf.Min(remainingToRemove, slot.quantity);
                    slot.quantity -= removeAmount;
                    remainingToRemove -= removeAmount;
                    
                    if (slot.quantity <= 0)
                    {
                        slot.Clear();
                    }
                    
                    OnItemRemoved?.Invoke(slot, i);
                }
            }
            
            if (remainingToRemove > 0)
            {
                Debug.LogWarning($"Could not remove {remainingToRemove} of {itemID} (not enough in inventory)");
                OnInventoryChanged?.Invoke();
                return false;
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Gets the quantity of an item in the inventory
        /// </summary>
        public int GetItemQuantity(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return 0;
            
            int total = 0;
            foreach (InventorySlot slot in _inventory)
            {
                if (!slot.IsEmpty && slot.itemID == itemID)
                {
                    total += slot.quantity;
                }
            }
            return total;
        }
        
        /// <summary>
        /// Checks if the inventory has enough of an item
        /// </summary>
        public bool HasItem(string itemID, int quantity = 1)
        {
            return GetItemQuantity(itemID) >= quantity;
        }
        
        /// <summary>
        /// Equips an item to a slot
        /// </summary>
        public bool EquipItem(string itemID, EquipmentType slot)
        {
            if (string.IsNullOrEmpty(itemID))
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData == null || itemData.itemType != ItemType.Equipment)
            {
                Debug.LogWarning($"Cannot equip {itemID}: not an equipment item");
                return false;
            }
            
            if (itemData.equipmentType != slot)
            {
                Debug.LogWarning($"Cannot equip {itemID}: equipment type mismatch (expected {slot}, got {itemData.equipmentType})");
                return false;
            }
            
            if (!HasItem(itemID, 1))
            {
                Debug.LogWarning($"Cannot equip {itemID}: not in inventory");
                return false;
            }
            
            // Unequip current item in slot if any
            string currentEquipped = _equippedItems.GetEquippedItem(slot);
            if (!string.IsNullOrEmpty(currentEquipped))
            {
                UnequipItem(slot);
            }
            
            // Equip without removing from inventory
            _equippedItems.Equip(slot, itemID);
            
            // Set global flags from item stats
            SetItemGlobalFlags(itemData, true);
            
            OnItemEquipped?.Invoke(slot, itemID);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Unequips an item from a slot
        /// </summary>
        public bool UnequipItem(EquipmentType slot)
        {
            string itemID = _equippedItems.GetEquippedItem(slot);
            if (string.IsNullOrEmpty(itemID))
                return false;
            
            // Unset global flags from item stats before unequipping
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData != null)
            {
                SetItemGlobalFlags(itemData, false);
            }
            
            // Unequip without adding back to inventory
            _equippedItems.Unequip(slot);
            OnItemUnequipped?.Invoke(slot);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Sets or unsets global flags from an item's stats
        /// </summary>
        private void SetItemGlobalFlags(ItemData itemData, bool value)
        {
            if (itemData == null || itemData.stats == null || itemData.stats.globalFlags == null)
                return;
            
            var saveManager = SaveManager.Instance;
            if (saveManager == null)
            {
                Debug.LogWarning("Cannot set global flags: SaveManager.Instance is null");
                return;
            }
            
            foreach (string flag in itemData.stats.globalFlags)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    saveManager.SetGlobalFlag(flag, value);
                    Debug.Log($"Set global flag '{flag}' to {value} (item: {itemData.itemID})");
                }
            }
        }
        
        /// <summary>
        /// Consumes a consumable item
        /// </summary>
        public bool ConsumeItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData == null || itemData.itemType != ItemType.Consumable)
            {
                Debug.LogWarning($"Cannot consume {itemID}: not a consumable item");
                return false;
            }
            
            if (!HasItem(itemID, 1))
            {
                Debug.LogWarning($"Cannot consume {itemID}: not in inventory");
                return false;
            }
            
            // Remove item from inventory
            RemoveItem(itemID, 1);
            
            // Apply consumable effects
            if (itemData.consumableEffect != null)
            {
                ApplyConsumableEffect(itemData.consumableEffect);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        private void ApplyConsumableEffect(ConsumableEffect effect)
        {
            if (effect == null) return;
            
            // Restore health
            if (effect.effectType == ConsumableEffectType.RestoreHealth || effect.effectType == ConsumableEffectType.Both)
            {
                if (effect.healthAmount > 0)
                {
                    // TODO: Integrate with player health system
                    Debug.Log($"Restoring {effect.healthAmount} health");
                }
            }
            
            // Give items
            if (effect.effectType == ConsumableEffectType.GiveItems || effect.effectType == ConsumableEffectType.Both)
            {
                foreach (ItemReward reward in effect.itemsToGive)
                {
                    if (!string.IsNullOrEmpty(reward.itemID) && reward.quantity > 0)
                    {
                        AddItem(reward.itemID, reward.quantity);
                    }
                }
            }
        }
        
        /// <summary>
        /// Gets the inventory slot at a specific index
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _inventory.Count)
                return null;
            
            return _inventory[index];
        }
        
        /// <summary>
        /// Gets all inventory slots
        /// </summary>
        public List<InventorySlot> GetAllSlots()
        {
            return new List<InventorySlot>(_inventory);
        }
        
        /// <summary>
        /// Calculates total stats from all equipped items
        /// </summary>
        public ItemStats GetTotalEquipmentStats()
        {
            ItemStats totalStats = new ItemStats();
            
            foreach (EquipmentType slot in System.Enum.GetValues(typeof(EquipmentType)))
            {
                string itemID = _equippedItems.GetEquippedItem(slot);
                if (!string.IsNullOrEmpty(itemID))
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
                    if (itemData != null && itemData.stats != null)
                    {
                        totalStats.AddStats(itemData.stats);
                    }
                }
            }
            
            return totalStats;
        }
        
        private int FindEmptySlot()
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].IsEmpty)
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// Clears the entire inventory
        /// </summary>
        public void ClearInventory()
        {
            InitializeInventory();
            OnInventoryChanged?.Invoke();
        }

        /// <summary>
        /// Manually triggers the inventory changed event (useful for external systems)
        /// </summary>
        public void NotifyInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
        }
        
        /// <summary>
        /// Swaps items between two slots
        /// </summary>
        public bool SwapSlots(int slotIndex1, int slotIndex2)
        {
            if (slotIndex1 < 0 || slotIndex1 >= _inventory.Count || 
                slotIndex2 < 0 || slotIndex2 >= _inventory.Count)
                return false;
            
            if (slotIndex1 == slotIndex2)
                return false;
            
            InventorySlot slot1 = _inventory[slotIndex1];
            InventorySlot slot2 = _inventory[slotIndex2];
            
            // Swap the slots
            _inventory[slotIndex1] = slot2.Clone();
            _inventory[slotIndex2] = slot1.Clone();
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Moves an item from one slot to another (swaps if target has item)
        /// </summary>
        public bool MoveItem(int fromSlotIndex, int toSlotIndex)
        {
            if (fromSlotIndex < 0 || fromSlotIndex >= _inventory.Count || 
                toSlotIndex < 0 || toSlotIndex >= _inventory.Count)
                return false;
            
            if (fromSlotIndex == toSlotIndex)
                return false;
            
            InventorySlot fromSlot = _inventory[fromSlotIndex];
            InventorySlot toSlot = _inventory[toSlotIndex];
            
            if (fromSlot.IsEmpty)
                return false;
            
            // If target is empty, move the item
            if (toSlot.IsEmpty)
            {
                _inventory[toSlotIndex] = fromSlot.Clone();
                _inventory[fromSlotIndex] = new InventorySlot();
            }
            // If same item and stackable, try to stack
            else if (fromSlot.itemID == toSlot.itemID)
            {
                ItemData itemData = ItemDatabase.Instance.GetItem(fromSlot.itemID);
                if (itemData != null && itemData.maxStackSize > 1)
                {
                    int maxStack = itemData.maxStackSize;
                    int spaceAvailable = maxStack - toSlot.quantity;
                    if (spaceAvailable > 0)
                    {
                        int moveAmount = Mathf.Min(fromSlot.quantity, spaceAvailable);
                        _inventory[toSlotIndex].quantity += moveAmount;
                        _inventory[fromSlotIndex].quantity -= moveAmount;
                        
                        if (_inventory[fromSlotIndex].quantity <= 0)
                        {
                            _inventory[fromSlotIndex] = new InventorySlot();
                        }
                    }
                    else
                    {
                        // Can't stack, swap instead
                        return SwapSlots(fromSlotIndex, toSlotIndex);
                    }
                }
                else
                {
                    // Can't stack, swap instead
                    return SwapSlots(fromSlotIndex, toSlotIndex);
                }
            }
            // Different items, swap
            else
            {
                return SwapSlots(fromSlotIndex, toSlotIndex);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Gets the hotbar size (always 9 slots for hotkeys 1-9, first row)
        /// </summary>
        public int GetHotbarSize()
        {
            return 9;
        }
        
        /// <summary>
        /// Gets a hotbar slot by index (0-8 for first row)
        /// </summary>
        public InventorySlot GetHotbarSlot(int hotbarIndex)
        {
            if (hotbarIndex < 0 || hotbarIndex >= 9)
                return null;
            
            return GetSlot(hotbarIndex);
        }
        
        /// <summary>
        /// Sets an item directly in a specific hotbar slot (0-8)
        /// Useful for ensuring items appear in the hotbar
        /// </summary>
        public bool SetHotbarSlot(int hotbarIndex, string itemID, int quantity = 1)
        {
            if (hotbarIndex < 0 || hotbarIndex >= GetHotbarSize())
            {
                Debug.LogWarning($"Hotbar index {hotbarIndex} is out of range (0-{GetHotbarSize() - 1})");
                return false;
            }
            
            if (string.IsNullOrEmpty(itemID) || quantity <= 0)
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData == null)
            {
                Debug.LogWarning($"Item {itemID} not found in database");
                return false;
            }
            
            // Set the item in the hotbar slot
            int maxStack = itemData.maxStackSize > 0 ? itemData.maxStackSize : quantity;
            int finalQuantity = Mathf.Min(quantity, maxStack);
            _inventory[hotbarIndex] = new InventorySlot(itemID, finalQuantity);
            
            OnItemAdded?.Invoke(_inventory[hotbarIndex], hotbarIndex);
            OnInventoryChanged?.Invoke();
            
            return true;
        }
        
        /// <summary>
        /// Equips an item from a hotbar slot (without removing from inventory)
        /// </summary>
        public bool EquipItemFromHotbar(int hotbarIndex)
        {
            InventorySlot slot = GetHotbarSlot(hotbarIndex);
            if (slot == null || slot.IsEmpty)
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(slot.itemID);
            if (itemData == null || itemData.itemType != ItemType.Equipment)
                return false;
            
            // Equip without removing from inventory (for hotbar quick-access)
            return EquipItemWithoutRemoving(itemData.itemID, itemData.equipmentType);
        }
        
        /// <summary>
        /// Equips an item without removing it from inventory (useful for hotbar)
        /// </summary>
        public bool EquipItemWithoutRemoving(string itemID, EquipmentType slot)
        {
            if (string.IsNullOrEmpty(itemID))
                return false;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
            if (itemData == null || itemData.itemType != ItemType.Equipment)
            {
                Debug.LogWarning($"Cannot equip {itemID}: not an equipment item");
                return false;
            }
            
            if (itemData.equipmentType != slot)
            {
                Debug.LogWarning($"Cannot equip {itemID}: equipment type mismatch (expected {slot}, got {itemData.equipmentType})");
                return false;
            }
            
            if (!HasItem(itemID, 1))
            {
                Debug.LogWarning($"Cannot equip {itemID}: not in inventory");
                return false;
            }
            
            // Unequip current item in slot if any
            string currentEquipped = _equippedItems.GetEquippedItem(slot);
            if (!string.IsNullOrEmpty(currentEquipped))
            {
                UnequipItem(slot);
            }
            
            // Equip without removing from inventory
            _equippedItems.Equip(slot, itemID);
            
            // Set global flags from item stats
            SetItemGlobalFlags(itemData, true);
            
            OnItemEquipped?.Invoke(slot, itemID);
            OnInventoryChanged?.Invoke();
            return true;
        }
    }
}

