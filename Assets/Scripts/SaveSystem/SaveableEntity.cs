using System;
using System.Collections.Generic;
using UnityEngine;
using Unbound.Inventory;

/// <summary>
/// Example implementation of ISaveable for game entities.
/// Extend this class to make any GameObject saveable.
/// </summary>
public class SaveableEntity : MonoBehaviour, ISaveable<SaveableEntityData>
{
    [SerializeField] private string saveID;
    
    public string SaveID 
    { 
        get 
        {
            if (string.IsNullOrEmpty(saveID))
            {
                saveID = Guid.NewGuid().ToString();
            }
            return saveID;
        }
    }

    /// <summary>
    /// Captures the current state of this entity
    /// </summary>
    public SaveableEntityData CaptureState()
    {
        SaveableEntityData data = new SaveableEntityData
        {
            saveID = SaveID,
            position = transform.position,
            rotation = transform.rotation,
            scale = transform.localScale,
            isActive = gameObject.activeSelf
        };
        
        return data;
    }

    /// <summary>
    /// Restores the state of this entity
    /// </summary>
    public void RestoreState(SaveableEntityData state)
    {
        if (state == null) return;
        
        transform.position = state.position;
        transform.rotation = state.rotation;
        transform.localScale = state.scale;
        gameObject.SetActive(state.isActive);
    }

    // Non-generic interface implementations
    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state as SaveableEntityData);

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Generate a unique ID if one doesn't exist (Editor only)
        if (string.IsNullOrEmpty(saveID))
        {
            saveID = Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}

/// <summary>
/// Data structure for saveable entity state
/// </summary>
[Serializable]
public class SaveableEntityData
{
    public string saveID;
    public Vector3Serializable position;
    public QuaternionSerializable rotation;
    public Vector3Serializable scale;
    public bool isActive;
}

/// <summary>
/// Example player implementation with extended saveable data
/// </summary>
public class SaveablePlayer : MonoBehaviour, ISaveable<SaveablePlayerData>
{
    [Header("Player Attributes")]
    [SerializeField] private string playerName = "Player";
    [SerializeField] private int level = 1;
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float experience = 0f;
    
    [Header("Save Settings")]
    [SerializeField] private string saveID = "player";
    
    public string SaveID => saveID;

    public SaveablePlayerData CaptureState()
    {
        return new SaveablePlayerData
        {
            playerName = this.playerName,
            level = this.level,
            health = this.health,
            maxHealth = this.maxHealth,
            experience = this.experience,
            position = transform.position,
            rotation = transform.rotation
        };
    }

    public void RestoreState(SaveablePlayerData state)
    {
        if (state == null) return;
        
        this.playerName = state.playerName;
        this.level = state.level;
        this.health = state.health;
        this.maxHealth = state.maxHealth;
        this.experience = state.experience;
        transform.position = state.position;
        transform.rotation = state.rotation;
    }

    // Non-generic interface implementations
    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state as SaveablePlayerData);

    // Public methods for modifying player state
    public void AddItem(string itemID) 
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(itemID, 1);
        }
    }
    
    public void RemoveItem(string itemID) 
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RemoveItem(itemID, 1);
        }
    }
    
    public void TakeDamage(float damage) => health = Mathf.Max(0, health - damage);
    public void Heal(float amount) => health = Mathf.Min(maxHealth, health + amount);
    public void AddExperience(float exp) => experience += exp;
    
    /// <summary>
    /// Captures inventory state from InventoryManager
    /// </summary>
    public void CaptureInventoryState(PlayerData playerData)
    {
        if (InventoryManager.Instance == null) return;
        
        // Capture inventory slots
        playerData.inventorySlots = InventoryManager.Instance.GetAllSlots();
        
        // Capture equipped items
        playerData.equippedItems.FromEquippedItems(InventoryManager.Instance.EquippedItems);
    }
    
    /// <summary>
    /// Restores inventory state to InventoryManager
    /// </summary>
    public void RestoreInventoryState(PlayerData playerData)
    {
        if (InventoryManager.Instance == null) return;
        
        // Restore inventory slots
        if (playerData.inventorySlots != null && playerData.inventorySlots.Count > 0)
        {
            InventoryManager.Instance.ClearInventory();
            foreach (var slot in playerData.inventorySlots)
            {
                if (!slot.IsEmpty)
                {
                    InventoryManager.Instance.AddItem(slot.itemID, slot.quantity);
                }
            }
        }
        
        // Restore equipped items
        if (playerData.equippedItems != null)
        {
            playerData.equippedItems.ToEquippedItems(InventoryManager.Instance.EquippedItems);
        }
    }
}

/// <summary>
/// Data structure for saveable player state
/// </summary>
[Serializable]
public class SaveablePlayerData
{
    public string playerName;
    public int level;
    public float health;
    public float maxHealth;
    public float experience;
    public Vector3Serializable position;
    public QuaternionSerializable rotation;
}

/// <summary>
/// Manager to handle all saveable objects in the scene
/// </summary>
public class SaveableObjectManager : MonoBehaviour
{
    private static SaveableObjectManager _instance;
    public static SaveableObjectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveableObjectManager");
                _instance = go.AddComponent<SaveableObjectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, ISaveable> saveableObjects = new Dictionary<string, ISaveable>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Register a saveable object
    /// </summary>
    public void RegisterSaveable(ISaveable saveable)
    {
        if (saveable == null) return;
        
        string id = saveable.SaveID;
        if (saveableObjects.ContainsKey(id))
        {
            Debug.LogWarning($"Saveable object with ID '{id}' already registered. Overwriting.");
            saveableObjects[id] = saveable;
        }
        else
        {
            saveableObjects.Add(id, saveable);
        }
    }

    /// <summary>
    /// Unregister a saveable object
    /// </summary>
    public void UnregisterSaveable(ISaveable saveable)
    {
        if (saveable == null) return;
        
        string id = saveable.SaveID;
        if (saveableObjects.ContainsKey(id))
        {
            saveableObjects.Remove(id);
        }
    }

    /// <summary>
    /// Capture state of all registered saveable objects
    /// </summary>
    public Dictionary<string, object> CaptureAllStates()
    {
        Dictionary<string, object> states = new Dictionary<string, object>();
        
        foreach (var kvp in saveableObjects)
        {
            try
            {
                states[kvp.Key] = kvp.Value.CaptureState();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to capture state for '{kvp.Key}': {e.Message}");
            }
        }
        
        return states;
    }

    /// <summary>
    /// Restore state of all registered saveable objects
    /// </summary>
    public void RestoreAllStates(Dictionary<string, object> states)
    {
        if (states == null) return;
        
        foreach (var kvp in states)
        {
            if (saveableObjects.ContainsKey(kvp.Key))
            {
                try
                {
                    saveableObjects[kvp.Key].RestoreState(kvp.Value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to restore state for '{kvp.Key}': {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Get all saveable objects of a specific type
    /// </summary>
    public List<T> GetSaveableObjects<T>() where T : ISaveable
    {
        List<T> result = new List<T>();
        
        foreach (var saveable in saveableObjects.Values)
        {
            if (saveable is T typedSaveable)
            {
                result.Add(typedSaveable);
            }
        }
        
        return result;
    }

    /// <summary>
    /// Clear all registered saveable objects
    /// </summary>
    public void ClearAll()
    {
        saveableObjects.Clear();
    }
}