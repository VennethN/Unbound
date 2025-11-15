using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unbound.Inventory;

#if UNITY_EDITOR
/// <summary>
/// Custom editor for InventoryManager to visualize inventory contents
/// </summary>
[CustomEditor(typeof(InventoryManager))]
public class InventoryManagerEditor : Editor
{
    private bool showInventory = true;
    private bool showEquippedItems = true;
    private Vector2 inventoryScrollPos;
    
    public override void OnInspectorGUI()
    {
        // Draw default inspector
        DrawDefaultInspector();
        
        InventoryManager manager = (InventoryManager)target;
        
        // Only show runtime data when playing
        if (!Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("Inventory contents will appear here when the game is running.", MessageType.Info);
            return;
        }
        
        InventoryManager instance = InventoryManager.Instance;
        if (instance == null)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("InventoryManager instance not found.", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Runtime Inventory Data", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Inventory Slots
        showInventory = EditorGUILayout.BeginFoldoutHeaderGroup(showInventory, $"Inventory Slots ({instance.InventorySize} total)");
        if (showInventory)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            List<InventorySlot> slots = instance.GetAllSlots();
            if (slots == null || slots.Count == 0)
            {
                EditorGUILayout.LabelField("No slots available");
            }
            else
            {
                inventoryScrollPos = EditorGUILayout.BeginScrollView(inventoryScrollPos, GUILayout.Height(200));
                
                int emptyCount = 0;
                int itemCount = 0;
                
                for (int i = 0; i < slots.Count; i++)
                {
                    InventorySlot slot = slots[i];
                    if (slot == null || slot.IsEmpty)
                    {
                        emptyCount++;
                        continue;
                    }
                    
                    itemCount++;
                    EditorGUILayout.BeginHorizontal();
                    
                    // Slot index
                    EditorGUILayout.LabelField($"Slot {i}:", GUILayout.Width(60));
                    
                    // Item ID
                    EditorGUILayout.LabelField(slot.itemID, GUILayout.Width(150));
                    
                    // Quantity
                    EditorGUILayout.LabelField($"x{slot.quantity}", GUILayout.Width(50));
                    
                    // Item name (if available)
                    ItemData itemData = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItem(slot.itemID) : null;
                    if (itemData != null)
                    {
                        EditorGUILayout.LabelField($"({itemData.name})", EditorStyles.miniLabel);
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"Summary: {itemCount} slots with items, {emptyCount} empty slots", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        EditorGUILayout.Space(5);
        
        // Equipped Items
        showEquippedItems = EditorGUILayout.BeginFoldoutHeaderGroup(showEquippedItems, "Equipped Items");
        if (showEquippedItems)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");
            
            EquippedItems equipped = instance.EquippedItems;
            if (equipped == null)
            {
                EditorGUILayout.LabelField("No equipped items data");
            }
            else
            {
                bool hasEquipped = false;
                foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
                {
                    string itemID = equipped.GetEquippedItem(type);
                    if (!string.IsNullOrEmpty(itemID))
                    {
                        hasEquipped = true;
                        ItemData itemData = ItemDatabase.Instance != null ? ItemDatabase.Instance.GetItem(itemID) : null;
                        string itemName = itemData != null ? itemData.name : itemID;
                        EditorGUILayout.LabelField($"{type}:", $"{itemName} ({itemID})");
                    }
                }
                
                if (!hasEquipped)
                {
                    EditorGUILayout.LabelField("No items equipped");
                }
            }
            
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        
        // Debug/Test buttons
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Refresh Display"))
        {
            Repaint();
        }
        
        if (GUILayout.Button("Add Test Item (wooden_sword)"))
        {
            if (instance != null)
            {
                bool success = instance.AddItem("wooden_sword", 1);
                if (success)
                {
                    Debug.Log("Successfully added wooden_sword to inventory");
                }
                else
                {
                    Debug.LogWarning("Failed to add wooden_sword - check if item exists in ItemDatabase");
                }
                Repaint();
            }
        }
        
        if (GUILayout.Button("Clear Inventory"))
        {
            if (instance != null)
            {
                instance.ClearInventory();
                Debug.Log("Inventory cleared");
                Repaint();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox("Note: Inventory starts empty on Awake(). Items must be added manually or loaded from save data.", MessageType.Info);
    }
}
#endif

