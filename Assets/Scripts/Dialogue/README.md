# Dialogue System Documentation

The Unbound Dialogue System provides a flexible, data-driven approach to creating branching conversations and interactions in your game.

## Core Components

### 1. DialogueAsset (ScriptableObject)
- **Purpose**: Contains complete dialogue data for a conversation
- **Creation**: Assets > Create > Unbound > Dialogue > Dialogue Asset
- **Fields**:
  - `dialogueID`: Unique identifier for the conversation
  - `displayName`: Human-readable name
  - `startNodeID`: ID of the first dialogue node
  - `nodes`: List of all dialogue nodes in the conversation
  - `localizationTable`: Localization table for text keys

### 2. DialogueNode
- **Purpose**: Represents a single dialogue entry
- **Fields**:
  - `nodeID`: Unique identifier for this node
  - `speakerID`: ID of the character speaking
  - `dialogueTextKey`: Localization key for the dialogue text
  - `portraitSprite`: Character portrait (optional)
  - `animationTrigger`: Animation trigger to play (optional)
  - `choices`: Available player choices
  - `nextNodeID`: ID of next node (for linear progression)
  - `conditions`: Conditions that must be met to show this node
  - `effects`: Effects to execute when this node is shown
  - `autoAdvanceDelay`: Auto-advance delay in seconds
  - `textSpeed`: Text typing speed (characters per second)

### 3. DialogueChoice
- **Purpose**: Represents a player choice in a conversation
- **Fields**:
  - `choiceID`: Unique identifier for this choice
  - `choiceTextKey`: Localization key for choice text
  - `targetNodeID`: Node to advance to when selected
  - `conditions`: Conditions that must be met to show this choice
  - `effects`: Effects to execute when this choice is selected

### 4. DialogueCondition
- **Purpose**: Defines conditions for showing dialogue nodes or choices
- **Types**:
  - `Flag`: Check dialogue flag value
  - `Inventory`: Check if player has items
  - `Quest`: Check quest state
  - `Custom`: Custom condition logic

### 5. DialogueEffect
- **Purpose**: Defines actions that occur when dialogue nodes or choices are executed
- **Types**:
  - `SetFlag`: Set dialogue flag value
  - `AddItem`: Add items to inventory
  - `RemoveItem`: Remove items from inventory
  - `UpdateQuest`: Update quest state
  - `PlayAnimation`: Play character animation
  - `TriggerEvent`: Trigger Unity event
  - `Custom`: Custom effect logic

## Runtime Components

### 1. DialogueController (MonoBehaviour)
- **Purpose**: Manages dialogue flow and state
- **Features**:
  - Start/end dialogues
  - Track dialogue progress and flags
  - Evaluate conditions
  - Execute effects
  - Save/load dialogue state

### 2. DialogueView (MonoBehaviour)
- **Purpose**: Displays dialogue UI and handles user interaction
- **Features**:
  - Text typing animation
  - Choice presentation
  - UI animations
  - Audio feedback

### 3. InteractableDialogueTrigger (MonoBehaviour)
- **Purpose**: Triggers dialogue when player interacts with objects/NPCs
- **Features**:
  - Proximity detection
  - Interaction input handling
  - Visual indicators
  - Trigger conditions

## Usage Examples

### Basic Setup

1. **Create Dialogue Asset**:
   ```csharp
   // In Unity Editor: Assets > Create > Unbound > Dialogue > Dialogue Asset
   // Configure nodes and choices in the inspector
   ```

2. **Setup Scene Objects**:
   ```csharp
   // Add DialogueController to a GameObject (usually a Manager or Canvas)
   var dialogueController = gameObject.AddComponent<DialogueController>();
   dialogueController.dialogueView = dialogueViewReference;

   // Add InteractableDialogueTrigger to NPCs/objects
   var trigger = npc.AddComponent<InteractableDialogueTrigger>();
   trigger.dialogueAsset = myDialogueAsset;
   ```

3. **Trigger Dialogue**:
   ```csharp
   // From InteractableDialogueTrigger (automatic)
   // Or manually:
   dialogueController.StartDialogue(myDialogueAsset);
   ```

### Advanced Features

#### Dialogue Flags
```csharp
// In dialogue effects or custom code:
dialogueController.SetFlag("quest_completed", true);

// Check flags in conditions:
condition.flagName = "quest_completed";
condition.requiredFlagValue = true;
```

#### Custom Conditions/Effects
```csharp
// Implement IDialogueConditionEvaluator:
public bool EvaluateCustomCondition(string conditionType, string[] parameters)
{
    if (conditionType == "HasVisitedLocation")
    {
        return PlayerHasVisitedLocation(parameters[0]);
    }
    return false;
}

// Implement IDialogueEffectExecutor:
public void ExecuteCustomEffect(string effectType, string[] parameters)
{
    if (effectType == "UnlockDoor")
    {
        UnlockDoor(parameters[0]);
    }
}
```

#### Save/Load Integration
```csharp
// Dialogue progress is automatically saved with SaveManager
// Restore progress when starting dialogue:
dialogueController.StartDialogue(dialogueAsset); // Automatically restores state
```

## Best Practices

1. **Modular Design**: Keep dialogue logic separate from game logic
2. **Localization**: Use text keys instead of hardcoded strings
3. **Reusable Assets**: Create dialogue templates for common interactions
4. **Condition Testing**: Test all condition branches thoroughly
5. **Performance**: Avoid complex conditions in Update() loops
6. **Debugging**: Use DialogueController events for debugging

## Integration Points

- **Save System**: Automatic save/load of dialogue progress
- **Input System**: Uses Interact action for triggering dialogue
- **Quest System**: Can check/update quest states
- **Inventory System**: Can check/add/remove items
- **Animation System**: Can trigger character animations
- **Event System**: Can trigger UnityEvents for custom logic

## Troubleshooting

**Dialogue not triggering**:
- Check DialogueController exists in scene
- Verify InteractableDialogueTrigger has valid DialogueAsset
- Ensure player is in interaction range
- Check console for validation errors

**Choices not appearing**:
- Verify choice conditions are met
- Check DialogueNode validation
- Ensure target nodes exist

**Text not displaying**:
- Check DialogueView UI references
- Verify localization keys exist
- Check Canvas setup and layering

## Extension Points

The system is designed for extensibility:

- Add new condition types by extending DialogueCondition.ConditionType
- Add new effect types by extending DialogueEffect.EffectType
- Implement custom evaluators/executors for game-specific logic
- Create custom UI layouts for different dialogue styles
- Add dialogue history/backlog functionality

For more advanced features like dialogue trees or complex branching logic, consider extending the base classes or creating additional manager components.

