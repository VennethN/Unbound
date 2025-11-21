using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// UI component for a single inventory slot
    /// </summary>
    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Button slotButton;
        [SerializeField] private GameObject emptySlotVisual;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color hotbarSelectedColor = Color.cyan;
        
        private int _slotIndex = -1;
        private int _hotbarIndex = -1; // -1 means not a hotbar slot
        private InventorySlot _slot;
        private bool _isSelected = false;
        private bool _isHotbarSelected = false;
        
        public int SlotIndex => _slotIndex;
        public InventorySlot Slot => _slot;
        public bool IsEmpty => _slot == null || _slot.IsEmpty;
        
        public System.Action<InventorySlotUI> OnSlotClicked;
        public System.Action<InventorySlotUI> OnSlotSelected;
        
        private void Awake()
        {
            // Ensure the slot GameObject is always active
            gameObject.SetActive(true);

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnSlotButtonClicked);
            }
            
            UpdateVisuals();
        }
        
        /// <summary>
        /// Initializes the slot with an index and optional hotbar index
        /// </summary>
        public void Initialize(int slotIndex, int hotbarIndex = -1)
        {
            _slotIndex = slotIndex;
            _hotbarIndex = hotbarIndex;
            UpdateVisuals();
        }
        
        /// <summary>
        /// Updates the slot with new inventory slot data
        /// </summary>
        public void UpdateSlot(InventorySlot slot)
        {
            _slot = slot;
            // Ensure slot GameObject stays active
            gameObject.SetActive(true);
            UpdateVisuals();
        }
        
        /// <summary>
        /// Sets the selected state of this slot
        /// </summary>
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisuals();
        }
        
        /// <summary>
        /// Sets the hotbar selected state (for hotbar slots)
        /// </summary>
        public void SetHotbarSelected(bool selected)
        {
            _isHotbarSelected = selected;
            UpdateVisuals();
        }
        
        private void OnSlotButtonClicked()
        {
            OnSlotClicked?.Invoke(this);
            OnSlotSelected?.Invoke(this);
        }
        
        private void UpdateVisuals()
        {
            bool isEmpty = IsEmpty;
            
            // Always keep the slot button enabled and visible - slots should always be interactive
            if (slotButton != null)
            {
                slotButton.interactable = true;
                slotButton.gameObject.SetActive(true);
            }

            // Update icon - show item icon if slot has an item
            if (iconImage != null)
            {
                if (!isEmpty && _slot != null && !string.IsNullOrEmpty(_slot.itemID))
                {
                    ItemData itemData = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItem(_slot.itemID) : null;
                    if (itemData != null)
                    {
                        Sprite iconSprite = ItemDatabase.Instance.GetItemSprite(_slot.itemID);
                        if (iconSprite != null)
                        {
                            iconImage.sprite = iconSprite;
                            iconImage.color = normalColor;
                            iconImage.gameObject.SetActive(true);
                        }
                        else
                        {
                            // Sprite not found - log warning for debugging
                            Debug.LogWarning($"InventorySlotUI: Could not find sprite for item {_slot.itemID} (spriteID: {itemData.spriteID}, iconPath: {itemData.iconPath})");
                            iconImage.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"InventorySlotUI: Could not find item data for itemID: {_slot.itemID}");
                        iconImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    // Empty slot - hide icon but keep slot visible
                    iconImage.gameObject.SetActive(false);
                }
            }
            
            // Update quantity text
            if (quantityText != null)
            {
                if (!isEmpty && _slot != null && _slot.quantity > 1)
                {
                    quantityText.text = _slot.quantity.ToString();
                    quantityText.gameObject.SetActive(true);
                }
                else
                {
                    quantityText.gameObject.SetActive(false);
                }
            }
            
            // Update hotkey text (for hotbar slots)
            if (hotkeyText != null)
            {
                if (_hotbarIndex >= 0)
                {
                    hotkeyText.text = (_hotbarIndex + 1).ToString();
                    hotkeyText.gameObject.SetActive(true);
                }
                else
                {
                    hotkeyText.gameObject.SetActive(false);
                }
            }
            
            // Update selection visual
            if (slotButton != null)
            {
                var colors = slotButton.colors;
                if (_isHotbarSelected)
                {
                    colors.normalColor = hotbarSelectedColor;
                }
                else if (_isSelected)
                {
                    colors.normalColor = selectedColor;
                }
                else
                {
                    colors.normalColor = normalColor;
                }
                slotButton.colors = colors;
            }
        }
    }
}

