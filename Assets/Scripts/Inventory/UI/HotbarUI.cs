using System.Collections.Generic;
using UnityEngine;
using Unbound.Inventory;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// Separate hotbar UI that displays the first 9 inventory slots
    /// This hotbar can be visible independently from the main inventory UI
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform slotContainer;
        [SerializeField] private GameObject slotPrefab;
        
        private List<InventorySlotUI> _slotUIs = new List<InventorySlotUI>();
        
        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged += RefreshHotbar;
                RefreshHotbar();
            }
            
            // Subscribe to hotbar selection events
            if (HotbarManager.Instance != null)
            {
                HotbarManager.Instance.OnHotbarSlotSelected += OnHotbarSlotSelected;
            }
        }
        
        private void OnDisable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= RefreshHotbar;
            }
            
            if (HotbarManager.Instance != null)
            {
                HotbarManager.Instance.OnHotbarSlotSelected -= OnHotbarSlotSelected;
            }
        }
        
        private void Awake()
        {
            InitializeSlots();
        }
        
        private void Start()
        {
            // Ensure we refresh after everything is initialized
            RefreshHotbar();
        }
        
        /// <summary>
        /// Initializes the hotbar slots (9 slots for keys 1-9)
        /// </summary>
        private void InitializeSlots()
        {
            if (slotContainer == null || slotPrefab == null)
            {
                Debug.LogWarning("HotbarUI: slotContainer or slotPrefab not assigned");
                return;
            }
            
            // Clear existing slots
            foreach (Transform child in slotContainer)
            {
                Destroy(child.gameObject);
            }
            _slotUIs.Clear();
            
            // Create 9 hotbar slots
            int hotbarSize = 9;
            
            for (int i = 0; i < hotbarSize; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotContainer);
                InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
                
                if (slotUI == null)
                {
                    slotUI = slotObj.AddComponent<InventorySlotUI>();
                }
                
                // Initialize as hotbar slot (hotbarIndex = i, slotIndex = i)
                slotUI.Initialize(i, i);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUI.OnSlotDropped += OnSlotDropped;
                
                _slotUIs.Add(slotUI);
            }
            
            RefreshHotbar();
        }
        
        /// <summary>
        /// Refreshes the hotbar display with current inventory data
        /// </summary>
        private void RefreshHotbar()
        {
            if (InventoryManager.Instance == null)
            {
                Debug.LogWarning("HotbarUI: InventoryManager.Instance is null, cannot refresh");
                return;
            }
            
            if (_slotUIs == null || _slotUIs.Count == 0)
            {
                Debug.LogWarning("HotbarUI: Slot UIs not initialized yet");
                return;
            }
            
            // Update each hotbar slot with inventory data
            for (int i = 0; i < _slotUIs.Count && i < 9; i++)
            {
                if (_slotUIs[i] != null)
                {
                    InventorySlot slot = InventoryManager.Instance.GetSlot(i);
                    if (slot != null)
                    {
                        _slotUIs[i].UpdateSlot(slot);
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles slot click events
        /// </summary>
        private void OnSlotClicked(InventorySlotUI slotUI)
        {
            if (slotUI == null || slotUI.IsEmpty)
                return;
            
            // Use the hotbar slot when clicked
            if (HotbarManager.Instance != null)
            {
                HotbarManager.Instance.UseHotbarSlot(slotUI.SlotIndex);
            }
        }
        
        /// <summary>
        /// Handles hotbar slot selection from HotbarManager
        /// </summary>
        private void OnHotbarSlotSelected(int hotbarIndex)
        {
            // Update visual selection for hotbar slots
            for (int i = 0; i < _slotUIs.Count; i++)
            {
                if (_slotUIs[i] != null)
                {
                    _slotUIs[i].SetHotbarSelected(i == hotbarIndex);
                }
            }
        }
        
        /// <summary>
        /// Handles drag and drop between slots
        /// </summary>
        private void OnSlotDropped(InventorySlotUI fromSlot, InventorySlotUI toSlot)
        {
            if (fromSlot == null || toSlot == null)
                return;
            
            if (InventoryManager.Instance == null)
                return;
            
            // Move/swap items
            bool success = InventoryManager.Instance.MoveItem(fromSlot.SlotIndex, toSlot.SlotIndex);
            
            if (success)
            {
                // Refresh hotbar to update visuals
                RefreshHotbar();
            }
        }
        
        /// <summary>
        /// Public method to manually refresh the hotbar UI
        /// </summary>
        public void Refresh()
        {
            RefreshHotbar();
        }
    }
}

