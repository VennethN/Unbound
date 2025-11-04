using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using System.Security.Cryptography;
using System.IO.Compression;

/// <summary>
/// Manager class for handling save/load operations with JSON and binary serialization.
/// Supports compression and encryption for additional security.
/// </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager _instance;
    public static SaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SaveManager");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Save Settings")]
    [SerializeField] private string saveDirectory = "SaveData";
    [SerializeField] private string defaultSaveFileName = "savefile";
    [SerializeField] private bool useEncryption = false; // Disabled by default for easy editing
    [SerializeField] private bool useCompression = false; // Disabled by default for easy editing
    
    [Header("Auto Save/Load Settings")]
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool autoSaveOnExit = true;
    [SerializeField] private bool autoSaveOnSceneChange = false;
    [SerializeField] private float autoSaveInterval = 0f; // 0 = disabled, > 0 = auto-save every X seconds
    
    // Encryption key (in production, store this securely!)
    private const string EncryptionKey = "UnboundGame2025SecureKey123456"; // Must be 32 chars for AES-256
    
    private string SavePath => Path.Combine(Application.persistentDataPath, saveDirectory);
    
    [Header("Current Save State (Read-Only)")]
    [SerializeField] private SaveData currentSaveData;
    
    private float autoSaveTimer = 0f;
    
    public enum SerializationFormat
    {
        JSON,
        Binary
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Ensure save directory exists
        if (!Directory.Exists(SavePath))
        {
            Directory.CreateDirectory(SavePath);
        }
        
        // Auto-load on start
        if (autoLoadOnStart)
        {
            currentSaveData = Load();
            if (currentSaveData != null)
            {
                Debug.Log("Auto-loaded save data on start");
            }
            else
            {
                currentSaveData = CreateNewSave();
                Debug.Log("No save file found, created new save data");
            }
        }
        else
        {
            currentSaveData = CreateNewSave();
        }
    }
    
    private void Start()
    {
        // Subscribe to scene change events if auto-save is enabled
        if (autoSaveOnSceneChange)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
    }
    
    private void Update()
    {
        // Handle auto-save interval
        if (autoSaveInterval > 0f && currentSaveData != null)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                Save(currentSaveData);
                Debug.Log($"Auto-saved (interval: {autoSaveInterval}s)");
            }
        }
    }
    
    private void OnApplicationQuit()
    {
        // Auto-save on exit
        if (autoSaveOnExit && currentSaveData != null)
        {
            Save(currentSaveData);
            Debug.Log("Auto-saved on application quit");
        }
        
        // Unsubscribe from events
        if (autoSaveOnSceneChange)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
    
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        if (autoSaveOnSceneChange && currentSaveData != null)
        {
            // Update current scene
            currentSaveData.gameStateData.currentScene = scene.name;
            Save(currentSaveData);
            Debug.Log($"Auto-saved on scene change to: {scene.name}");
        }
    }

    #region Save Operations

    /// <summary>
    /// Saves data using the specified format
    /// </summary>
    public bool Save(SaveData data, string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        try
        {
            fileName = fileName ?? defaultSaveFileName;
            string filePath = GetFilePath(fileName, format);
            
            data.UpdateSaveTime();
            data.saveFileName = fileName;
            data.gameStateData.saveCount++;
            
            // Sync dictionaries to serializable versions before saving
            SyncDictionariesToSerializable(data);
            
            byte[] dataBytes = format == SerializationFormat.JSON 
                ? SerializeToJSON(data) 
                : SerializeToBinary(data);
            
            if (useCompression)
            {
                dataBytes = Compress(dataBytes);
            }
            
            if (useEncryption)
            {
                dataBytes = Encrypt(dataBytes);
            }
            
            File.WriteAllBytes(filePath, dataBytes);
            Debug.Log($"Game saved successfully to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves data asynchronously
    /// </summary>
    public async System.Threading.Tasks.Task<bool> SaveAsync(SaveData data, string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        try
        {
            fileName = fileName ?? defaultSaveFileName;
            string filePath = GetFilePath(fileName, format);
            
            data.UpdateSaveTime();
            data.saveFileName = fileName;
            data.gameStateData.saveCount++;
            
            // Sync dictionaries to serializable versions before saving
            SyncDictionariesToSerializable(data);
            
            byte[] dataBytes = format == SerializationFormat.JSON 
                ? SerializeToJSON(data) 
                : SerializeToBinary(data);
            
            if (useCompression)
            {
                dataBytes = Compress(dataBytes);
            }
            
            if (useEncryption)
            {
                dataBytes = Encrypt(dataBytes);
            }
            
            await System.Threading.Tasks.Task.Run(() => File.WriteAllBytes(filePath, dataBytes));
            Debug.Log($"Game saved successfully to: {filePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
            return false;
        }
    }

    #endregion

    #region Load Operations

    /// <summary>
    /// Loads data using the specified format
    /// </summary>
    public SaveData Load(string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        try
        {
            fileName = fileName ?? defaultSaveFileName;
            string filePath = GetFilePath(fileName, format);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Save file not found: {filePath}");
                return null;
            }
            
            byte[] dataBytes = File.ReadAllBytes(filePath);
            
            if (useEncryption)
            {
                dataBytes = Decrypt(dataBytes);
            }
            
            if (useCompression)
            {
                dataBytes = Decompress(dataBytes);
            }
            
            SaveData data = format == SerializationFormat.JSON 
                ? DeserializeFromJSON(dataBytes) 
                : DeserializeFromBinary(dataBytes);
            
            Debug.Log($"Game loaded successfully from: {filePath}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads data asynchronously
    /// </summary>
    public async System.Threading.Tasks.Task<SaveData> LoadAsync(string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        try
        {
            fileName = fileName ?? defaultSaveFileName;
            string filePath = GetFilePath(fileName, format);
            
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"Save file not found: {filePath}");
                return null;
            }
            
            byte[] dataBytes = await System.Threading.Tasks.Task.Run(() => File.ReadAllBytes(filePath));
            
            if (useEncryption)
            {
                dataBytes = Decrypt(dataBytes);
            }
            
            if (useCompression)
            {
                dataBytes = Decompress(dataBytes);
            }
            
            SaveData data = format == SerializationFormat.JSON 
                ? DeserializeFromJSON(dataBytes) 
                : DeserializeFromBinary(dataBytes);
            
            Debug.Log($"Game loaded successfully from: {filePath}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
            return null;
        }
    }

    #endregion

    #region Serialization Methods

    /// <summary>
    /// Serializes SaveData to JSON format
    /// </summary>
    private byte[] SerializeToJSON(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// Deserializes SaveData from JSON format
    /// </summary>
    private SaveData DeserializeFromJSON(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        SaveData saveData = JsonUtility.FromJson<SaveData>(json);
        
        // Sync serializable dictionaries to runtime dictionaries after loading
        if (saveData != null)
        {
            SyncSerializableToDictionaries(saveData);
        }
        
        return saveData;
    }

    /// <summary>
    /// Serializes SaveData to binary format
    /// </summary>
    private byte[] SerializeToBinary(SaveData data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, data);
            return stream.ToArray();
        }
    }

    /// <summary>
    /// Deserializes SaveData from binary format
    /// </summary>
    private SaveData DeserializeFromBinary(byte[] data)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream(data))
        {
            SaveData saveData = (SaveData)formatter.Deserialize(stream);
            
            // Sync serializable dictionaries to runtime dictionaries after loading
            if (saveData != null)
            {
                SyncSerializableToDictionaries(saveData);
            }
            
            return saveData;
        }
    }
    
    /// <summary>
    /// Syncs runtime dictionaries to serializable versions before saving
    /// </summary>
    private void SyncDictionariesToSerializable(SaveData data)
    {
        if (data == null) return;
        
        // Sync PlayerData stats
        if (data.playerData != null)
        {
            data.playerData.stats = data.playerData.stats; // Trigger setter to sync
        }
        
        // Sync GameStateData dictionaries
        if (data.gameStateData != null)
        {
            data.gameStateData.questStates = data.gameStateData.questStates; // Trigger setter to sync
            data.gameStateData.gameSettings = data.gameStateData.gameSettings; // Trigger setter to sync
            data.gameStateData.globalFlags = data.gameStateData.globalFlags; // Trigger setter to sync
        }
    }
    
    /// <summary>
    /// Syncs serializable dictionaries to runtime dictionaries after loading
    /// </summary>
    private void SyncSerializableToDictionaries(SaveData data)
    {
        if (data == null) return;
        
        // Force initialization of runtime dictionaries from serializable versions
        if (data.playerData != null)
        {
            // Initialize runtime dictionary from serializable
            if (data.playerData.statsSerializable != null)
            {
                data.playerData._stats = data.playerData.statsSerializable.ToDictionary();
            }
            else
            {
                data.playerData._stats = new System.Collections.Generic.Dictionary<string, int>();
            }
        }
        
        if (data.gameStateData != null)
        {
            // Initialize runtime dictionaries from serializable versions
            if (data.gameStateData.questStatesSerializable != null)
            {
                data.gameStateData._questStates = data.gameStateData.questStatesSerializable.ToDictionary();
            }
            else
            {
                data.gameStateData._questStates = new System.Collections.Generic.Dictionary<string, bool>();
            }
            
            if (data.gameStateData.gameSettingsSerializable != null)
            {
                data.gameStateData._gameSettings = data.gameStateData.gameSettingsSerializable.ToDictionary();
            }
            else
            {
                data.gameStateData._gameSettings = new System.Collections.Generic.Dictionary<string, float>();
            }
            
            if (data.gameStateData.globalFlagsSerializable != null)
            {
                data.gameStateData._globalFlags = data.gameStateData.globalFlagsSerializable.ToDictionary();
            }
            else
            {
                data.gameStateData._globalFlags = new System.Collections.Generic.Dictionary<string, bool>();
            }
        }
    }

    #endregion

    #region Compression Methods

    /// <summary>
    /// Compresses data using GZip
    /// </summary>
    private byte[] Compress(byte[] data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    /// <summary>
    /// Decompresses data using GZip
    /// </summary>
    private byte[] Decompress(byte[] data)
    {
        using (MemoryStream input = new MemoryStream(data))
        using (MemoryStream output = new MemoryStream())
        using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
        {
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }

    #endregion

    #region Encryption Methods

    /// <summary>
    /// Encrypts data using AES-256
    /// </summary>
    private byte[] Encrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            aes.GenerateIV();
            
            using (MemoryStream output = new MemoryStream())
            {
                // Write IV to the beginning of the stream
                output.Write(aes.IV, 0, aes.IV.Length);
                
                using (CryptoStream cryptoStream = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cryptoStream.Write(data, 0, data.Length);
                    cryptoStream.FlushFinalBlock();
                }
                
                return output.ToArray();
            }
        }
    }

    /// <summary>
    /// Decrypts data using AES-256
    /// </summary>
    private byte[] Decrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(EncryptionKey);
            
            using (MemoryStream input = new MemoryStream(data))
            {
                // Read IV from the beginning of the stream
                byte[] iv = new byte[aes.IV.Length];
                input.Read(iv, 0, iv.Length);
                aes.IV = iv;
                
                using (CryptoStream cryptoStream = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cryptoStream.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    #endregion

    #region File Management

    /// <summary>
    /// Gets the full file path for a save file
    /// </summary>
    private string GetFilePath(string fileName, SerializationFormat format)
    {
        string extension = format == SerializationFormat.JSON ? ".json" : ".dat";
        return Path.Combine(SavePath, fileName + extension);
    }

    /// <summary>
    /// Checks if a save file exists
    /// </summary>
    public bool SaveExists(string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        fileName = fileName ?? defaultSaveFileName;
        string filePath = GetFilePath(fileName, format);
        return File.Exists(filePath);
    }

    /// <summary>
    /// Deletes a save file
    /// </summary>
    public bool DeleteSave(string fileName = null, SerializationFormat format = SerializationFormat.JSON)
    {
        try
        {
            fileName = fileName ?? defaultSaveFileName;
            string filePath = GetFilePath(fileName, format);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Save file deleted: {filePath}");
                return true;
            }
            else
            {
                Debug.LogWarning($"Save file not found: {filePath}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets all save files in the save directory
    /// </summary>
    public string[] GetAllSaveFiles(SerializationFormat format = SerializationFormat.JSON)
    {
        string extension = format == SerializationFormat.JSON ? "*.json" : "*.dat";
        
        if (!Directory.Exists(SavePath))
        {
            return new string[0];
        }
        
        string[] files = Directory.GetFiles(SavePath, extension);
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        
        return files;
    }

    /// <summary>
    /// Deletes all save files
    /// </summary>
    public void DeleteAllSaves()
    {
        try
        {
            if (Directory.Exists(SavePath))
            {
                Directory.Delete(SavePath, true);
                Directory.CreateDirectory(SavePath);
                Debug.Log("All save files deleted");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete all saves: {e.Message}");
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Creates a new SaveData instance with default values
    /// </summary>
    public SaveData CreateNewSave()
    {
        return new SaveData();
    }

    #region Global Flags Management

    /// <summary>
    /// Sets a global flag value
    /// </summary>
    public void SetGlobalFlag(string flagName, bool value)
    {
        if (currentSaveData == null)
        {
            currentSaveData = CreateNewSave();
        }

        // Access through property to ensure serializable version is synced
        var flags = currentSaveData.gameStateData.globalFlags;
        flags[flagName] = value;
        // Sync back to serializable version
        currentSaveData.gameStateData.globalFlags = flags;
    }

    /// <summary>
    /// Gets a global flag value
    /// </summary>
    public bool GetGlobalFlag(string flagName, bool defaultValue = false)
    {
        if (currentSaveData == null || currentSaveData.gameStateData.globalFlags == null)
        {
            return defaultValue;
        }

        if (currentSaveData.gameStateData.globalFlags.TryGetValue(flagName, out bool value))
        {
            return value;
        }

        return defaultValue;
    }

    /// <summary>
    /// Checks if a global flag matches the required value
    /// </summary>
    public bool EvaluateGlobalFlag(string flagName, bool requiredValue)
    {
        return GetGlobalFlag(flagName) == requiredValue;
    }

    /// <summary>
    /// Gets all global flags (read-only)
    /// </summary>
    public System.Collections.Generic.IReadOnlyDictionary<string, bool> GetAllGlobalFlags()
    {
        if (currentSaveData == null || currentSaveData.gameStateData.globalFlags == null)
        {
            return new System.Collections.Generic.Dictionary<string, bool>();
        }

        return currentSaveData.gameStateData.globalFlags;
    }

    #endregion
    
    /// <summary>
    /// Gets the current save data being used
    /// </summary>
    public SaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }
    
    /// <summary>
    /// Sets the current save data
    /// </summary>
    public void SetCurrentSaveData(SaveData data)
    {
        currentSaveData = data;
    }
    
    /// <summary>
    /// Manually triggers an auto-save
    /// </summary>
    public void TriggerAutoSave()
    {
        if (currentSaveData != null)
        {
            Save(currentSaveData);
            Debug.Log("Manual auto-save triggered");
        }
    }

    /// <summary>
    /// Sets encryption enabled/disabled
    /// </summary>
    public void SetEncryption(bool enabled)
    {
        useEncryption = enabled;
    }

    /// <summary>
    /// Sets compression enabled/disabled
    /// </summary>
    public void SetCompression(bool enabled)
    {
        useCompression = enabled;
    }

    /// <summary>
    /// Gets the save directory path
    /// </summary>
    public string GetSavePath()
    {
        return SavePath;
    }

    #endregion
}