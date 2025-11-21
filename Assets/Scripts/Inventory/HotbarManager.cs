using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unbound.Inventory
{
    /// <summary>
    /// Manages the hotbar (first row of inventory) and hotkey input
    /// </summary>
    public class HotbarManager : MonoBehaviour
    {
        private static HotbarManager _instance;
        
        [Header("Hotbar Settings")]
        [SerializeField] private int hotbarSize = 9; // Max 9 for keys 1-9
        
        private int _selectedHotbarSlot = -1;
        
        public static HotbarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<HotbarManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HotbarManager");
                        _instance = go.AddComponent<HotbarManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        public int HotbarSize => hotbarSize;
        public int SelectedHotbarSlot => _selectedHotbarSlot;
        
        public event Action<int> OnHotbarSlotSelected;
        public event Action<int, string> OnHotbarItemUsed;
        
#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
        private InputAction[] _hotkeyActions = new InputAction[9];
#endif
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
            if (_playerInput == null)
            {
                _playerInput = gameObject.AddComponent<PlayerInput>();
            }
            
            InitializeHotkeys();
#endif
        }
        
#if ENABLE_INPUT_SYSTEM
        private void InitializeHotkeys()
        {
            // Create hotkey actions for numbers 1-9
            for (int i = 0; i < 9; i++)
            {
                int slotIndex = i; // Capture for closure
                string keyName = (i + 1).ToString();
                
                // Try to find existing action or create new one
                InputAction hotkeyAction = _playerInput.actions?.FindAction($"Hotkey{keyName}", false);
                if (hotkeyAction == null)
                {
                    // Create action map if needed
                    if (_playerInput.actions == null)
                    {
                        var actionMap = new InputActionMap("Hotbar");
                        _playerInput.actions = new InputActionAsset();
                        _playerInput.actions.AddActionMap(actionMap);
                    }
                    
                    hotkeyAction = _playerInput.actions.FindAction($"Hotkey{keyName}", false);
                }
                
                _hotkeyActions[i] = hotkeyAction;
            }
        }
#endif
        
        private void OnEnable()
        {
            // Enable hotkey checking via Update
        }
        
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            // Check for number key presses (1-9) using new Input System
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    Key key = (Key)((int)Key.Digit1 + i);
                    if (keyboard[key].wasPressedThisFrame)
                    {
                        UseHotbarSlot(i);
                    }
                }
            }
#else
            // Fallback to old input system if Input System is not enabled
            for (int i = 0; i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    UseHotbarSlot(i);
                }
            }
#endif
        }
        
        /// <summary>
        /// Uses the item in the specified hotbar slot
        /// </summary>
        public void UseHotbarSlot(int slotIndex)
        {
            // Get actual hotbar size from inventory (first row)
            int actualHotbarSize = InventoryManager.Instance != null 
                ? InventoryManager.Instance.GetHotbarSize() 
                : hotbarSize;
            
            if (slotIndex < 0 || slotIndex >= actualHotbarSize)
                return;
            
            if (InventoryManager.Instance == null)
                return;
            
            // If switching to a different hotkey, unequip the item from the previously selected hotkey
            if (_selectedHotbarSlot != slotIndex && _selectedHotbarSlot >= 0)
            {
                InventorySlot previousSlot = InventoryManager.Instance.GetSlot(_selectedHotbarSlot);
                if (previousSlot != null && !previousSlot.IsEmpty)
                {
                    ItemData previousItemData = ItemDatabase.Instance.GetItem(previousSlot.itemID);
                    if (previousItemData != null && previousItemData.itemType == ItemType.Equipment)
                    {
                        // Unequip the equipment from the previously selected hotkey
                        InventoryManager.Instance.UnequipItem(previousItemData.equipmentType);
                    }
                }
            }
            
            // Get the slot index in the full inventory (first row)
            int inventorySlotIndex = slotIndex;
            InventorySlot slot = InventoryManager.Instance.GetSlot(inventorySlotIndex);
            
            if (slot == null || slot.IsEmpty)
            {
                // Deselect if slot is empty
                _selectedHotbarSlot = -1;
                OnHotbarSlotSelected?.Invoke(-1);
                return;
            }
            
            ItemData itemData = ItemDatabase.Instance.GetItem(slot.itemID);
            if (itemData == null)
                return;
            
            _selectedHotbarSlot = slotIndex;
            OnHotbarSlotSelected?.Invoke(slotIndex);
            
            // If it's equipment, equip it (without removing from hotbar)
            if (itemData.itemType == ItemType.Equipment)
            {
                InventoryManager.Instance.EquipItemFromHotbar(slotIndex);
            }
            // If it's consumable, consume it
            else if (itemData.itemType == ItemType.Consumable)
            {
                InventoryManager.Instance.ConsumeItem(itemData.itemID);
            }
            
            OnHotbarItemUsed?.Invoke(slotIndex, slot.itemID);
        }
        
        /// <summary>
        /// Gets the item ID in the specified hotbar slot
        /// </summary>
        public string GetHotbarItemID(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= hotbarSize)
                return null;
            
            if (InventoryManager.Instance == null)
                return null;
            
            InventorySlot slot = InventoryManager.Instance.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
                return null;
            
            return slot.itemID;
        }
        
        /// <summary>
        /// Gets the inventory slot index for a hotbar slot
        /// </summary>
        public int GetInventorySlotIndex(int hotbarSlotIndex)
        {
            if (hotbarSlotIndex < 0 || hotbarSlotIndex >= hotbarSize)
                return -1;
            
            return hotbarSlotIndex;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}

