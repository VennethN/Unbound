# Save System Documentation

A robust, flexible save system for Unity with JSON and binary serialization support, including optional compression and encryption capabilities.

## Features

- **Multiple Serialization Formats**: JSON and Binary serialization
- **Human-Readable by Default**: JSON format with no encryption/compression for easy manual editing
- **Optional Compression**: GZip compression to reduce file sizes (can be enabled)
- **Optional Encryption**: AES-256 encryption for save file security (can be enabled)
- **Async Support**: Asynchronous save/load operations
- **Flexible Architecture**: ISaveable interface for easy integration
- **Type Safety**: Generic ISaveable<T> for type-safe implementations
- **Utility Functions**: Comprehensive utilities for encryption, validation, and file management

## Components

### 1. SaveData.cs
The main data container class that stores all game data.

```csharp
SaveData saveData = new SaveData();
saveData.playerData.playerName = "Hero";
saveData.playerData.level = 10;
saveData.gameStateData.currentScene = "Level1";
```

**Features:**
- Player data (name, level, health, position, inventory)
- Game state data (scene, playtime, achievements, quests)
- Custom data dictionary for flexibility
- Serializable Vector3 and Quaternion structs

### 2. SaveManager.cs
Singleton manager that handles all save/load operations.

```csharp
// Basic save (JSON, no encryption, easily editable)
SaveData data = SaveManager.Instance.CreateNewSave();
SaveManager.Instance.Save(data, "save1");

// Save with binary format
SaveManager.Instance.Save(data, "save1", SaveManager.SerializationFormat.Binary);

// Async save
await SaveManager.Instance.SaveAsync(data, "save1");

// Load
SaveData loadedData = SaveManager.Instance.Load("save1");

// Load async
SaveData loadedData = await SaveManager.Instance.LoadAsync("save1");
```

**Features:**
- JSON and Binary serialization
- Optional compression and encryption (disabled by default)
- Automatic file management
- Save file listing and deletion
- Backup creation

**Configuration (Optional):**
```csharp
// These are DISABLED by default for easy editing
SaveManager.Instance.SetEncryption(true);  // Enable if you need security
SaveManager.Instance.SetCompression(true); // Enable to reduce file size
```

### 3. ISaveable.cs
Interface for creating saveable objects.

```csharp
public class MyComponent : MonoBehaviour, ISaveable<MyData>
{
    public string SaveID => "my_component";
    
    public MyData CaptureState()
    {
        return new MyData { value = myValue };
    }
    
    public void RestoreState(MyData state)
    {
        myValue = state.value;
    }
}
```

**Attributes:**
- `[Saveable]`: Mark fields/properties that should be saved
- `[NonSaveable]`: Mark fields/properties that should NOT be saved

### 4. SaveableEntity.cs
Pre-built implementations and manager for saveable objects.

**SaveableEntity**: Basic entity with position, rotation, and scale
```csharp
public class MyEntity : SaveableEntity
{
    // Automatically saves transform data
}
```

**SaveablePlayer**: Extended player implementation
```csharp
SaveablePlayer player = GetComponent<SaveablePlayer>();
player.AddItem("Sword");
player.AddExperience(100);
```

**SaveableObjectManager**: Manages all saveable objects
```csharp
// Register objects
SaveableObjectManager.Instance.RegisterSaveable(myObject);

// Capture all states
var states = SaveableObjectManager.Instance.CaptureAllStates();

// Restore all states
SaveableObjectManager.Instance.RestoreAllStates(states);
```

### 5. SaveUtilities.cs
Utility functions for save system operations.

```csharp
// Encryption (if needed)
string encrypted = SaveUtilities.EncryptString("data", "key");
string decrypted = SaveUtilities.DecryptString(encrypted, "key");

// File utilities
string size = SaveUtilities.GetFileSizeFormatted(filePath);
SaveUtilities.CreateBackup(filePath);

// Validation
bool isValid = SaveUtilities.IsValidFileName("save1");
string sanitized = SaveUtilities.SanitizeFileName("my/save\\file");

// Hashing
string hash = SaveUtilities.GenerateHash(data);
bool valid = SaveUtilities.VerifyHash(data, hash);
```

## Usage Examples

### Basic Save/Load (Default - Easily Editable JSON)
```csharp
void SaveGame()
{
    SaveData data = SaveManager.Instance.CreateNewSave();
    
    // Fill in player data
    data.playerData.playerName = "Player1";
    data.playerData.level = 5;
    data.playerData.health = 80f;
    data.playerData.position = transform.position;
    
    // Save - creates human-readable JSON file
    SaveManager.Instance.Save(data, "quicksave");
}

void LoadGame()
{
    SaveData data = SaveManager.Instance.Load("quicksave");
    
    if (data != null)
    {
        // Restore player data
        playerName = data.playerData.playerName;
        playerLevel = data.playerData.level;
        transform.position = data.playerData.position;
    }
}
```

The saved JSON file will look like this (easily editable in any text editor):
```json
{
    "saveVersion": "1.0.0",
    "lastSaveTime": "2025-10-18T14:00:00Z",
    "playerData": {
        "playerName": "Player1",
        "level": 5,
        "health": 80.0,
        "maxHealth": 100.0
    }
}
```

### Using ISaveable Interface
```csharp
public class InventorySystem : MonoBehaviour, ISaveable<InventoryData>
{
    [SerializeField] private List<string> items = new List<string>();
    
    public string SaveID => "inventory";
    
    public InventoryData CaptureState()
    {
        return new InventoryData { items = new List<string>(items) };
    }
    
    public void RestoreState(InventoryData state)
    {
        items = new List<string>(state.items);
    }
    
    object ISaveable.CaptureState() => CaptureState();
    void ISaveable.RestoreState(object state) => RestoreState(state as InventoryData);
}

[Serializable]
public class InventoryData
{
    public List<string> items;
}
```

### Multiple Save Slots
```csharp
// Save to different slots
SaveManager.Instance.Save(data, "slot1");
SaveManager.Instance.Save(data, "slot2");
SaveManager.Instance.Save(data, "slot3");

// List all saves
string[] saves = SaveManager.Instance.GetAllSaveFiles();
foreach (string saveName in saves)
{
    Debug.Log($"Found save: {saveName}");
}

// Load specific slot
SaveData slot1Data = SaveManager.Instance.Load("slot1");
```

### Custom Data Storage
```csharp
SaveData data = new SaveData();

// Add custom data
data.AddCustomData("difficulty", "Hard");
data.AddCustomData("custom_flag", true);
data.AddCustomData("special_value", 42);

// Retrieve custom data
string difficulty = data.GetCustomData<string>("difficulty", "Normal");
bool flag = data.GetCustomData<bool>("custom_flag", false);
int value = data.GetCustomData<int>("special_value", 0);
```

### Optional: Encrypted and Compressed Saves
```csharp
void SaveWithSecurity()
{
    // Enable security features (optional, if you don't want manual editing)
    SaveManager.Instance.SetEncryption(true);
    SaveManager.Instance.SetCompression(true);
    
    SaveData data = SaveManager.Instance.CreateNewSave();
    // ... fill data ...
    
    // This will be encrypted and compressed (not human-readable)
    SaveManager.Instance.Save(data, "secure_save");
}
```

## Best Practices

1. **Always check for null** when loading saves
2. **Use async methods** for large save files to prevent frame drops
3. **Keep encryption/compression disabled** for development and debugging
4. **Enable encryption only** if you need to prevent cheating/tampering
5. **Create backups** before overwriting saves
6. **Use unique SaveIDs** for all ISaveable implementations
7. **Register/Unregister** saveable objects properly in OnEnable/OnDisable

## File Locations

Save files are stored in `Application.persistentDataPath/SaveData/`

- **Windows**: `C:\Users\[username]\AppData\LocalLow\[CompanyName]\[ProductName]\SaveData\`
- **macOS**: `~/Library/Application Support/[CompanyName]/[ProductName]/SaveData/`
- **Linux**: `~/.config/unity3d/[CompanyName]/[ProductName]/SaveData/`

## Editing Save Files Manually

By default, saves are stored as **human-readable JSON** files. You can:

1. Navigate to the save directory
2. Open `.json` files with any text editor
3. Edit values directly
4. Save the file
5. Load it in-game

**Example manual edit:**
```json
{
    "playerData": {
        "playerName": "Cheater",
        "level": 99,
        "health": 9999.0
    }
}
```

## Performance Considerations

- **JSON**: Human-readable, easily editable, slightly larger files, good for debugging
- **Binary**: Smaller files, faster serialization, not human-readable, not easily editable
- **Compression**: Reduces file size by 50-80%, adds small overhead, not easily editable
- **Encryption**: Adds security, minimal performance impact, not easily editable

## Default Configuration

✅ **Encryption**: DISABLED (files are easily editable)  
✅ **Compression**: DISABLED (files are easily editable)  
✅ **Format**: JSON (human-readable)

This means you can open save files in any text editor and modify them directly!

## Troubleshooting

**Save not loading?**
- Check file exists with `SaveManager.Instance.SaveExists()`
- Verify encryption/compression settings match
- Check console for error messages

**Data not serializing?**
- Ensure classes are marked `[Serializable]`
- Unity's JsonUtility doesn't support Dictionary by default (use custom serialization)
- Check that all fields are public or have `[SerializeField]`

**Want to prevent manual editing?**
- Enable encryption: `SaveManager.Instance.SetEncryption(true);`
- Enable compression: `SaveManager.Instance.SetCompression(true);`
- Or use binary format: `SaveManager.SerializationFormat.Binary`

**File corrupted after manual edit?**
- JSON must be valid (check brackets, commas, quotes)
- Use a JSON validator online if unsure
- Create a backup before editing