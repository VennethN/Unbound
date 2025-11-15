using UnityEngine;
using Unbound.Inventory;
using Unbound.Utility;

namespace Unbound.Player
{
    /// <summary>
    /// Manages player stats with equipment bonuses (additive stacking)
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float baseMaxHealth = 100f;
        [SerializeField] private float baseHealthRegen = 0f;
        [SerializeField] private float baseAttackDamage = 10f;
        [SerializeField] private float baseMoveSpeed = 3.5f;
        [SerializeField] private float baseAttackSpeed = 1f;
        [SerializeField] private float baseAttackRange = 1f;
        
        [Header("Current Stats (Read-only)")]
        [ReadOnly] [SerializeField] private float currentMaxHealth;
        [ReadOnly] [SerializeField] private float currentHealthRegen;
        [ReadOnly] [SerializeField] private float currentAttackDamage;
        [ReadOnly] [SerializeField] private float currentMoveSpeed;
        [ReadOnly] [SerializeField] private float currentAttackSpeed;
        [ReadOnly] [SerializeField] private float currentAttackRange;
        
        private ItemStats _equipmentStats;
        
        public float MaxHealth => currentMaxHealth;
        public float HealthRegen => currentHealthRegen;
        public float AttackDamage => currentAttackDamage;
        public float MoveSpeed => currentMoveSpeed;
        public float AttackSpeed => currentAttackSpeed;
        public float AttackRange => currentAttackRange;
        
        private void Awake()
        {
            UpdateStats();
        }
        
        private void OnEnable()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemEquipped += OnEquipmentChanged;
                InventoryManager.Instance.OnItemUnequipped += OnEquipmentChanged;
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
        
        private void OnEquipmentChanged(EquipmentType slot, string itemID)
        {
            UpdateStats();
        }
        
        private void OnEquipmentChanged(EquipmentType slot)
        {
            UpdateStats();
        }
        
        /// <summary>
        /// Updates stats by calculating base stats + equipment bonuses
        /// </summary>
        public void UpdateStats()
        {
            // Get equipment stats from inventory manager
            if (InventoryManager.Instance != null)
            {
                _equipmentStats = InventoryManager.Instance.GetTotalEquipmentStats();
            }
            else
            {
                _equipmentStats = new ItemStats();
            }
            
            // Calculate total stats (base + equipment bonuses, additive)
            currentMaxHealth = baseMaxHealth + (_equipmentStats?.maxHealth ?? 0f);
            currentHealthRegen = baseHealthRegen + (_equipmentStats?.healthRegen ?? 0f);
            currentAttackDamage = baseAttackDamage + (_equipmentStats?.attackDamage ?? 0f);
            currentMoveSpeed = baseMoveSpeed + (_equipmentStats?.moveSpeed ?? 0f);
            currentAttackSpeed = baseAttackSpeed + (_equipmentStats?.attackSpeed ?? 0f);
            currentAttackRange = baseAttackRange + (_equipmentStats?.attackRange ?? 0f);
            
            // Ensure stats don't go below minimum values
            currentMaxHealth = Mathf.Max(1f, currentMaxHealth);
            currentMoveSpeed = Mathf.Max(0f, currentMoveSpeed);
            currentAttackSpeed = Mathf.Max(0.1f, currentAttackSpeed);
            currentAttackRange = Mathf.Max(0f, currentAttackRange);
        }
        
        /// <summary>
        /// Checks if a global flag is set by any equipped item
        /// </summary>
        public bool HasGlobalFlag(string flagName)
        {
            if (_equipmentStats == null || string.IsNullOrEmpty(flagName))
                return false;
            
            return _equipmentStats.globalFlags.Contains(flagName);
        }
        
        /// <summary>
        /// Sets base stats (useful for leveling up or other stat modifications)
        /// </summary>
        public void SetBaseMaxHealth(float value)
        {
            baseMaxHealth = value;
            UpdateStats();
        }
        
        public void SetBaseAttackDamage(float value)
        {
            baseAttackDamage = value;
            UpdateStats();
        }
        
        public void SetBaseMoveSpeed(float value)
        {
            baseMoveSpeed = value;
            UpdateStats();
        }
    }
}

