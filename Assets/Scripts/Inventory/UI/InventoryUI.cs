using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unbound.Inventory;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// Grid-based inventory UI controller
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;
        [SerializeField] private ItemDescriptionPanel descriptionPanel;
        
        [Header("Settings")]
        [SerializeField] private int slotsPerRow = 6;
        
        private List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
        private InventorySlotUI _selectedSlot = null;
        
        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RefreshInventory;
                RefreshInventory();
            }
        }
        
        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RefreshInventory;
            }

            // Ensure description panel closes when inventory UI is closed/disabled
            HideDescriptionPanel();
        }
        
        private void Awake()
        {
            InitializeSlots();
            
            // Subscribe to description panel button callbacks
            if (descriptionPanel != null)
            {
                descriptionPanel.OnEquipClicked += OnEquipItem;
                descriptionPanel.OnUseClicked += OnUseItem;
            }
        }
        
        private void Start()
        {
            // Ensure we refresh after everything is initialized
            RefreshInventory();
        }
        
        private void OnDestroy()
        {
            if (descriptionPanel != null)
            {
                descriptionPanel.OnEquipClicked -= OnEquipItem;
                descriptionPanel.OnUseClicked -= OnUseItem;
            }
        }
        
        /// <summary>
        /// Initializes the inventory slot grid
        /// </summary>
        private void InitializeSlots()
        {
            if (slotContainer == null || slotPrefab == null)
            {
                Debug.LogWarning("InventoryUI: slotContainer or slotPrefab not assigned");
                return;
            }
            
            // Clear existing slots
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
            _slotUIs.Clear();
            
            // Create slots
            int totalSlots = InventoryManager.Instance != null ? InventoryManager.Instance.InventorySize : 48;
            
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
                
                if (slotUI == null)
                {
                    slotUI = slotObj.AddComponent<InventorySlotUI>();
                }
                
                slotUI.Initialize(i);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUI.OnSlotSelected += OnSlotSelected;
                
                _slotUIs.Add(slotUI);
            }
            
            RefreshInventory();
        }
        
        /// <summary>
        /// Refreshes the inventory display
        /// </summary>
        private void RefreshInventory()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("InventoryUI: InventoryManager.Instance is null, cannot refresh");
                return;
            }
            
            if (_slotUIs == null || _slotUIs.Count == 0)
            {
                Debug.LogWarning("InventoryUI: Slot UIs not initialized yet");
                return;
            }
            
            List<InventorySlot> slots = InventoryManager.Instance.GetAllSlots();
            
            if (slots == null)
            {
                Debug.LogWarning("InventoryUI: Got null slots list from InventoryManager");
                return;
            }
            
            for (int i = 0; i < _slotUIs.Count && i < slots.Count; i++)
            {
                if (_slotUIs[i] != null)
                {
                    _slotUIs[i].UpdateSlot(slots[i]);
                }
            }
        }
        
        /// <summary>
        /// Public method to manually refresh the inventory UI
        /// </summary>
        public void Refresh()
        {
            RefreshInventory();
        }
        
        /// <summary>
        /// Handles slot click events
        /// </summary>
        private void OnSlotClicked(InventorySlotUI slotUI)
        {
            if (slotUI == null || slotUI.IsEmpty)
            {
                HideDescriptionPanel();
                _selectedSlot = null;
                return;
            }
            
            // Deselect previous slot
            if (_selectedSlot != null && _selectedSlot != slotUI)
            {
                _selectedSlot.SetSelected(false);
            }
            
            _selectedSlot = slotUI;
            slotUI.SetSelected(true);
            
            // Handle item actions based on type
            ItemData itemData = ItemDatabase.Instance.GetItem(slotUI.Slot.itemID);
            if (itemData == null) return;
            
            ShowDescriptionPanel(itemData, slotUI);
        }
        
        /// <summary>
        /// Handles slot selection (focus) - now only updates selection visual
        /// </summary>
        private void OnSlotSelected(InventorySlotUI slotUI)
        {
            // This is called on click, but we handle everything in OnSlotClicked
            // Keeping for compatibility but selection is handled there
        }

        /// <summary>
        /// Ensures description panel is active and displays the item
        /// </summary>
        private void ShowDescriptionPanel(ItemData itemData, InventorySlotUI slotUI)
        {
            if (descriptionPanel == null || itemData == null)
                return;

            if (!descriptionPanel.gameObject.activeSelf)
            {
                descriptionPanel.gameObject.SetActive(true);
            }

            descriptionPanel.ShowItem(itemData, slotUI);
        }

        /// <summary>
        /// Hides the description panel safely
        /// </summary>
        private void HideDescriptionPanel()
        {
            if (descriptionPanel != null)
            {
                descriptionPanel.Hide();
            }
        }
        
        /// <summary>
        /// Handles equip button click from description panel
        /// </summary>
        private void OnEquipItem(ItemData itemData)
        {
            if (itemData == null || itemData.itemType != ItemType.Equipment)
                return;
            
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.EquipItem(itemData.itemID, itemData.equipmentType);
                
                // Hide panel after equipping
                if (descriptionPanel != null)
                {
                    descriptionPanel.Hide();
                }
                
                // Refresh inventory to update slot display
                RefreshInventory();
            }
        }
        
        /// <summary>
        /// Handles use button click from description panel
        /// </summary>
        private void OnUseItem(ItemData itemData)
        {
            if (itemData == null || itemData.itemType != ItemType.Consumable)
                return;
            
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ConsumeItem(itemData.itemID);
                
                // Hide panel after using
                if (descriptionPanel != null)
                {
                    descriptionPanel.Hide();
                }
                
                // Refresh inventory to update slot display
                RefreshInventory();
            }
        }
        
        /// <summary>
        /// Equips the selected item (can be called from UI button)
        /// </summary>
        public void EquipSelectedItem()
        {
            if (_selectedSlot == null || _selectedSlot.IsEmpty)
                return;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(_selectedSlot.Slot.itemID);
            if (itemData == null || itemData.itemType != ItemType.Equipment)
                return;
            
            InventoryManager.Instance.EquipItem(itemData.itemID, itemData.equipmentType);
        }
        
        /// <summary>
        /// Consumes the selected item (can be called from UI button)
        /// </summary>
        public void ConsumeSelectedItem()
        {
            if (_selectedSlot == null || _selectedSlot.IsEmpty)
                return;
            
            ItemData itemData = ItemDatabase.Instance.GetItem(_selectedSlot.Slot.itemID);
            if (itemData == null || itemData.itemType != ItemType.Consumable)
                return;
            
            InventoryManager.Instance.ConsumeItem(itemData.itemID);
        }
    }
}

