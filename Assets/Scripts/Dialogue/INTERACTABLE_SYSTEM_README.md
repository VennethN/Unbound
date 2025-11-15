# Interactable System

This document describes the new interactable system that allows creating different types of interactive objects in the game.

## Overview

The interactable system is built around a base class architecture that provides common functionality for all interactable objects, while allowing specific implementations for different interaction types.

## Base Class: BaseInteractable

The `BaseInteractable` class provides the foundation for all interactable objects. It handles:

- **Proximity Detection**: Checks if the player is within interaction range
- **Visual Feedback**: Shows/hides interaction indicators when player is in range
- **Input Handling**: Listens for the "Interact" input action
- **Trigger Conditions**: Supports one-time triggers and quest-based requirements
- **Unity Events**: Provides events for interaction start/end

### Key Features

- **Interaction Radius**: Configurable range for player detection
- **Visual Indicators**: Optional GameObject that shows when player can interact
- **Trigger Once**: Option to make interaction happen only once
- **Quest Integration**: Placeholder for quest state checking
- **Gizmo Support**: Visual debugging in the editor

## Derived Classes

### InteractableDialogueTrigger

Triggers dialogue when the player interacts with the object.

**Usage:**
1. Add the `InteractableDialogueTrigger` component to a GameObject
2. Assign a `DialogueAsset` in the inspector
3. Optionally configure interaction settings (radius, visual indicator, etc.)
4. Ensure a `DialogueController` exists in the scene

**Key Methods:**
- `SetDialogueAsset(DialogueAsset)`: Change the dialogue at runtime
- `TriggerDialogue()`: Manually trigger the dialogue

### InteractableSceneTransition

Transports the player to another scene when they interact with the object.

**Usage:**
1. Add the `InteractableSceneTransition` component to a GameObject
2. Choose transition type (by name, build index, next/previous scene, main menu)
3. Configure the target scene and optional player positioning
4. Ensure a `SceneTransitionManager` exists in the scene

**Transition Types:**
- **By Name**: Specify exact scene name
- **By Build Index**: Use scene build index
- **Next Scene**: Go to next scene in build order
- **Previous Scene**: Go to previous scene in build order
- **Main Menu**: Go to main menu scene

**Player Positioning Options:**
- **Set Player Position**: Set exact coordinates for player spawn
- **Set Player Spawn Point**: Use a GameObject with specific tag as spawn point

**Key Methods:**
- `SetTargetScene(string)`: Set target by scene name
- `SetTargetScene(int)`: Set target by build index
- `SetNextScene()`, `SetPreviousScene()`, `SetMainMenu()`: Quick scene setters
- `SetPlayerSpawnPosition(Vector2)`: Set spawn coordinates
- `SetPlayerSpawnPoint(string)`: Set spawn point by tag

### InteractableTeleporter

Moves the player to a specific spot within the current scene and optionally gates the interaction behind global flags.

**Usage:**
1. Add the `InteractableTeleporter` component to a GameObject
2. Assign a destination via `destinationPoint`, `destinationPointTag`, or enable **Use Manual Destination** with coordinates
3. (Optional) Toggle **Reset Player Velocity** if you want the rigidbody to stop after teleporting
4. (Optional) Enable **Require Global Flag** to gate the teleport and provide a `DialogueAsset` for blocked feedback

**Destination Options:**
- **Transform Reference**: Drag any scene transform into `Destination Point`
- **Tag Lookup**: Provide a tag; the teleporter will find the first object with that tag at runtime
- **Manual Coordinates**: Enable **Use Manual Destination** and specify X/Y coordinates directly

**Flag Gating & Feedback:**
- Supports multiple `FlagRequirement` entries with **All Must Pass** (AND) or **Any Can Pass** (OR) logic
- When requirements fail, the teleporter can automatically start a `blockedDialogueAsset` through the scene's `DialogueController`

**Key Methods:**
- `SetDestination(Transform)` / `SetDestination(Vector2)` / `SetDestinationTag(string)` / `ClearDestination()`
- `SetGlobalFlagRequirement(...)`, `AddFlagRequirement(...)`, `RemoveFlagRequirement(...)`, `SetFlagEvaluationLogic(...)`
- `CanTeleport()`: Check if flag requirements currently pass

## Creating Custom Interactables

To create a new type of interactable:

1. **Inherit from BaseInteractable**: Create a new class that extends `BaseInteractable`
2. **Override PerformInteraction()**: Implement your specific interaction logic
3. **Override CanInteract()** (optional): Add additional interaction requirements
4. **Override CanTrigger()** (optional): Add trigger conditions

```csharp
public class MyCustomInteractable : BaseInteractable
{
    protected override void PerformInteraction()
    {
        // Your custom interaction logic here
        Debug.Log("Custom interaction triggered!");
    }

    protected override bool CanInteract()
    {
        // Add custom conditions
        return base.CanInteract() && SomeCustomCondition();
    }
}
```

## Setup Requirements

1. **Input System**: Ensure "Interact" action is set up in Input System Actions
2. **SceneTransitionManager**: Required for scene transitions (auto-creates if missing)
3. **DialogueController**: Required for dialogue interactions (if using dialogue triggers)
4. **Player Tag**: GameObjects tagged "Player" or with PlayerController2D component

## Migration from Old System

If you have existing `InteractableDialogueTrigger` components, they will continue to work as the class now inherits from `BaseInteractable` and maintains the same public API.







