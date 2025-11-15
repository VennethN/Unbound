using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unbound.Inventory;

namespace Unbound.Inventory.UI
{
    /// <summary>
    /// Panel that displays item description when an item is focused/clicked
    /// </summary>
    public class ItemDescriptionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI itemDescriptionText;
        [SerializeField] private TextMeshProUGUI itemStatsText;
        [SerializeField] private Image itemIconImage;
        [SerializeField] private GameObject panelRoot;
        
        [Header("Equipment UI")]
        [SerializeField] private GameObject equipmentInfoPanel;
        [SerializeField] private TextMeshProUGUI equipmentTypeText;
        
        [Header("Consumable UI")]
        [SerializeField] private GameObject consumableInfoPanel;
        [SerializeField] private TextMeshProUGUI consumableEffectText;
        
        [Header("Action Buttons")]
        [SerializeField] private Button equipButton;
        [SerializeField] private Button useButton;
        
        [Header("Positioning")]
        [SerializeField] private float offsetX = 10f;
        [SerializeField] private float offsetY = 0f;
        
        private RectTransform _rectTransform;
        private ItemData _currentItemData;
        private InventorySlotUI _currentSlot;
        
        public System.Action<ItemData> OnEquipClicked;
        public System.Action<ItemData> OnUseClicked;
        
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
            {
                _rectTransform = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
            }
            
            if (equipButton != null)
            {
                equipButton.onClick.AddListener(OnEquipButtonClicked);
            }
            
            if (useButton != null)
            {
                useButton.onClick.AddListener(OnUseButtonClicked);
            }
            
            Hide();
        }
        
        /// <summary>
        /// Shows the description for an item and positions the panel next to the slot
        /// </summary>
        public void ShowItem(ItemData itemData, InventorySlotUI slotUI = null)
        {
            if (itemData == null)
            {
                Hide();
                return;
            }
            
            _currentItemData = itemData;
            _currentSlot = slotUI;
            
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
            
            // Position panel next to the slot
            if (slotUI != null && _rectTransform != null)
            {
                PositionNextToSlot(slotUI);
            }
            
            // Set name and description
            if (itemNameText != null)
            {
                itemNameText.text = itemData.name;
            }
            
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = itemData.description;
            }
            
            // Set icon
            if (itemIconImage != null)
            {
                Sprite iconSprite = ItemDatabase.Instance.GetItemSprite(itemData.itemID);
                if (iconSprite != null)
                {
                    itemIconImage.sprite = iconSprite;
                    itemIconImage.gameObject.SetActive(true);
                }
                else
                {
                    itemIconImage.gameObject.SetActive(false);
                }
            }
            
            // Show/hide equip button
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(itemData.itemType == ItemType.Equipment);
            }
            
            // Show/hide use button
            if (useButton != null)
            {
                useButton.gameObject.SetActive(itemData.itemType == ItemType.Consumable);
            }
            
            // Show equipment-specific info
            if (equipmentInfoPanel != null)
            {
                equipmentInfoPanel.SetActive(itemData.itemType == ItemType.Equipment);
            }
            
            if (itemData.itemType == ItemType.Equipment)
            {
                if (equipmentTypeText != null)
                {
                    equipmentTypeText.text = $"Type: {itemData.equipmentType}";
                }
                
                // Display stats
                if (itemStatsText != null && itemData.stats != null)
                {
                    string statsString = "";
                    ItemStats stats = itemData.stats;
                    
                    if (stats.maxHealth != 0) statsString += $"Max Health: +{stats.maxHealth}\n";
                    if (stats.healthRegen != 0) statsString += $"Health Regen: +{stats.healthRegen}\n";
                    if (stats.attackDamage != 0) statsString += $"Attack Damage: +{stats.attackDamage}\n";
                    if (stats.moveSpeed != 0) statsString += $"Move Speed: +{stats.moveSpeed}\n";
                    if (stats.attackSpeed != 0) statsString += $"Attack Speed: +{stats.attackSpeed}\n";
                    if (stats.attackRange != 0) statsString += $"Attack Range: +{stats.attackRange}\n";
                    
                    if (stats.globalFlags != null && stats.globalFlags.Count > 0)
                    {
                        statsString += "\nFlags: ";
                        statsString += string.Join(", ", stats.globalFlags);
                    }
                    
                    itemStatsText.text = statsString;
                }
            }
            
            // Show consumable-specific info
            if (consumableInfoPanel != null)
            {
                consumableInfoPanel.SetActive(itemData.itemType == ItemType.Consumable);
            }
            
            if (itemData.itemType == ItemType.Consumable && itemData.consumableEffect != null)
            {
                if (consumableEffectText != null)
                {
                    string effectString = "";
                    ConsumableEffect effect = itemData.consumableEffect;
                    
                    if (effect.effectType == ConsumableEffectType.RestoreHealth || effect.effectType == ConsumableEffectType.Both)
                    {
                        effectString += $"Restores {effect.healthAmount} health\n";
                    }
                    
                    if (effect.effectType == ConsumableEffectType.GiveItems || effect.effectType == ConsumableEffectType.Both)
                    {
                        if (effect.itemsToGive != null && effect.itemsToGive.Count > 0)
                        {
                            effectString += "Gives:\n";
                            foreach (var reward in effect.itemsToGive)
                            {
                                ItemData rewardItem = ItemDatabase.Instance.GetItem(reward.itemID);
                                string rewardName = rewardItem != null ? rewardItem.name : reward.itemID;
                                effectString += $"  - {rewardName} x{reward.quantity}\n";
                            }
                        }
                    }
                    
                    consumableEffectText.text = effectString;
                }
            }
        }
        
        /// <summary>
        /// Positions the panel next to the specified slot
        /// </summary>
        private void PositionNextToSlot(InventorySlotUI slotUI)
        {
            if (slotUI == null || _rectTransform == null) return;
            
            RectTransform slotRect = slotUI.GetComponent<RectTransform>();
            if (slotRect == null) return;
            
            // Get the canvas to determine coordinate space
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return;
            
            // Convert slot position to canvas local space
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(
                canvas.worldCamera != null ? canvas.worldCamera : UnityEngine.Camera.main,
                slotRect.position
            );
            
            Vector2 slotCanvasPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                slotScreenPos,
                canvas.worldCamera != null ? canvas.worldCamera : UnityEngine.Camera.main,
                out slotCanvasPos
            );
            
            // Position panel to the right of the slot
            float panelWidth = _rectTransform.rect.width;
            float slotWidth = slotRect.rect.width;
            
            // Set the panel's parent to canvas if not already
            if (_rectTransform.parent != canvasRect)
            {
                _rectTransform.SetParent(canvasRect, false);
            }
            
            _rectTransform.anchoredPosition = new Vector2(
                slotCanvasPos.x + slotWidth / 2 + panelWidth / 2 + offsetX,
                slotCanvasPos.y + offsetY
            );
        }
        
        private void OnEquipButtonClicked()
        {
            if (_currentItemData != null)
            {
                OnEquipClicked?.Invoke(_currentItemData);
            }
        }
        
        private void OnUseButtonClicked()
        {
            if (_currentItemData != null)
            {
                OnUseClicked?.Invoke(_currentItemData);
            }
        }
        
        /// <summary>
        /// Hides the description panel
        /// </summary>
        public void Hide()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
            _currentItemData = null;
            _currentSlot = null;
        }
    }
}

