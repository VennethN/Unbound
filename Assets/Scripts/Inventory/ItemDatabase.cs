using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Unbound.Inventory
{
    /// <summary>
    /// Singleton manager that loads and manages items from JSON files
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        private static ItemDatabase _instance;
        
        [Header("Item Data Path")]
        [Tooltip("Path to the items folder relative to Resources folder (e.g., 'Data/Items')")]
        [SerializeField] private string itemsPath = "Data/Items";
        
        private Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>();
        private Dictionary<string, Sprite> _spriteRegistry = new Dictionary<string, Sprite>(); // Maps spriteID -> Sprite
        private bool _isLoaded = false;
        
        public static ItemDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ItemDatabase>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ItemDatabase");
                        _instance = go.AddComponent<ItemDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
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
            
            LoadItems();
        }
        
        private void Start()
        {
            // Ensure all sprite registries have registered their sprites
            // This handles cases where ItemSpriteRegistry components initialize after ItemDatabase
            RefreshSpriteRegistries();
        }
        
        /// <summary>
        /// Refreshes sprites from all ItemSpriteRegistry components in the scene
        /// </summary>
        public void RefreshSpriteRegistries()
        {
            ItemSpriteRegistry[] registries = FindObjectsByType<ItemSpriteRegistry>(FindObjectsSortMode.None);
            foreach (ItemSpriteRegistry registry in registries)
            {
                registry.RegisterSprites();
            }
        }
        
        /// <summary>
        /// Loads all items from JSON files in the Resources folder
        /// </summary>
        public void LoadItems()
        {
            if (_isLoaded) return;
            
            _items.Clear();
            
            // Load all JSON files from the Resources folder
            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(itemsPath);
            
            foreach (TextAsset jsonFile in jsonFiles)
            {
                try
                {
                    ItemData itemData = JsonUtility.FromJson<ItemData>(jsonFile.text);
                    
                    if (itemData != null && itemData.IsValid())
                    {
                        if (_items.ContainsKey(itemData.itemID))
                        {
                            Debug.LogWarning($"Duplicate item ID found: {itemData.itemID} in file {jsonFile.name}. Skipping.");
                            continue;
                        }
                        
                        _items[itemData.itemID] = itemData;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to validate item from file {jsonFile.name}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading item from {jsonFile.name}: {e.Message}");
                }
            }
            
            _isLoaded = true;
            Debug.Log($"Loaded {_items.Count} items from database");
        }
        
        /// <summary>
        /// Gets an item by its ID
        /// </summary>
        public ItemData GetItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return null;
            
            if (!_isLoaded)
                LoadItems();
            
            _items.TryGetValue(itemID, out ItemData item);
            return item;
        }
        
        /// <summary>
        /// Checks if an item exists in the database
        /// </summary>
        public bool HasItem(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return false;
            
            if (!_isLoaded)
                LoadItems();
            
            return _items.ContainsKey(itemID);
        }
        
        /// <summary>
        /// Gets all items in the database
        /// </summary>
        public Dictionary<string, ItemData> GetAllItems()
        {
            if (!_isLoaded)
                LoadItems();
            
            return new Dictionary<string, ItemData>(_items);
        }
        
        /// <summary>
        /// Reloads items from JSON files (useful for runtime updates)
        /// </summary>
        public void ReloadItems()
        {
            _isLoaded = false;
            LoadItems();
        }
        
        /// <summary>
        /// Registers a sprite with a sprite ID (allows items to reference sprites by ID)
        /// </summary>
        public void RegisterSprite(string spriteID, Sprite sprite)
        {
            if (string.IsNullOrEmpty(spriteID) || sprite == null)
                return;
            
            _spriteRegistry[spriteID] = sprite;
        }
        
        /// <summary>
        /// Gets a sprite by sprite ID from the registry
        /// If not found, checks ItemSpriteRegistry components in the scene as fallback
        /// </summary>
        public Sprite GetSprite(string spriteID)
        {
            if (string.IsNullOrEmpty(spriteID))
                return null;
            
            // Check internal registry first
            if (_spriteRegistry.TryGetValue(spriteID, out Sprite sprite))
                return sprite;
            
            // Fallback: Check ItemSpriteRegistry components in scene
            // This handles initialization order issues where registries haven't registered yet
            ItemSpriteRegistry[] registries = FindObjectsByType<ItemSpriteRegistry>(FindObjectsSortMode.None);
            foreach (ItemSpriteRegistry registry in registries)
            {
                Sprite foundSprite = registry.GetSprite(spriteID);
                if (foundSprite != null)
                {
                    // Register it in our internal registry for future lookups
                    RegisterSprite(spriteID, foundSprite);
                    return foundSprite;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets a sprite for an item (checks spriteID first, then iconPath from Resources)
        /// If spriteID is not registered, tries to load from Resources using spriteID as path
        /// </summary>
        public Sprite GetItemSprite(string itemID)
        {
            if (string.IsNullOrEmpty(itemID))
                return null;
            
            ItemData itemData = GetItem(itemID);
            if (itemData == null)
                return null;
            
            // Check spriteID first (from registry)
            if (!string.IsNullOrEmpty(itemData.spriteID))
            {
                Sprite sprite = GetSprite(itemData.spriteID);
                if (sprite != null)
                    return sprite;
                
                // If spriteID not registered, try loading from Resources using spriteID as path
                // Try common paths: Items/{spriteID}, Sprites/{spriteID}, Icons/{spriteID}
                string[] possiblePaths = {
                    $"Items/{itemData.spriteID}",
                    $"Sprites/{itemData.spriteID}",
                    $"Icons/{itemData.spriteID}",
                    itemData.spriteID
                };
                
                foreach (string path in possiblePaths)
                {
                    sprite = ItemIconLoader.LoadIcon(path);
                    if (sprite != null)
                        return sprite;
                }
            }
            
            // Fall back to iconPath (Resources)
            if (!string.IsNullOrEmpty(itemData.iconPath))
            {
                return ItemIconLoader.LoadIcon(itemData.iconPath);
            }
            
            return null;
        }
        
        /// <summary>
        /// Registers multiple sprites at once (spriteID -> Sprite mappings)
        /// </summary>
        public void RegisterSprites(Dictionary<string, Sprite> sprites)
        {
            foreach (var kvp in sprites)
            {
                RegisterSprite(kvp.Key, kvp.Value);
            }
        }
        
        /// <summary>
        /// Clears the sprite registry
        /// </summary>
        public void ClearSpriteRegistry()
        {
            _spriteRegistry.Clear();
        }
    }
}

