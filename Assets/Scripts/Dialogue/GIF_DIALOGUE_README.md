# GIF Dialogue System

This system extends the dialogue system to support animated GIF portraits with smooth transitions between idle and talking states.

## Overview

The GIF dialogue system consists of several components working together:

- **GifAsset**: ScriptableObject for storing and managing GIF animation data
- **GifPlayer**: Unity component for playing GIF animations in UI
- **DialogueGifController**: Manages state transitions between idle/talking animations
- **GifAssetEditor**: Editor window for creating and configuring GIF assets

## Setup

### 1. Create GIF Assets

Use the **GifAsset Editor** window (`Window > Unbound > GifAsset Editor`) to create and configure GIF assets:

1. Create a new GifAsset or select an existing one
2. Import sprites from a sprite sheet or add individual sprites
3. Configure animation settings (frame rate, looping, etc.)
4. Set up transition GIFs for idle and talking states
5. Save the asset

### 2. Configure Dialogue Nodes

In your DialogueNode assets:

1. Set the **Portrait Gif** field to your GifAsset
2. The system will automatically handle transitions between idle and talking states
3. Regular sprite portraits still work as before

### 3. Set Up UI Components

Add these components to your dialogue UI:

1. **GifPlayer** component on your portrait image GameObject
2. **DialogueGifController** component (can be on the same GameObject or separate)
3. **DialogueView** should reference both components

## Usage

### Basic Usage

The system automatically handles transitions:

```csharp
// The DialogueView will automatically:
// 1. Set up the GIF controller for each node
// 2. Transition to idle state when node starts
// 3. Transition to talking state when text animation begins
// 4. Transition back to idle when text completes
```

### Manual Control

You can manually control GIF states:

```csharp
// Get reference to the GIF controller
var gifController = dialogueView.GetComponent<DialogueGifController>();

// Manually set states
gifController.SetIdleState();
gifController.SetTalkingState();
gifController.ToggleTalkingState();

// Check current state
bool isTalking = gifController.IsInTalkingState();
```

### Advanced Configuration

#### GifAsset Configuration

```csharp
// Create a GifAsset programmatically
var gifAsset = GifPlayer.CreateGifAssetFromSprites(spriteArray, frameRate: 12f);

// Configure transitions
gifAsset.IdleTransition = idleGifAsset;
gifAsset.TalkingTransition = talkingGifAsset;
gifAsset.TransitionSpeed = 2f; // Faster transitions
```

#### Custom Transitions

```csharp
// Custom transition with specific speed
gifPlayer.SwitchToGif(targetGif, transitionSpeed: 1.5f);

// Immediate switch (no transition)
gifPlayer.SwitchToGif(targetGif);
```

## Animation Workflow

1. **Node Start**: Dialogue system shows the node
2. **Idle State**: GIF shows idle animation (if configured)
3. **Text Animation**: When text starts typing, **immediately** transitions to talking animation
4. **Text Complete**: When text finishes, **immediately** transitions back to idle animation
5. **Node End**: When continuing to next node, resets to idle state

## Best Practices

### GIF Creation

1. **Consistent Frame Sizes**: All frames should be the same size
2. **Frame Rate**: 12-24 FPS works well for most dialogue animations
3. **Looping**: Enable looping for continuous idle animations
4. **Transitions**: Create separate idle/talking animations for better control

### Performance

1. **Frame Optimization**: Use sprite atlases for better performance
2. **Memory Management**: Dispose of temporary sprites in editor tools
3. **Update Frequency**: Only update animations when visible

### Editor Tips

1. Use the GifAsset Editor for bulk sprite import
2. Preview animations before saving
3. Test transitions in play mode
4. Save frequently to avoid data loss

## Troubleshooting

### Common Issues

**GIF not playing**:
- Check that GifPlayer component is attached to an Image component
- Verify the GifAsset has frames and proper frame rate
- Ensure the GameObject is active

**Transitions not working**:
- Verify idle/talking transition assets are assigned
- Check that DialogueGifController is properly referenced
- Ensure auto-transition is enabled

**Performance issues**:
- Reduce frame rate for smoother performance
- Use sprite atlases to reduce texture switches
- Disable unused animations

## Integration Examples

### Custom Dialogue Controller

```csharp
public class CustomDialogueController : MonoBehaviour
{
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private DialogueGifController gifController;

    public void StartCustomDialogue()
    {
        var node = GetCurrentDialogueNode();

        // Setup with custom GIF behavior
        gifController.SetupForNode(node);
        gifController.SetAutoTransition(false); // Manual control

        dialogueView.ShowNode(node, conditionEvaluator);

        // Custom timing for transitions
        StartCoroutine(CustomGifSequence());
    }

    private IEnumerator CustomGifSequence()
    {
        yield return new WaitForSeconds(1f);
        gifController.SetTalkingState();

        yield return new WaitForSeconds(2f);
        gifController.SetIdleState();
    }
}
```

This system provides a flexible and powerful way to add animated portraits to your dialogue system with smooth state transitions and easy configuration through ScriptableObjects.
