using UnityEngine;
using Unbound.Inventory;

/// <summary>
/// MonoBehaviour script that resets the save file by deleting existing saves
/// and creating a new save data instance.
/// </summary>
public class SaveFileReset : MonoBehaviour
{
    [Header("Reset Settings")]
    [SerializeField] private bool resetOnStart = false;
    [SerializeField] private bool saveAfterReset = true;
    [SerializeField] private string saveFileName = null; // null = use default save file name
    
    [Header("Reset Options")]
    [SerializeField] private bool deleteAllSaves = false; // If true, deletes all saves; if false, only deletes the specified/default save
    
    private void Start()
    {
        if (resetOnStart)
        {
            ResetSaveFile();
        }
    }
    
    /// <summary>
    /// Resets the save file by deleting existing saves and creating a new save data instance.
    /// </summary>
    public void ResetSaveFile()
    {
        SaveManager saveManager = SaveManager.Instance;
        
        if (saveManager == null)
        {
            Debug.LogError("SaveManager instance not found. Cannot reset save file.");
            return;
        }
        
        try
        {
            // Delete existing save file(s)
            if (deleteAllSaves)
            {
                saveManager.DeleteAllSaves();
                Debug.Log("All save files deleted.");
            }
            else
            {
                // Delete the specific save file (or default if saveFileName is null)
                SaveManager.SerializationFormat format = SaveManager.SerializationFormat.JSON;
                
                // Try both JSON and Binary formats
                bool deleted = saveManager.DeleteSave(saveFileName, SaveManager.SerializationFormat.JSON);
                if (!deleted)
                {
                    saveManager.DeleteSave(saveFileName, SaveManager.SerializationFormat.Binary);
                }
            }
            
            // Clear InventoryManager's in-memory inventory and equipped items
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ClearInventory();
                InventoryManager.Instance.EquippedItems.ClearAll();
                Debug.Log("Inventory cleared.");
            }
            
            // Create new save data
            SaveData newSaveData = saveManager.CreateNewSave();
            
            // Set as current save data
            saveManager.SetCurrentSaveData(newSaveData);
            
            Debug.Log("Save file reset: New save data created.");
            
            // Optionally save immediately
            if (saveAfterReset)
            {
                saveManager.Save(newSaveData, saveFileName);
                Debug.Log("New save file saved.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to reset save file: {e.Message}");
        }
    }
    
    /// <summary>
    /// Resets the save file (public method for external calls, e.g., from UI buttons).
    /// </summary>
    public void ResetSave()
    {
        ResetSaveFile();
    }
    
    /// <summary>
    /// Deletes a save file without creating a new one (public method for external calls, e.g., from UI buttons).
    /// </summary>
    public void DeleteSaveFile()
    {
        SaveManager saveManager = SaveManager.Instance;
        
        if (saveManager == null)
        {
            Debug.LogError("SaveManager instance not found. Cannot delete save file.");
            return;
        }
        
        try
        {
            // Delete existing save file(s)
            if (deleteAllSaves)
            {
                saveManager.DeleteAllSaves();
                // Clear current save data when deleting all saves
                saveManager.SetCurrentSaveData(null);
                Debug.Log("All save files deleted.");
            }
            else
            {
                // Delete the specific save file (or default if saveFileName is null)
                // Try both JSON and Binary formats
                bool deletedJSON = saveManager.DeleteSave(saveFileName, SaveManager.SerializationFormat.JSON);
                bool deletedBinary = saveManager.DeleteSave(saveFileName, SaveManager.SerializationFormat.Binary);
                
                if (deletedJSON || deletedBinary)
                {
                    Debug.Log($"Save file deleted: {saveFileName ?? "default"}");
                    
                    // If we deleted the current save file, clear the current save data
                    SaveData currentSave = saveManager.GetCurrentSaveData();
                    if (currentSave != null && 
                        (string.IsNullOrEmpty(saveFileName) || currentSave.saveFileName == saveFileName))
                    {
                        saveManager.SetCurrentSaveData(null);
                    }
                }
                else
                {
                    Debug.LogWarning($"Save file not found: {saveFileName ?? "default"}");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }
}

