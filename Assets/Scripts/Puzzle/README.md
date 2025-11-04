# Puzzle System

A flexible puzzle system for Unity 2D that enables physics-based pushable object puzzles.

## Overview

The puzzle system consists of three main components:
- **PushableObject**: Makes objects pushable by the player
- **PuzzleTarget**: Acts as a target/receiver for pushable objects
- **PuzzleManager**: Coordinates multiple pushable-target pairs and tracks puzzle completion

## Components

### PushableObject

Makes an object pushable by detecting collisions with the player and applying physics-based movement.

**Setup:**
1. Add a `Rigidbody2D` component (will be added automatically)
2. Add a `Collider2D` component (will be added automatically)
3. Add the `PushableObject` component
4. Configure push settings in the inspector

**Properties:**
- **Push Speed**: How fast the object moves when pushed (default: 2f)
- **Friction**: Friction multiplier when not being pushed (default: 0.95f)
- **Lock Rotation**: Prevents the object from rotating (default: true)
- **Can Push On Any Side**: Whether the object can be pushed from any direction (default: true)
- **Constrain X/Y**: Locks movement to one axis (useful for sliding puzzles)

**Events:**
- `OnPushStart`: Fired when player starts pushing the object
- `OnPushEnd`: Fired when player stops pushing the object
- `OnPushed`: Fired continuously while being pushed (provides push direction)

**Example Usage:**
```csharp
var pushable = GetComponent<PushableObject>();
pushable.SetPushSpeed(3f); // Make it push faster
if (pushable.IsBeingPushed())
{
    Debug.Log("Object is being pushed!");
}
```

### PuzzleTarget

Detects when pushable objects reach this target and triggers events.

**Setup:**
1. Add a `Collider2D` component (will be added automatically)
2. Add the `PuzzleTarget` component
3. Configure detection settings

**Properties:**
- **Detection Radius**: How close a pushable object needs to be to trigger (default: 0.5f)
- **Use Trigger Collider**: Whether to use OnTriggerEnter/Exit (recommended, default: true)
- **Require Exact Match**: Whether to stop the pushable object when it reaches the target (default: false)
- **Required Pushable Object**: Optional - if set, only this specific pushable will trigger this target

**Events:**
- `OnTargetReached`: Fired when a pushable object reaches this target
- `OnTargetLeft`: Fired when a pushable object leaves this target
- `OnPuzzleSolved`: Fired when target is reached (same as OnTargetReached)

**Example Usage:**
```csharp
var target = GetComponent<PuzzleTarget>();
if (target.IsSolved())
{
    Debug.Log("Target has been reached!");
}
target.ResetTarget(); // Reset the target state
```

### PuzzleManager

Manages puzzles with multiple pushable objects and targets. Tracks completion state and triggers events when all required pairs are satisfied.

**Setup:**
1. Create an empty GameObject in your scene
2. Add the `PuzzleManager` component
3. Assign pushable objects and their corresponding targets in the inspector
4. Configure completion settings

**Properties:**
- **Puzzle Pairs**: List of pushable object - target pairs
  - **Pushable Object**: The object that needs to be pushed
  - **Target**: The target it needs to reach
  - **Is Required**: Whether this pair is required for puzzle completion
- **Auto Find Pairs In Scene**: Automatically finds and pairs pushable objects with nearby targets
- **Reset On Completion**: Whether to reset the puzzle after completion
- **Completion Delay**: Delay before triggering completion event (default: 0f)
- **Check Continuous**: Whether to check completion every frame (default: true)

**Events:**
- `OnPuzzleComplete`: Fired when all required pairs are satisfied
- `OnPuzzleReset`: Fired when puzzle is reset
- `OnProgressChanged`: Fired when progress changes (provides solved count and total count)

**Example Usage:**
```csharp
var manager = GetComponent<PuzzleManager>();
float progress = manager.GetProgress(); // Returns 0.0 to 1.0
if (manager.IsCompleted())
{
    Debug.Log("Puzzle completed!");
}
manager.ResetPuzzle(); // Reset all targets
```

## Common Puzzle Patterns

### Single Object Puzzle

1. Create a pushable object (add `PushableObject` component)
2. Create a target (add `PuzzleTarget` component)
3. Connect them via events or use a `PuzzleManager`

**Simple Setup:**
- Pushable object's `OnPushEnd` → Target's `CheckPushableObject` (via Unity Events)
- Target's `OnPuzzleSolved` → Your completion logic

### Multiple Object Puzzle

1. Create multiple pushable objects
2. Create corresponding targets
3. Add a `PuzzleManager` component
4. Assign all pairs in the manager's `Puzzle Pairs` list

**Example:** A puzzle where 3 boxes need to be pushed onto 3 pressure plates.

### Sliding Puzzle

1. Create pushable objects with `Constrain X` or `Constrain Y` enabled
2. Create targets aligned to the constrained axis
3. Use `PuzzleManager` to track completion

**Example:** A horizontal sliding puzzle where objects can only move left/right.

### Ordered Puzzle

1. Set `Required Pushable Object` on each target
2. Use events to unlock the next target when one is completed
3. Track completion order in your own script

**Example:** A puzzle where boxes must be pushed in a specific order.

### PuzzleActionTrigger

A component with callable functions for puzzle actions. Add it to objects and call the functions via Unity Events from PuzzleManager.

**Setup:**
1. Add the `PuzzleActionTrigger` component to any GameObject
2. Configure settings (target color, position, etc.) in the inspector
3. In PuzzleManager's `OnPuzzleComplete` event, add a call to the desired function

**Available Functions:**
- `Show()` / `Hide()` / `ToggleVisibility()` - Control visibility
- `ChangeColor()` - Change renderer color
- `EnableComponents()` / `DisableComponents()` - Toggle components
- `ChangePosition()` / `ChangeRotation()` / `ChangeScale()` / `ChangeTransform()` - Modify transform
- `TriggerAnimation()` - Play animations
- `PlayAudio()` - Play audio clips
- `ResetTransform()` - Reset to initial transform

**Example Usage:**
```
1. Add PuzzleActionTrigger to a door GameObject
2. In PuzzleManager's OnPuzzleComplete event (inspector):
   - Click "+" to add event
   - Drag door GameObject to object field
   - Select: PuzzleActionTrigger → Hide()
3. Door will hide when puzzle completes!
```

**Common Use Cases:**
- Open/hide doors when puzzle completes
- Change object colors to indicate completion
- Play sounds/animations on puzzle solve
- Enable/disable colliders or other components
- Move objects to reveal secrets

## Integration with Other Systems

### With Dialogue System

```csharp
// On PuzzleManager's OnPuzzleComplete event
public void OnPuzzleSolved()
{
    var dialogueTrigger = FindFirstObjectByType<InteractableDialogueTrigger>();
    if (dialogueTrigger != null)
    {
        dialogueTrigger.TriggerDialogue();
    }
}
```

### With Save System

You can make puzzle states saveable by implementing `ISaveable` on a custom component that tracks puzzle state.

### With Scene Transitions

```csharp
// On PuzzleManager's OnPuzzleComplete event
public void OnPuzzleSolved()
{
    var sceneTransition = FindFirstObjectByType<InteractableSceneTransition>();
    if (sceneTransition != null)
    {
        sceneTransition.TryInteract();
    }
}
```

## Tips and Best Practices

1. **Collider Setup**: Ensure pushable objects have appropriate colliders. BoxCollider2D works well for rectangular objects.

2. **Rigidbody Settings**: 
   - Set `Body Type` to `Dynamic` for pushable objects
   - Adjust `Mass` if objects feel too heavy/light
   - Consider `Freeze Rotation Z` to prevent unwanted rotation

3. **Target Detection**: Use trigger colliders for targets when possible - they're more reliable and performant.

4. **Layer Management**: Consider using Physics Layers to prevent unwanted collisions (e.g., separate "Pushable" layer).

5. **Visual Feedback**: Use the events to trigger visual/audio feedback:
   - Highlight targets when objects are nearby
   - Play sounds when objects are pushed
   - Show particle effects on completion

6. **Performance**: For puzzles with many objects, consider:
   - Using `Check Continuous = false` and relying on events
   - Limiting detection radius on targets
   - Using object pooling for frequently reset puzzles

## Troubleshooting

**Object doesn't push:**
- Check that Rigidbody2D has `Body Type` set to `Dynamic`
- Ensure the player has a Rigidbody2D and Collider2D
- Verify collision layers aren't preventing collisions

**Target doesn't detect object:**
- Increase `Detection Radius` on the target
- Ensure target's collider is set as a trigger if `Use Trigger Collider` is enabled
- Check that the pushable object is actually reaching the target position

**Objects push too fast/slow:**
- Adjust `Push Speed` on the PushableObject
- Modify player movement speed or object mass

**Objects slide forever:**
- Increase `Friction` value (closer to 1.0 = less friction)
- Adjust Rigidbody2D's `Linear Damping`

