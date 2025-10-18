using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_EDITOR
/// <summary>
/// Custom editor for SaveData to visualize the customData dictionary
/// </summary>
[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    private bool showPlayerData = true;
    private bool showGameStateData = true;
    private bool showCustomData = true;
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        SaveManager saveManager = (SaveManager)target;
        SaveData currentSave = saveManager.GetCurrentSaveData();
        
        if (currentSave == null)
        {
            EditorGUILayout.HelpBox("No save data loaded. Save data will appear here when the game is running.", MessageType.Info);
            return;
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Save Data Visualization", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Metadata
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Metadata", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Save Version:", currentSave.saveVersion);
        EditorGUILayout.LabelField("Last Save Time:", currentSave.lastSaveTime.ToString("yyyy-MM-dd HH:mm:ss"));
        EditorGUILayout.LabelField("Save File Name:", currentSave.saveFileName);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Player Data
        showPlayerData = EditorGUILayout.BeginFoldoutHeaderGroup(showPlayerData, "Player Data");
        if (showPlayerData && currentSave.playerData != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Name:", currentSave.playerData.playerName);
            EditorGUILayout.LabelField("Level:", currentSave.playerData.level.ToString());
            EditorGUILayout.LabelField("Health:", $"{currentSave.playerData.health} / {currentSave.playerData.maxHealth}");
            EditorGUILayout.LabelField("Experience:", currentSave.playerData.experience.ToString("F1"));
            EditorGUILayout.LabelField("Position:", currentSave.playerData.position.ToVector3().ToString());
            EditorGUILayout.LabelField("Rotation:", currentSave.playerData.rotation.ToQuaternion().eulerAngles.ToString());
            
            // Inventory
            if (currentSave.playerData.inventory != null && currentSave.playerData.inventory.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Inventory:", EditorStyles.boldLabel);
                foreach (var item in currentSave.playerData.inventory)
                {
                    EditorGUILayout.LabelField("  • " + item);
                }
            }
            
            // Stats
            if (currentSave.playerData.stats != null && currentSave.playerData.stats.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Stats:", EditorStyles.boldLabel);
                foreach (var stat in currentSave.playerData.stats)
                {
                    EditorGUILayout.LabelField($"  {stat.Key}:", stat.Value.ToString());
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUILayout.Space(5);
        
        // Game State Data
        showGameStateData = EditorGUILayout.BeginFoldoutHeaderGroup(showGameStateData, "Game State Data");
        if (showGameStateData && currentSave.gameStateData != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField("Current Scene:", currentSave.gameStateData.currentScene);
            EditorGUILayout.LabelField("Playtime:", FormatPlaytime(currentSave.gameStateData.playtime));
            EditorGUILayout.LabelField("Save Count:", currentSave.gameStateData.saveCount.ToString());
            
            // Achievements
            if (currentSave.gameStateData.unlockedAchievements != null && currentSave.gameStateData.unlockedAchievements.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Achievements ({currentSave.gameStateData.unlockedAchievements.Count}):", EditorStyles.boldLabel);
                foreach (var achievement in currentSave.gameStateData.unlockedAchievements)
                {
                    EditorGUILayout.LabelField("  🏆 " + achievement);
                }
            }
            
            // Quest States
            if (currentSave.gameStateData.questStates != null && currentSave.gameStateData.questStates.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Quest States:", EditorStyles.boldLabel);
                foreach (var quest in currentSave.gameStateData.questStates)
                {
                    string status = quest.Value ? "✓ Complete" : "○ Incomplete";
                    EditorGUILayout.LabelField($"  {status}", quest.Key);
                }
            }
            
            // Game Settings
            if (currentSave.gameStateData.gameSettings != null && currentSave.gameStateData.gameSettings.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Game Settings:", EditorStyles.boldLabel);
                foreach (var setting in currentSave.gameStateData.gameSettings)
                {
                    EditorGUILayout.LabelField($"  {setting.Key}:", setting.Value.ToString("F2"));
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUILayout.Space(5);
        
        // Custom Data
        showCustomData = EditorGUILayout.BeginFoldoutHeaderGroup(showCustomData, "Custom Data");
        if (showCustomData && currentSave.customData != null)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            if (currentSave.customData.Count == 0)
            {
                EditorGUILayout.LabelField("No custom data", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Total Entries: {currentSave.customData.Count}", EditorStyles.boldLabel);
                EditorGUILayout.Space(3);
                
                foreach (var kvp in currentSave.customData)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // Key
                    EditorGUILayout.LabelField(kvp.Key + ":", GUILayout.Width(150));
                    
                    // Value with type
                    if (kvp.Value != null)
                    {
                        string valueStr = kvp.Value.ToString();
                        string typeStr = kvp.Value.GetType().Name;
                        
                        // Color code based on type
                        Color originalColor = GUI.contentColor;
                        if (kvp.Value is bool)
                        {
                            GUI.contentColor = (bool)kvp.Value ? Color.green : Color.red;
                        }
                        else if (kvp.Value is int || kvp.Value is float || kvp.Value is double)
                        {
                            GUI.contentColor = Color.cyan;
                        }
                        else if (kvp.Value is string)
                        {
                            GUI.contentColor = Color.yellow;
                        }
                        
                        EditorGUILayout.LabelField($"{valueStr} ({typeStr})", EditorStyles.boldLabel);
                        GUI.contentColor = originalColor;
                    }
                    else
                    {
                        EditorGUILayout.LabelField("null", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // Manual save/load buttons
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Manual Save", GUILayout.Height(30)))
        {
            saveManager.TriggerAutoSave();
        }
        
        if (GUILayout.Button("Reload Save", GUILayout.Height(30)))
        {
            SaveData reloadedData = saveManager.Load();
            if (reloadedData != null)
            {
                saveManager.SetCurrentSaveData(reloadedData);
                Debug.Log("Save data reloaded");
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // File info
        EditorGUILayout.Space(5);
        string savePath = saveManager.GetSavePath();
        EditorGUILayout.HelpBox($"Save Location: {savePath}", MessageType.None);
        
        // Repaint in play mode to show live updates
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
    
    private string FormatPlaytime(float seconds)
    {
        int hours = Mathf.FloorToInt(seconds / 3600);
        int minutes = Mathf.FloorToInt((seconds % 3600) / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        
        if (hours > 0)
            return $"{hours}h {minutes}m {secs}s";
        else if (minutes > 0)
            return $"{minutes}m {secs}s";
        else
            return $"{secs}s";
    }
}
#endif