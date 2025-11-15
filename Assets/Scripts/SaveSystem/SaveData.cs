using System;
using System.Collections.Generic;
using UnityEngine;
using Unbound.Inventory;

/// <summary>
/// Flexible save data container that can store various types of game data.
/// This class is marked as [Serializable] to support both JSON and binary serialization.
/// </summary>
[Serializable]
public class SaveData
{
    // Metadata
    public string saveVersion = "1.0.0";
    public DateTime lastSaveTime;
    public string saveFileName;
    
    // Player Data
    public PlayerData playerData;
    
    // Game State Data
    public GameStateData gameStateData;
    
    // Custom data storage for flexibility
    public Dictionary<string, object> customData;
    
    public SaveData()
    {
        lastSaveTime = DateTime.UtcNow;
        playerData = new PlayerData();
        gameStateData = new GameStateData();
        customData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Updates the last save time to current time
    /// </summary>
    public void UpdateSaveTime()
    {
        lastSaveTime = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds custom data to the save file
    /// </summary>
    public void AddCustomData(string key, object value)
    {
        if (customData.ContainsKey(key))
        {
            customData[key] = value;
        }
        else
        {
            customData.Add(key, value);
        }
    }
    
    /// <summary>
    /// Gets custom data from the save file
    /// </summary>
    public T GetCustomData<T>(string key, T defaultValue = default)
    {
        if (customData.ContainsKey(key))
        {
            try
            {
                return (T)customData[key];
            }
            catch (InvalidCastException)
            {
                Debug.LogWarning($"Could not cast custom data with key '{key}' to type {typeof(T)}");
                return defaultValue;
            }
        }
        return defaultValue;
    }
}

/// <summary>
/// Player-specific data
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerName;
    public int level;
    public float health;
    public float maxHealth;
    public float experience;
    public Vector3Serializable position;
    public QuaternionSerializable rotation;
    
    [Header("Inventory System")]
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    public EquippedItemsData equippedItems = new EquippedItemsData();
    
    public SerializableIntDictionary statsSerializable;
    
    // Runtime-only dictionary (not serialized)
    [System.NonSerialized]
    internal Dictionary<string, int> _stats;
    
    public Dictionary<string, int> stats
    {
        get
        {
            if (_stats == null)
            {
                _stats = statsSerializable != null ? statsSerializable.ToDictionary() : new Dictionary<string, int>();
            }
            return _stats;
        }
        set
        {
            _stats = value;
            statsSerializable = value != null ? new SerializableIntDictionary(value) : new SerializableIntDictionary();
        }
    }
    
    public PlayerData()
    {
        playerName = "Player";
        level = 1;
        health = 100f;
        maxHealth = 100f;
        experience = 0f;
        position = Vector3Serializable.zero;
        rotation = QuaternionSerializable.identity;
        inventorySlots = new List<InventorySlot>();
        equippedItems = new EquippedItemsData();
        statsSerializable = new SerializableIntDictionary();
        _stats = new Dictionary<string, int>();
    }
}

/// <summary>
/// Serializable version of EquippedItems for save system
/// </summary>
[Serializable]
public class EquippedItemsData
{
    public string weaponItemID = string.Empty;
    public string artifactItemID = string.Empty;
    public string shoesItemID = string.Empty;
    public string headwearItemID = string.Empty;
    public string chestplateItemID = string.Empty;
    
    public void FromEquippedItems(EquippedItems equippedItems)
    {
        if (equippedItems == null) return;
        
        weaponItemID = equippedItems.GetEquippedItem(EquipmentType.Weapon) ?? string.Empty;
        artifactItemID = equippedItems.GetEquippedItem(EquipmentType.Artifact) ?? string.Empty;
        shoesItemID = equippedItems.GetEquippedItem(EquipmentType.Shoes) ?? string.Empty;
        headwearItemID = equippedItems.GetEquippedItem(EquipmentType.Headwear) ?? string.Empty;
        chestplateItemID = equippedItems.GetEquippedItem(EquipmentType.Chestplate) ?? string.Empty;
    }
    
    public void ToEquippedItems(EquippedItems equippedItems)
    {
        if (equippedItems == null) return;
        
        equippedItems.Equip(EquipmentType.Weapon, string.IsNullOrEmpty(weaponItemID) ? null : weaponItemID);
        equippedItems.Equip(EquipmentType.Artifact, string.IsNullOrEmpty(artifactItemID) ? null : artifactItemID);
        equippedItems.Equip(EquipmentType.Shoes, string.IsNullOrEmpty(shoesItemID) ? null : shoesItemID);
        equippedItems.Equip(EquipmentType.Headwear, string.IsNullOrEmpty(headwearItemID) ? null : headwearItemID);
        equippedItems.Equip(EquipmentType.Chestplate, string.IsNullOrEmpty(chestplateItemID) ? null : chestplateItemID);
    }
}

/// <summary>
/// Game state data
/// </summary>
[Serializable]
public class GameStateData
{
    public string currentScene;
    public float playtime;
    public int saveCount;
    public List<string> unlockedAchievements;
    public SerializableBoolDictionary questStatesSerializable;
    public SerializableFloatDictionary gameSettingsSerializable;
    public SerializableBoolDictionary globalFlagsSerializable;
    
    // Runtime-only dictionaries (not serialized)
    [System.NonSerialized]
    internal Dictionary<string, bool> _questStates;
    [System.NonSerialized]
    internal Dictionary<string, float> _gameSettings;
    [System.NonSerialized]
    internal Dictionary<string, bool> _globalFlags;
    
    public Dictionary<string, bool> questStates
    {
        get
        {
            if (_questStates == null)
            {
                _questStates = questStatesSerializable != null ? questStatesSerializable.ToDictionary() : new Dictionary<string, bool>();
            }
            return _questStates;
        }
        set
        {
            _questStates = value;
            questStatesSerializable = value != null ? new SerializableBoolDictionary(value) : new SerializableBoolDictionary();
        }
    }
    
    public Dictionary<string, float> gameSettings
    {
        get
        {
            if (_gameSettings == null)
            {
                _gameSettings = gameSettingsSerializable != null ? gameSettingsSerializable.ToDictionary() : new Dictionary<string, float>();
            }
            return _gameSettings;
        }
        set
        {
            _gameSettings = value;
            gameSettingsSerializable = value != null ? new SerializableFloatDictionary(value) : new SerializableFloatDictionary();
        }
    }
    
    public Dictionary<string, bool> globalFlags
    {
        get
        {
            if (_globalFlags == null)
            {
                _globalFlags = globalFlagsSerializable != null ? globalFlagsSerializable.ToDictionary() : new Dictionary<string, bool>();
            }
            return _globalFlags;
        }
        set
        {
            _globalFlags = value;
            globalFlagsSerializable = value != null ? new SerializableBoolDictionary(value) : new SerializableBoolDictionary();
        }
    }
    
    public GameStateData()
    {
        currentScene = "";
        playtime = 0f;
        saveCount = 0;
        unlockedAchievements = new List<string>();
        questStatesSerializable = new SerializableBoolDictionary();
        gameSettingsSerializable = new SerializableFloatDictionary();
        globalFlagsSerializable = new SerializableBoolDictionary();
        _questStates = new Dictionary<string, bool>();
        _gameSettings = new Dictionary<string, float>();
        _globalFlags = new Dictionary<string, bool>();
    }
}

/// <summary>
/// Serializable wrapper for Dictionary<string, bool> to support JSON serialization
/// </summary>
[Serializable]
public class SerializableBoolDictionary
{
    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public bool value;

        public KeyValuePair() { }

        public KeyValuePair(string k, bool v)
        {
            key = k;
            value = v;
        }
    }

    public List<KeyValuePair> items = new List<KeyValuePair>();

    public SerializableBoolDictionary() { }

    public SerializableBoolDictionary(Dictionary<string, bool> dictionary)
    {
        if (dictionary != null)
        {
            foreach (var kvp in dictionary)
            {
                items.Add(new KeyValuePair(kvp.Key, kvp.Value));
            }
        }
    }

    public Dictionary<string, bool> ToDictionary()
    {
        Dictionary<string, bool> dict = new Dictionary<string, bool>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.key))
            {
                dict[item.key] = item.value;
            }
        }
        return dict;
    }
}

/// <summary>
/// Serializable wrapper for Dictionary<string, int> to support JSON serialization
/// </summary>
[Serializable]
public class SerializableIntDictionary
{
    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public int value;

        public KeyValuePair() { }

        public KeyValuePair(string k, int v)
        {
            key = k;
            value = v;
        }
    }

    public List<KeyValuePair> items = new List<KeyValuePair>();

    public SerializableIntDictionary() { }

    public SerializableIntDictionary(Dictionary<string, int> dictionary)
    {
        if (dictionary != null)
        {
            foreach (var kvp in dictionary)
            {
                items.Add(new KeyValuePair(kvp.Key, kvp.Value));
            }
        }
    }

    public Dictionary<string, int> ToDictionary()
    {
        Dictionary<string, int> dict = new Dictionary<string, int>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.key))
            {
                dict[item.key] = item.value;
            }
        }
        return dict;
    }
}

/// <summary>
/// Serializable wrapper for Dictionary<string, float> to support JSON serialization
/// </summary>
[Serializable]
public class SerializableFloatDictionary
{
    [Serializable]
    public class KeyValuePair
    {
        public string key;
        public float value;

        public KeyValuePair() { }

        public KeyValuePair(string k, float v)
        {
            key = k;
            value = v;
        }
    }

    public List<KeyValuePair> items = new List<KeyValuePair>();

    public SerializableFloatDictionary() { }

    public SerializableFloatDictionary(Dictionary<string, float> dictionary)
    {
        if (dictionary != null)
        {
            foreach (var kvp in dictionary)
            {
                items.Add(new KeyValuePair(kvp.Key, kvp.Value));
            }
        }
    }

    public Dictionary<string, float> ToDictionary()
    {
        Dictionary<string, float> dict = new Dictionary<string, float>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.key))
            {
                dict[item.key] = item.value;
            }
        }
        return dict;
    }
}

/// <summary>
/// Serializable Vector3 for JSON/Binary serialization
/// </summary>
[Serializable]
public struct Vector3Serializable
{
    public float x;
    public float y;
    public float z;
    
    public Vector3Serializable(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    
    public Vector3Serializable(Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }
    
    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
    
    public static implicit operator Vector3(Vector3Serializable v)
    {
        return v.ToVector3();
    }
    
    public static implicit operator Vector3Serializable(Vector3 v)
    {
        return new Vector3Serializable(v);
    }
    
    public static Vector3Serializable zero => new Vector3Serializable(0, 0, 0);
}

/// <summary>
/// Serializable Quaternion for JSON/Binary serialization
/// </summary>
[Serializable]
public struct QuaternionSerializable
{
    public float x;
    public float y;
    public float z;
    public float w;
    
    public QuaternionSerializable(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }
    
    public QuaternionSerializable(Quaternion quaternion)
    {
        x = quaternion.x;
        y = quaternion.y;
        z = quaternion.z;
        w = quaternion.w;
    }
    
    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
    
    public static implicit operator Quaternion(QuaternionSerializable q)
    {
        return q.ToQuaternion();
    }
    
    public static implicit operator QuaternionSerializable(Quaternion q)
    {
        return new QuaternionSerializable(q);
    }
    
    public static QuaternionSerializable identity => new QuaternionSerializable(0, 0, 0, 1);
}
