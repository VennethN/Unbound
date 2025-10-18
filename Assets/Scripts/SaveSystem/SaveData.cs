using System;
using System.Collections.Generic;
using UnityEngine;

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
    public List<string> inventory;
    public Dictionary<string, int> stats;
    
    public PlayerData()
    {
        playerName = "Player";
        level = 1;
        health = 100f;
        maxHealth = 100f;
        experience = 0f;
        position = Vector3Serializable.zero;
        rotation = QuaternionSerializable.identity;
        inventory = new List<string>();
        stats = new Dictionary<string, int>();
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
    public Dictionary<string, bool> questStates;
    public Dictionary<string, float> gameSettings;
    
    public GameStateData()
    {
        currentScene = "";
        playtime = 0f;
        saveCount = 0;
        unlockedAchievements = new List<string>();
        questStates = new Dictionary<string, bool>();
        gameSettings = new Dictionary<string, float>();
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
