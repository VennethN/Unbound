using UnityEngine;
using Unbound.Inventory;

namespace Unbound.Player
{
    /// <summary>
    /// Handles swapping the player's animator controller when equipment with custom animators is equipped/unequipped.
    /// Attach this component to the player GameObject alongside PlayerController2D.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimatorEquipment : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator playerAnimator;
        
        [Header("Default Animator")]
        [Tooltip("The default animator controller to use when no equipment overrides it. If not set, will use the animator's current controller on Start.")]
        [SerializeField] private RuntimeAnimatorController defaultAnimatorController;
        
        private RuntimeAnimatorController _originalController;
        private string _currentEquippedAnimatorItemID;
        
        private void Awake()
        {
            if (playerAnimator == null)
            {
                playerAnimator = GetComponent<Animator>();
            }
        }
        
        private void Start()
        {
            // Store the original animator controller
            if (defaultAnimatorController != null)
            {
                _originalController = defaultAnimatorController;
            }
            else if (playerAnimator != null)
            {
                _originalController = playerAnimator.runtimeAnimatorController;
            }
            
            // Subscribe to equipment events
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped += OnItemEquipped;
                InventoryManager.Instance.OnItemUnequipped += OnItemUnequipped;
                
                // Check if any currently equipped items have custom animators
                CheckCurrentEquipment();
            }
        }
        
        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped -= OnItemEquipped;
                InventoryManager.Instance.OnItemUnequipped -= OnItemUnequipped;
            }
        }
        
        private void CheckCurrentEquipment()
        {
            // Check all equipped items for custom animators
            foreach (EquipmentType slot in System.Enum.GetValues(typeof(EquipmentType)))
            {
                string itemID = InventoryManager.Instance.EquippedItems.GetEquippedItem(slot);
                if (!string.IsNullOrEmpty(itemID))
                {
                    ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);
                    RuntimeAnimatorController controller = itemData?.GetEquipAnimatorController();
                    if (controller != null)
                    {
                        ApplyAnimatorController(controller, itemID);
                        return; // Only apply one animator at a time
                    }
                }
            }
        }
        
        private void OnItemEquipped(EquipmentType slot, string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return;
            
            ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);
            if (itemData == null)
                return;
            
            // Check if this equipment has a custom animator
            RuntimeAnimatorController controller = itemData.GetEquipAnimatorController();
            if (controller != null)
            {
                ApplyAnimatorController(controller, itemID);
            }
        }
        
        private void OnItemUnequipped(EquipmentType slot)
        {
            // If the unequipped item was the one providing the current animator, revert to default
            // We need to check if any other equipped item has a custom animator
            string animatorItemID = null;
            RuntimeAnimatorController animatorController = null;
            
            foreach (EquipmentType equipSlot in System.Enum.GetValues(typeof(EquipmentType)))
            {
                string itemID = InventoryManager.Instance.EquippedItems.GetEquippedItem(equipSlot);
                if (!string.IsNullOrEmpty(itemID))
                {
                    ItemData itemData = ItemDatabase.Instance?.GetItem(itemID);
                    RuntimeAnimatorController controller = itemData?.GetEquipAnimatorController();
                    if (controller != null)
                    {
                        animatorItemID = itemID;
                        animatorController = controller;
                        break;
                    }
                }
            }
            
            if (animatorController != null)
            {
                // Another equipped item has a custom animator
                if (animatorItemID != _currentEquippedAnimatorItemID)
                {
                    ApplyAnimatorController(animatorController, animatorItemID);
                }
            }
            else
            {
                // No equipped items have custom animators, revert to default
                RevertToDefaultAnimator();
            }
        }
        
        private void ApplyAnimatorController(RuntimeAnimatorController controller, string itemID)
        {
            if (playerAnimator == null)
            {
                Debug.LogWarning("PlayerAnimatorEquipment: No animator assigned!");
                return;
            }
            
            if (controller == null)
            {
                Debug.LogWarning($"PlayerAnimatorEquipment: Animator controller is null for item: {itemID}");
                return;
            }
            
            playerAnimator.runtimeAnimatorController = controller;
            _currentEquippedAnimatorItemID = itemID;
            Debug.Log($"PlayerAnimatorEquipment: Applied animator controller '{controller.name}' (item: {itemID})");
        }
        
        private void RevertToDefaultAnimator()
        {
            if (playerAnimator == null)
                return;
            
            if (_originalController != null)
            {
                playerAnimator.runtimeAnimatorController = _originalController;
                _currentEquippedAnimatorItemID = null;
                Debug.Log("PlayerAnimatorEquipment: Reverted to default animator controller");
            }
        }
        
        /// <summary>
        /// Manually sets the default animator controller (useful for runtime changes)
        /// </summary>
        public void SetDefaultAnimatorController(RuntimeAnimatorController controller)
        {
            _originalController = controller;
            defaultAnimatorController = controller;
        }
        
        /// <summary>
        /// Gets the currently active animator controller
        /// </summary>
        public RuntimeAnimatorController GetCurrentAnimatorController()
        {
            return playerAnimator?.runtimeAnimatorController;
        }
        
        /// <summary>
        /// Gets the default animator controller
        /// </summary>
        public RuntimeAnimatorController GetDefaultAnimatorController()
        {
            return _originalController;
        }
        
        /// <summary>
        /// Forces a revert to the default animator regardless of equipped items
        /// </summary>
        public void ForceRevertToDefault()
        {
            RevertToDefaultAnimator();
        }
        
        /// <summary>
        /// Forces a re-check of current equipment and applies any custom animators
        /// </summary>
        public void RefreshEquipmentAnimator()
        {
            CheckCurrentEquipment();
        }
    }
}

