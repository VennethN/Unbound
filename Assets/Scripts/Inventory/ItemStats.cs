using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Serializable stats structure for equipment items
    /// </summary>
    [Serializable]
    public class ItemStats
    {
        [Tooltip("Maximum health bonus")]
        public float maxHealth = 0f;
        
        [Tooltip("Health regeneration bonus")]
        public float healthRegen = 0f;
        
        [Tooltip("Attack damage bonus")]
        public float attackDamage = 0f;
        
        [Tooltip("Move speed bonus")]
        public float moveSpeed = 0f;
        
        [Tooltip("Attack speed bonus (lower = faster)")]
        public float attackSpeed = 0f;
        
        [Tooltip("Attack range bonus")]
        public float attackRange = 0f;
        
        [Tooltip("Global flags to toggle on/off when equipped")]
        public List<string> globalFlags = new List<string>();
        
        /// <summary>
        /// Adds stats from another ItemStats (for additive stacking)
        /// </summary>
        public void AddStats(ItemStats other)
        {
            if (other == null) return;
            
            maxHealth += other.maxHealth;
            healthRegen += other.healthRegen;
            attackDamage += other.attackDamage;
            moveSpeed += other.moveSpeed;
            attackSpeed += other.attackSpeed;
            attackRange += other.attackRange;
            
            foreach (var flag in other.globalFlags)
            {
                if (!globalFlags.Contains(flag))
                {
                    globalFlags.Add(flag);
                }
            }
        }
        
        /// <summary>
        /// Subtracts stats from another ItemStats
        /// </summary>
        public void SubtractStats(ItemStats other)
        {
            if (other == null) return;
            
            maxHealth -= other.maxHealth;
            healthRegen -= other.healthRegen;
            attackDamage -= other.attackDamage;
            moveSpeed -= other.moveSpeed;
            attackSpeed -= other.attackSpeed;
            attackRange -= other.attackRange;
            
            foreach (var flag in other.globalFlags)
            {
                globalFlags.Remove(flag);
            }
        }
        
        /// <summary>
        /// Creates a copy of this ItemStats
        /// </summary>
        public ItemStats Clone()
        {
            return new ItemStats
            {
                maxHealth = this.maxHealth,
                healthRegen = this.healthRegen,
                attackDamage = this.attackDamage,
                moveSpeed = this.moveSpeed,
                attackSpeed = this.attackSpeed,
                attackRange = this.attackRange,
                globalFlags = new List<string>(this.globalFlags)
            };
        }
    }
}

