using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbound.Inventory;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// Panel showing equipped items
    /// </summary>
    public class EquipmentPanel : MonoBehaviour
    {
        [System.Serializable]
        public class EquipmentSlotUI
        {
            public EquipmentType equipmentType;
            public Image iconImage;
            public TextMeshProUGUI itemNameText;
            public Button unequipButton;
        }
        
        [Header("Equipment Slots")]
        [SerializeField] private List<EquipmentSlotUI> equipmentSlots = new List<EquipmentSlotUI>();
        
        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped += OnEquipmentChanged;
                InventoryManager.Instance.OnItemUnequipped += OnEquipmentChanged;
                RefreshEquipment();
            }
        }
        
        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped -= OnEquipmentChanged;
                InventoryManager.Instance.OnItemUnequipped -= OnEquipmentChanged;
            }
        }
        
        private void Awake()
        {
            // Setup unequip buttons
            foreach (var slotUI in equipmentSlots)
            {
                if (slotUI.unequipButton != null)
                {
                    EquipmentType type = slotUI.equipmentType; // Capture for closure
                    slotUI.unequipButton.onClick.AddListener(() => UnequipItem(type));
                }
            }
        }
        
        /// <summary>
        /// Refreshes the equipment display
        /// </summary>
        private void RefreshEquipment()
        {
            if (InventoryManager.Instance == null) return;
            
            EquippedItems equipped = InventoryManager.Instance.EquippedItems;
            
            foreach (var slotUI in equipmentSlots)
            {
                string itemID = equipped.GetEquippedItem(slotUI.equipmentType);
                
                if (!string.IsNullOrEmpty(itemID))
                {
                    ItemData itemData = ItemDatabase.Instance.GetItem(itemID);
                    
                    // Update icon
                    if (slotUI.iconImage != null)
                    {
                        Sprite iconSprite = ItemDatabase.Instance.GetItemSprite(itemID);
                        if (iconSprite != null)
                        {
                            slotUI.iconImage.sprite = iconSprite;
                            slotUI.iconImage.gameObject.SetActive(true);
                        }
                        else
                        {
                            slotUI.iconImage.gameObject.SetActive(false);
                        }
                    }
                    
                    // Update name
                    if (slotUI.itemNameText != null)
                    {
                        slotUI.itemNameText.text = itemData != null ? itemData.name : itemID;
                    }
                    
                    // Show unequip button
                    if (slotUI.unequipButton != null)
                    {
                        slotUI.unequipButton.gameObject.SetActive(true);
                    }
                }
                else
                {
                    // Empty slot
                    if (slotUI.iconImage != null)
                    {
                        slotUI.iconImage.gameObject.SetActive(false);
                    }
                    
                    if (slotUI.itemNameText != null)
                    {
                        slotUI.itemNameText.text = "Empty";
                    }
                    
                    if (slotUI.unequipButton != null)
                    {
                        slotUI.unequipButton.gameObject.SetActive(false);
                    }
                }
            }
        }
        
        private void OnEquipmentChanged(EquipmentType slot, string itemID)
        {
            RefreshEquipment();
        }
        
        private void OnEquipmentChanged(EquipmentType slot)
        {
            RefreshEquipment();
        }
        
        /// <summary>
        /// Unequips an item from a slot
        /// </summary>
        public void UnequipItem(EquipmentType slot)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.UnequipItem(slot);
            }
        }
    }
}

