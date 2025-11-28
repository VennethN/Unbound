using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Unbound.Inventory;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// UI component for a single inventory slot
    /// </summary>
    public class InventorySlotUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Button slotButton;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color hotbarSelectedColor = Color.cyan;
        [SerializeField] private Color draggingColor = new Color(1f, 1f, 1f, 0.5f);
        
        private int _slotIndex = -1;
        private int _hotbarIndex = -1; // -1 means not a hotbar slot
        private InventorySlot _slot;
        private bool _isSelected = false;
        private bool _isHotbarSelected = false;
        
        // Drag and drop
        private GameObject _dragVisual;
        private Canvas _canvas;
        private bool _isDragging = false;
        
        public int SlotIndex => _slotIndex;
        public InventorySlot Slot => _slot;
        public bool IsEmpty => _slot == null || _slot.IsEmpty;
        
        public System.Action<InventorySlotUI> OnSlotClicked;
        public System.Action<InventorySlotUI> OnSlotSelected;
        public System.Action<InventorySlotUI, InventorySlotUI> OnSlotDropped;
        public System.Action<InventorySlotUI> OnSlotHoverEnter;
        public System.Action<InventorySlotUI> OnSlotHoverExit;
        
        private void Awake()
        {
            // Ensure the slot GameObject is always active
            gameObject.SetActive(true);

            if (slotButton != null)
            {
                slotButton.onClick.AddListener(OnSlotButtonClicked);
            }
            
            // Find canvas for drag visual
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                _canvas = FindFirstObjectByType<Canvas>();
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
            // Don't trigger click if we're dragging
            if (_isDragging)
                return;
                
            OnSlotClicked?.Invoke(this);
            OnSlotSelected?.Invoke(this);
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (IsEmpty)
                return;
            
            // Prevent dragging if item is currently equipped
            if (IsItemEquipped())
                return;
            
            _isDragging = true;
            
            // Create drag visual
            if (_canvas != null && iconImage != null && iconImage.sprite != null)
            {
                _dragVisual = new GameObject("DragVisual");
                _dragVisual.transform.SetParent(_canvas.transform, false);
                _dragVisual.transform.SetAsLastSibling();
                
                RectTransform dragRect = _dragVisual.AddComponent<RectTransform>();
                dragRect.sizeDelta = new Vector2(iconImage.rectTransform.rect.width, iconImage.rectTransform.rect.height);
                
                Image dragImage = _dragVisual.AddComponent<Image>();
                dragImage.sprite = iconImage.sprite;
                dragImage.color = draggingColor;
                dragImage.raycastTarget = false;
                
                // Add quantity text if needed
                if (quantityText != null && _slot != null && _slot.quantity > 1)
                {
                    GameObject quantityObj = new GameObject("Quantity");
                    quantityObj.transform.SetParent(_dragVisual.transform, false);
                    RectTransform quantityRect = quantityObj.AddComponent<RectTransform>();
                    quantityRect.anchorMin = new Vector2(1f, 0f);
                    quantityRect.anchorMax = new Vector2(1f, 0f);
                    quantityRect.pivot = new Vector2(1f, 0f);
                    quantityRect.anchoredPosition = Vector2.zero;
                    
                    TextMeshProUGUI dragQuantity = quantityObj.AddComponent<TextMeshProUGUI>();
                    dragQuantity.text = _slot.quantity.ToString();
                    dragQuantity.fontSize = quantityText.fontSize;
                    dragQuantity.color = quantityText.color;
                    dragQuantity.alignment = TextAlignmentOptions.BottomRight;
                }
                
                // Make original icon semi-transparent
                if (iconImage != null)
                {
                    iconImage.color = draggingColor;
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (_dragVisual != null && _canvas != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvas.transform as RectTransform,
                    eventData.position,
                    _canvas.worldCamera,
                    out Vector2 localPoint);
                _dragVisual.transform.localPosition = localPoint;
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            
            // Restore original icon color
            if (iconImage != null && !IsEmpty)
            {
                iconImage.color = normalColor;
            }
            
            // Destroy drag visual
            if (_dragVisual != null)
            {
                Destroy(_dragVisual);
                _dragVisual = null;
            }
            
            // Check if we dropped on a valid slot
            GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
            if (dropTarget != null)
            {
                InventorySlotUI targetSlot = dropTarget.GetComponent<InventorySlotUI>();
                if (targetSlot == null)
                {
                    // Try parent
                    targetSlot = dropTarget.GetComponentInParent<InventorySlotUI>();
                }
                
                if (targetSlot != null && targetSlot != this)
                {
                    OnSlotDropped?.Invoke(this, targetSlot);
                }
            }
        }
        
        public void OnDrop(PointerEventData eventData)
        {
            // This is handled in OnEndDrag, but we implement IDropHandler for proper event handling
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Only trigger hover if slot has an item and we're not dragging
            if (!IsEmpty && !_isDragging)
            {
                OnSlotHoverEnter?.Invoke(this);
            }
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            OnSlotHoverExit?.Invoke(this);
        }
        
        /// <summary>
        /// Checks if the item in this slot is currently equipped
        /// </summary>
        private bool IsItemEquipped()
        {
            if (IsEmpty || _slot == null || string.IsNullOrEmpty(_slot.itemID))
                return false;
            
            // Check if it's an equipment item
            ItemData itemData = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItem(_slot.itemID) : null;
            if (itemData == null || itemData.itemType != ItemType.Equipment)
                return false;
            
            // Check if this item is currently equipped
            if (InventoryManager.Instance != null)
            {
                EquippedItems equippedItems = InventoryManager.Instance.EquippedItems;
                if (equippedItems != null)
                {
                    string equippedItemID = equippedItems.GetEquippedItem(itemData.equipmentType);
                    return equippedItemID == _slot.itemID;
                }
            }
            
            return false;
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

