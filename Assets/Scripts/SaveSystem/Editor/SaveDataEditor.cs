using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

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
    private bool showDialogueFlags = true;
    private string dialogueFlagsSearchFilter = "";
    
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
            if (currentSave.playerData.inventorySlots != null && currentSave.playerData.inventorySlots.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Inventory:", EditorStyles.boldLabel);
                foreach (var slot in currentSave.playerData.inventorySlots)
                {
                    if (slot != null && !slot.IsEmpty)
                    {
                        EditorGUILayout.LabelField($"  • {slot.itemID} x{slot.quantity}");
                    }
                }
            }
            
            // Equipped Items
            if (currentSave.playerData.equippedItems != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Equipped Items:", EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(currentSave.playerData.equippedItems.weaponItemID))
                    EditorGUILayout.LabelField("  • Weapon: " + currentSave.playerData.equippedItems.weaponItemID);
                if (!string.IsNullOrEmpty(currentSave.playerData.equippedItems.artifactItemID))
                    EditorGUILayout.LabelField("  • Artifact: " + currentSave.playerData.equippedItems.artifactItemID);
                if (!string.IsNullOrEmpty(currentSave.playerData.equippedItems.shoesItemID))
                    EditorGUILayout.LabelField("  • Shoes: " + currentSave.playerData.equippedItems.shoesItemID);
                if (!string.IsNullOrEmpty(currentSave.playerData.equippedItems.headwearItemID))
                    EditorGUILayout.LabelField("  • Headwear: " + currentSave.playerData.equippedItems.headwearItemID);
                if (!string.IsNullOrEmpty(currentSave.playerData.equippedItems.chestplateItemID))
                    EditorGUILayout.LabelField("  • Chestplate: " + currentSave.playerData.equippedItems.chestplateItemID);
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
        
        // Dialogue Flags
        showDialogueFlags = EditorGUILayout.BeginFoldoutHeaderGroup(showDialogueFlags, "Dialogue Flags");
        if (showDialogueFlags)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            // Get all global flag names referenced in DialogueAssets and InteractableSceneTransition
            HashSet<string> referencedFlagNames = GetAllReferencedFlags();
            
            // Get current global flag states from SaveManager
            Dictionary<string, bool> globalFlagStates = new Dictionary<string, bool>();
            
            if (Application.isPlaying)
            {
                var instanceSaveManager = SaveManager.Instance;
                if (instanceSaveManager != null)
                {
                    var flags = instanceSaveManager.GetAllGlobalFlags();
                    if (flags != null)
                    {
                        foreach (var flag in flags)
                        {
                            globalFlagStates[flag.Key] = flag.Value;
                        }
                    }
                }
            }
            
            if (referencedFlagNames.Count == 0)
            {
                EditorGUILayout.LabelField("No global flag requirements found.", EditorStyles.miniLabel);
                EditorGUILayout.HelpBox("No DialogueAssets or InteractableSceneTransition components with global flag requirements were found.", MessageType.Info);
            }
            else
            {
                // Search filter
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
                dialogueFlagsSearchFilter = EditorGUILayout.TextField(dialogueFlagsSearchFilter);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.Space(5);
                
                EditorGUILayout.LabelField($"Global Flags Referenced: {referencedFlagNames.Count}", EditorStyles.boldLabel);
                if (!Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("Global flag states are only available during Play Mode. Enter Play Mode to see current values.", MessageType.Info);
                }
                EditorGUILayout.Space(3);
                
                // Filter flags if search is active
                var filteredFlags = referencedFlagNames.Where(flagName => 
                    string.IsNullOrEmpty(dialogueFlagsSearchFilter) ||
                    flagName.IndexOf(dialogueFlagsSearchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0
                ).OrderBy(f => f).ToList();
                
                if (filteredFlags.Count == 0 && !string.IsNullOrEmpty(dialogueFlagsSearchFilter))
                {
                    EditorGUILayout.HelpBox("No flags match the search filter.", MessageType.Info);
                }
                else
                {
                    // Display all referenced flags with their current global state
                    EditorGUILayout.LabelField("Global Flag States:", EditorStyles.boldLabel);
                    EditorGUILayout.BeginVertical("box");
                    
                    foreach (var flagName in filteredFlags)
                    {
                        EditorGUILayout.BeginHorizontal();
                        
                        // Flag name
                        EditorGUILayout.LabelField($"  • {flagName}:", GUILayout.Width(200));
                        
                        // Flag value with color coding
                        if (Application.isPlaying && globalFlagStates.ContainsKey(flagName))
                        {
                            bool flagValue = globalFlagStates[flagName];
                            Color originalColor = GUI.contentColor;
                            GUI.contentColor = flagValue ? new Color(0.4f, 0.8f, 0.4f) : new Color(0.8f, 0.4f, 0.4f);
                            EditorGUILayout.LabelField(flagValue ? "✓ True" : "✗ False", EditorStyles.boldLabel);
                            GUI.contentColor = originalColor;
                        }
                        else
                        {
                            EditorGUILayout.LabelField("(Not Set)", EditorStyles.miniLabel);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndVertical();
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
    
    /// <summary>
    /// Gets all global flag names referenced in DialogueAssets and InteractableSceneTransition
    /// </summary>
    private HashSet<string> GetAllReferencedFlags()
    {
        HashSet<string> flagNames = new HashSet<string>();
        
        // Find all DialogueAsset ScriptableObjects in the project
        string[] dialogueGuids = AssetDatabase.FindAssets("t:DialogueAsset");
        
        foreach (string guid in dialogueGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Unbound.Dialogue.DialogueAsset asset = AssetDatabase.LoadAssetAtPath<Unbound.Dialogue.DialogueAsset>(path);
            
            if (asset == null)
                continue;
            
            // Extract flags from startNodeConditions (these are now global flags)
            if (asset.startNodeConditions != null)
            {
                foreach (var startCondition in asset.startNodeConditions)
                {
                    if (startCondition.flagRequirements != null)
                    {
                        foreach (var requirement in startCondition.flagRequirements)
                        {
                            if (!string.IsNullOrEmpty(requirement.flagName))
                            {
                                flagNames.Add(requirement.flagName);
                            }
                        }
                    }
                }
            }
            
            // Extract flags from nodes (conditions) - check for global flag conditions
            if (asset.nodes != null)
            {
                foreach (var node in asset.nodes)
                {
                    if (node.conditions != null)
                    {
                        foreach (var condition in node.conditions)
                        {
                            if (condition.conditionType == Unbound.Dialogue.DialogueCondition.ConditionType.Flag &&
                                !string.IsNullOrEmpty(condition.flagName))
                            {
                                flagNames.Add(condition.flagName);
                            }
                        }
                    }
                    
                    // Extract flags from choices
                    if (node.choices != null)
                    {
                        foreach (var choice in node.choices)
                        {
                            if (choice.conditions != null)
                            {
                                foreach (var condition in choice.conditions)
                                {
                                    if (condition.conditionType == Unbound.Dialogue.DialogueCondition.ConditionType.Flag &&
                                        !string.IsNullOrEmpty(condition.flagName))
                                    {
                                        flagNames.Add(condition.flagName);
                                    }
                                }
                            }
                            
                            // Extract flags from choice effects (SetGlobalFlag)
                            if (choice.effects != null)
                            {
                                foreach (var effect in choice.effects)
                                {
                                    if (effect.effectType == Unbound.Dialogue.DialogueEffect.EffectType.SetGlobalFlag &&
                                        !string.IsNullOrEmpty(effect.flagName))
                                    {
                                        flagNames.Add(effect.flagName);
                                    }
                                }
                            }
                        }
                    }
                    
                    // Extract flags from effects (SetGlobalFlag)
                    if (node.effects != null)
                    {
                        foreach (var effect in node.effects)
                        {
                            if (effect.effectType == Unbound.Dialogue.DialogueEffect.EffectType.SetGlobalFlag &&
                                !string.IsNullOrEmpty(effect.flagName))
                            {
                                flagNames.Add(effect.flagName);
                            }
                        }
                    }
                }
            }
        }
        
        // Find all InteractableSceneTransition components in scenes (for prefabs and scene files)
        // Note: This requires checking prefabs and scene files
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                Unbound.Dialogue.InteractableSceneTransition[] transitions = prefab.GetComponentsInChildren<Unbound.Dialogue.InteractableSceneTransition>(true);
                foreach (var transition in transitions)
                {
                    if (transition.GetFlagRequirements() != null)
                    {
                        foreach (var requirement in transition.GetFlagRequirements())
                        {
                            if (!string.IsNullOrEmpty(requirement.flagName))
                            {
                                flagNames.Add(requirement.flagName);
                            }
                        }
                    }
                }
            }
        }
        
        return flagNames;
    }
}
#endif