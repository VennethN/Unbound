# Inventory System

A comprehensive inventory system for Unity that supports equipment, consumables, and collectables with stat modifications, JSON-based item definitions, and grid-based UI.

## Overview

The inventory system provides:
- **Item Types**: Equipment, Collectables, and Consumables
- **Equipment Slots**: Weapon, Artifact, Shoes, Headwear, Chestplate
- **Stat Modifications**: Additive stat bonuses from equipped items
- **JSON-Based Items**: Define items in JSON files for easy editing
- **Grid-Based Inventory**: Configurable grid layout for inventory management
- **Save System Integration**: Full integration with the save/load system
- **Dialogue Integration**: Items can be added/removed through dialogue

## Architecture

### Core Components

- **ItemDatabase** - Singleton that loads items from JSON files
- **InventoryManager** - Core inventory system managing items and equipment
- **PlayerStats** - Calculates total stats from base stats + equipment bonuses
- **InventoryUI** - Grid-based inventory UI controller
- **EquipmentPanel** - UI for displaying equipped items
- **ItemDescriptionPanel** - Shows item details on focus/click

## Icon Loading

Icons can be loaded in two ways:

### Method 1: Sprite Registry (Recommended)
Register sprites by ID in Unity, then reference them in JSON:

1. Add `ItemSpriteRegistry` component to a GameObject
2. In the Inspector, add sprite ID to sprite mappings (e.g., "sword_icon" -> your sprite)
3. In your JSON item files, set `spriteID` to reference the sprite:
```json
{
  "itemID": "sword_iron",
  "spriteID": "sword_icon"
}
```

**Benefits:**
- No need for Resources folder
- Multiple items can share the same sprite
- Easy to update sprites in one place

### Method 2: Resources Folder (JSON-based)
Set `iconPath` in your JSON item files to load from Resources:
```json
{
  "iconPath": "Items/sword_iron"
}
```

**Runtime Registration:**
```csharp
// Register sprites at runtime by sprite ID
ItemDatabase.Instance.RegisterSprite("sword_icon", mySprite);
```

The system checks `spriteID` first (from registry), then falls back to `iconPath` (Resources).

## Setup

### 1. Create Item Database GameObject

1. Create an empty GameObject in your scene
2. Name it "ItemDatabase"
3. Add the `ItemDatabase` component
4. Configure the `Items Path` (default: "Data/Items")
5. Mark as `DontDestroyOnLoad` if needed

### 2. Create Inventory Manager GameObject

1. Create an empty GameObject in your scene
2. Name it "InventoryManager"
3. Add the `InventoryManager` component
4. Configure inventory size (default: 6x8 = 48 slots)
5. Mark as `DontDestroyOnLoad` if needed

### 2b. (Optional) Create Item Sprite Registry

If you want to load icons without using Resources:

1. Create an empty GameObject in your scene
2. Name it "ItemSpriteRegistry"
3. Add the `ItemSpriteRegistry` component
4. In the Inspector, add sprite ID to sprite mappings (e.g., "sword_icon" -> your sprite)
5. Assign sprites directly from your project assets
6. Reference these sprite IDs in your JSON item files using the `spriteID` field

### 3. Create Item JSON Files

Create JSON files in `Assets/Resources/Data/Items/` (or your configured path).

#### Equipment Item Example

```json
{
  "itemID": "sword_iron",
  "name": "Iron Sword",
  "description": "A sturdy iron sword that increases attack damage.",
  "itemType": 0,
  "spriteID": "sword_icon",
  "equipmentType": 0,
  "stats": {
    "maxHealth": 0,
    "healthRegen": 0,
    "attackDamage": 10,
    "moveSpeed": 0,
    "attackSpeed": -0.1,
    "attackRange": 0.5,
    "globalFlags": ["can_break_rocks"]
  },
  "maxStackSize": 1
}
```

#### Consumable Item Example

```json
{
  "itemID": "health_potion",
  "name": "Health Potion",
  "description": "Restores 50 health points.",
  "itemType": 2,
  "iconPath": "Items/health_potion",
  "consumableEffect": {
    "effectType": 0,
    "healthAmount": 50,
    "itemsToGive": []
  },
  "maxStackSize": 10
}
```

#### Collectable Item Example

```json
{
  "itemID": "ancient_coin",
  "name": "Ancient Coin",
  "description": "A mysterious coin from ancient times.",
  "itemType": 1,
  "iconPath": "Items/ancient_coin",
  "maxStackSize": 99
}
```

### JSON Field Reference

#### ItemData Fields

- `itemID` (string, required) - Unique identifier for the item
- `name` (string, required) - Display name
- `description` (string, required) - Item description
- `itemType` (int, required) - 0=Equipment, 1=Collectable, 2=Consumable
- `iconPath` (string) - Path to icon sprite in Resources folder (optional if using spriteID)
- `spriteID` (string) - Sprite ID to reference from sprite registry (alternative to iconPath)
- `maxStackSize` (int) - Maximum stack size (0 = unlimited, default: 1)

#### Equipment-Specific Fields

- `equipmentType` (int, required for Equipment) - 0=Weapon, 1=Artifact, 2=Shoes, 3=Headwear, 4=Chestplate
- `stats` (object, required for Equipment) - Stat bonuses:
  - `maxHealth` (float) - Maximum health bonus
  - `healthRegen` (float) - Health regeneration bonus
  - `attackDamage` (float) - Attack damage bonus
  - `moveSpeed` (float) - Movement speed bonus
  - `attackSpeed` (float) - Attack speed bonus (lower = faster)
  - `attackRange` (float) - Attack range bonus
  - `globalFlags` (string[]) - List of global flags to toggle

#### Consumable-Specific Fields

- `consumableEffect` (object, required for Consumable) - Effect when consumed:
  - `effectType` (int) - 0=RestoreHealth, 1=GiveItems, 2=Both
  - `healthAmount` (float) - Health to restore
  - `itemsToGive` (array) - Items to give:
    - `itemID` (string) - Item identifier
    - `quantity` (int) - Quantity to give

## Usage

### Adding Items to Inventory

```csharp
// Add a single item
InventoryManager.Instance.AddItem("sword_iron", 1);

// Add multiple items
InventoryManager.Instance.AddItem("health_potion", 5);
```

### Removing Items from Inventory

```csharp
// Remove a single item
InventoryManager.Instance.RemoveItem("sword_iron", 1);

// Remove multiple items
InventoryManager.Instance.RemoveItem("health_potion", 3);
```

### Checking Item Quantity

```csharp
// Check if player has an item
bool hasItem = InventoryManager.Instance.HasItem("sword_iron", 1);

// Get item quantity
int quantity = InventoryManager.Instance.GetItemQuantity("health_potion");
```

### Equipping Items

```csharp
// Equip an item (must be in inventory)
bool success = InventoryManager.Instance.EquipItem("sword_iron", EquipmentType.Weapon);

// Unequip an item
bool success = InventoryManager.Instance.UnequipItem(EquipmentType.Weapon);
```

### Consuming Items

```csharp
// Consume a consumable item
bool success = InventoryManager.Instance.ConsumeItem("health_potion");
```

### Getting Equipment Stats

```csharp
// Get total stats from all equipped items
ItemStats totalStats = InventoryManager.Instance.GetTotalEquipmentStats();

// Access individual stats
float totalAttackDamage = totalStats.attackDamage;
float totalMoveSpeed = totalStats.moveSpeed;
```

### Player Stats Integration

The `PlayerStats` component automatically calculates total stats from base stats + equipment bonuses:

```csharp
// Get current player stats (includes equipment bonuses)
PlayerStats playerStats = GetComponent<PlayerStats>();
float currentMoveSpeed = playerStats.MoveSpeed;
float currentAttackDamage = playerStats.AttackDamage;
float currentMaxHealth = playerStats.MaxHealth;
```

### Events

The inventory system provides events for UI updates:

```csharp
// Subscribe to inventory changes
InventoryManager.Instance.OnInventoryChanged += OnInventoryChanged;
InventoryManager.Instance.OnItemEquipped += OnItemEquipped;
InventoryManager.Instance.OnItemUnequipped += OnItemUnequipped;

void OnInventoryChanged()
{
    // Refresh inventory UI
}

void OnItemEquipped(EquipmentType slot, string itemID)
{
    // Update equipment UI
}

void OnItemUnequipped(EquipmentType slot)
{
    // Update equipment UI
}
```

## UI Setup

### Inventory UI

1. Create a UI Panel for the inventory
2. Add a Grid Layout Group component
3. Create a slot prefab with:
   - Image component for icon
   - TextMeshProUGUI for quantity
   - Button component
   - InventorySlotUI component
4. Add `InventoryUI` component to the panel
5. Assign:
   - Slot Container (Grid Layout Group transform)
   - Slot Prefab
   - Item Description Panel

### Equipment Panel

1. Create UI elements for each equipment slot
2. Add `EquipmentPanel` component
3. Configure equipment slots in the inspector:
   - Equipment Type
   - Icon Image
   - Item Name Text
   - Unequip Button

### Item Description Panel

1. Create a UI Panel for item description
2. Add `ItemDescriptionPanel` component
3. Assign UI references:
   - Item Name Text
   - Item Description Text
   - Item Stats Text
   - Item Icon Image
   - Equipment Info Panel (optional)
   - Consumable Info Panel (optional)

## Save System Integration

The inventory system automatically integrates with the save system. Inventory slots and equipped items are saved/loaded with player data.

### Manual Save/Load

```csharp
// Capture inventory state
SaveablePlayer player = FindObjectOfType<SaveablePlayer>();
PlayerData playerData = new PlayerData();
player.CaptureInventoryState(playerData);

// Restore inventory state
player.RestoreInventoryState(playerData);
```

## Dialogue Integration

Items can be added/removed through dialogue effects:

```csharp
// In dialogue effects
dialogueController.AddItem("sword_iron", 1);
dialogueController.RemoveItem("health_potion", 1);

// In dialogue conditions
// Check if player has item (handled automatically by DialogueController)
```

## Stat Modifications

Equipment stats stack **additively**. For example:
- Base Attack Damage: 10
- Weapon equipped: +10 attack damage
- Artifact equipped: +5 attack damage
- **Total Attack Damage: 25**

Stats are recalculated automatically when items are equipped/unequipped.

## Global Flags

Equipment can set global flags that can be checked elsewhere:

```csharp
// Check if a global flag is set by equipped items
bool canBreakRocks = playerStats.HasGlobalFlag("can_break_rocks");
```

## Examples

### Example: Creating a Weapon

Create `Assets/Resources/Data/Items/sword_steel.json`:

```json
{
  "itemID": "sword_steel",
  "name": "Steel Sword",
  "description": "A powerful steel sword that increases attack damage and range.",
  "itemType": 0,
  "spriteID": "sword_icon",
  "equipmentType": 0,
  "stats": {
    "attackDamage": 15,
    "attackRange": 1.0,
    "attackSpeed": -0.2,
    "globalFlags": []
  },
  "maxStackSize": 1
}
```

### Example: Creating a Health Potion

Create `Assets/Resources/Data/Items/health_potion.json`:

```json
{
  "itemID": "health_potion",
  "name": "Health Potion",
  "description": "Restores 50 health points.",
  "itemType": 2,
  "iconPath": "Items/health_potion",
  "consumableEffect": {
    "effectType": 0,
    "healthAmount": 50,
    "itemsToGive": []
  },
  "maxStackSize": 10
}
```

### Example: Creating Boots with Speed Bonus

Create `Assets/Resources/Data/Items/boots_speed.json`:

```json
{
  "itemID": "boots_speed",
  "name": "Speed Boots",
  "description": "Boots that increase movement speed.",
  "itemType": 0,
  "spriteID": "boots_icon",
  "equipmentType": 2,
  "stats": {
    "moveSpeed": 2.0,
    "globalFlags": []
  },
  "maxStackSize": 1
}
```

## Troubleshooting

### Items not loading

- Check that JSON files are in the correct Resources folder path
- Verify JSON syntax is valid
- Check ItemDatabase logs for loading errors
- Ensure itemID is unique

### Stats not updating

- Ensure PlayerStats component is on the player GameObject
- Check that equipment is actually equipped (not just in inventory)
- Verify PlayerController2D has PlayerStats reference assigned

### UI not updating

- Ensure InventoryUI is subscribed to InventoryManager events
- Check that slot prefab has InventorySlotUI component
- Verify ItemDescriptionPanel is assigned in InventoryUI

### Save/Load issues

- Ensure InventoryManager exists before saving
- Check that inventory slots are being captured in PlayerData
- Verify EquippedItemsData is being serialized correctly

## API Reference

### InventoryManager

- `AddItem(string itemID, int quantity)` - Add item to inventory
- `RemoveItem(string itemID, int quantity)` - Remove item from inventory
- `HasItem(string itemID, int quantity)` - Check if item exists
- `GetItemQuantity(string itemID)` - Get item quantity
- `EquipItem(string itemID, EquipmentType slot)` - Equip an item
- `UnequipItem(EquipmentType slot)` - Unequip an item
- `ConsumeItem(string itemID)` - Consume a consumable
- `GetTotalEquipmentStats()` - Get combined stats from all equipment
- `GetSlot(int index)` - Get inventory slot at index
- `GetAllSlots()` - Get all inventory slots

### ItemDatabase

- `GetItem(string itemID)` - Get item data by ID
- `HasItem(string itemID)` - Check if item exists in database
- `GetAllItems()` - Get all items in database
- `ReloadItems()` - Reload items from JSON files
- `RegisterSprite(string spriteID, Sprite sprite)` - Register a sprite with a sprite ID
- `GetSprite(string spriteID)` - Get sprite by sprite ID from registry
- `GetItemSprite(string itemID)` - Get sprite for an item (checks spriteID first, then iconPath)
- `RegisterSprites(Dictionary<string, Sprite> sprites)` - Register multiple sprites at once (spriteID -> Sprite)
- `ClearSpriteRegistry()` - Clear the sprite registry

### PlayerStats

- `MaxHealth` - Current max health (base + equipment)
- `HealthRegen` - Current health regen (base + equipment)
- `AttackDamage` - Current attack damage (base + equipment)
- `MoveSpeed` - Current move speed (base + equipment)
- `AttackSpeed` - Current attack speed (base + equipment)
- `AttackRange` - Current attack range (base + equipment)
- `HasGlobalFlag(string flagName)` - Check if global flag is set
- `UpdateStats()` - Manually recalculate stats

