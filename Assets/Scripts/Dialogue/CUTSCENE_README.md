# Cutscene System Documentation

This comprehensive cutscene system integrates seamlessly with your existing dialogue system, providing powerful visual storytelling capabilities with easy editing through a dedicated editor window.

## Overview

The cutscene system extends your dialogue framework with:

- **CutsceneAsset**: ScriptableObject for storing complete cutscene sequences
- **CutsceneController**: High-level API for managing cutscene playback and system integration
- **CutscenePlayer**: Component that executes individual cutscene actions and manages playback state
- **CutsceneEditor**: Visual editor window for creating and editing cutscenes
- **Action System**: Modular action types for different cutscene behaviors

## Architecture

### Core Components

```
CutsceneAsset (ScriptableObject)
├── CutsceneStep[]
│   ├── CutsceneAction[]
│   ├── CameraSettings
│   ├── Audio Settings
│   └── Scene Effects
└── Completion Actions

CutsceneController (MonoBehaviour)
├── CutscenePlayer (Component)
├── Dialogue Integration
├── Gameplay Control
└── Audio Management

CutsceneEditor (EditorWindow)
├── Visual Step/Action Editing
├── Validation Tools
└── Asset Management
```

## Quick Start

### 1. Create a Cutscene Asset

1. Right-click in Project window → `Create → Unbound → Dialogue → Cutscene Asset`
2. Name your cutscene (e.g., "OpeningSequence")
3. Open the Cutscene Editor: `Window → Unbound → Cutscene Editor`

### 2. Add Steps

1. In the Cutscene Editor, click the "+" button in the Steps list
2. Name your step (e.g., "CameraPan", "CharacterEntrance")
3. Set duration and delay as needed

### 3. Add Actions to Steps

1. Select a step in the left panel
2. Click "+" in the Actions list
3. Choose an action type from the dropdown menu
4. Configure the action properties in the right panel

### 4. Play the Cutscene

```csharp
// Get reference to CutsceneController
var cutsceneController = FindFirstObjectByType<CutsceneController>();

// Play by asset reference
cutsceneController.PlayCutscene(myCutsceneAsset);

// Or play by ID
cutsceneController.PlayCutsceneByID("OpeningSequence");
```

## Action Types

### Transform Actions

#### MoveTransformAction
Moves a transform from one position/rotation to another over time.

**Properties:**
- `targetTransform`: The GameObject to move
- `startPosition`/`endPosition`: Start and end positions
- `startRotation`/`endRotation`: Start and end rotations
- `useLocalCoordinates`: Use local or world coordinates
- `positionCurve`/`rotationCurve`: Animation curves for easing

**Example Usage:**
```csharp
// Move character from left to center
MoveTransformAction moveAction = new MoveTransformAction();
moveAction.targetTransform = character.transform;
moveAction.startPosition = new Vector3(-5f, 0f, 0f);
moveAction.endPosition = new Vector3(0f, 0f, 0f);
moveAction.duration = 2f;
```

#### SetActiveAction
Enables or disables GameObjects at specific times.

**Properties:**
- `targetGameObject`: Object to activate/deactivate
- `activate`: True to enable, false to disable
- `setAtStart`: Execute at action start or end

**Example Usage:**
```csharp
// Show door at start of step
SetActiveAction doorAction = new SetActiveAction();
doorAction.targetGameObject = doorObject;
doorAction.activate = true;
doorAction.setAtStart = true;
```

### Visual Actions

#### FadeAction
Fades CanvasGroups, SpriteRenderers, or UI Images in/out.

**Properties:**
- `fadeTargetType`: Type of object to fade
- `fadeTarget`: The specific object to fade
- `startAlpha`/`endAlpha`: Alpha values (0-1)
- `fadeCurve`: Animation curve for easing

**Example Usage:**
```csharp
// Fade in UI text
FadeAction textFade = new FadeAction();
textFade.fadeTargetType = FadeAction.FadeTargetType.CanvasGroup;
textFade.fadeTarget = dialogueTextCanvas;
textFade.startAlpha = 0f;
textFade.endAlpha = 1f;
```

#### PlayAnimationAction
Triggers animations on Animator components.

**Properties:**
- `targetAnimator`: Animator component to control
- `animationStateName`: Name of the animation state
- `normalizedTransitionTime`: Blend time for transitions
- `layerIndex`: Animator layer to use

**Example Usage:**
```csharp
// Play character walk animation
PlayAnimationAction walkAnim = new PlayAnimationAction();
walkAnim.targetAnimator = characterAnimator;
walkAnim.animationStateName = "Walk";
walkAnim.normalizedTransitionTime = 0.2f;
```

### Camera Actions

#### CameraMovementAction
Moves the camera to follow targets or specific positions with smooth interpolation.

**Properties:**
- `targetCamera`: Camera to control
- `followTarget`: Optional transform to follow
- `targetPosition`/`targetRotation`: Target camera position/rotation
- `fieldOfView`: Target field of view
- `positionCurve`/`rotationCurve`/`fovCurve`: Animation curves

**Example Usage:**
```csharp
// Pan camera to character closeup
CameraMovementAction cameraPan = new CameraMovementAction();
cameraPan.targetCamera = mainCamera;
cameraPan.followTarget = character.transform;
cameraPan.targetPosition = new Vector3(0f, 1f, -2f);
cameraPan.fieldOfView = 45f;
```

### Audio Actions

#### PlayAudioAction
Plays audio clips with spatial audio support.

**Properties:**
- `audioClip`: Audio file to play
- `audioSource`: Optional existing AudioSource
- `audioPosition`: Position for spatial audio
- `useSpatialAudio`: Enable 3D audio positioning
- `volume`: Playback volume (0-1)
- `loop`: Loop the audio

**Example Usage:**
```csharp
// Play dramatic music
PlayAudioAction musicAction = new PlayAudioAction();
musicAction.audioClip = dramaticMusic;
musicAction.volume = 0.7f;
musicAction.loop = true;
```

### System Integration Actions

#### PlayDialogueAction
Plays a dialogue sequence as part of the cutscene.

**Properties:**
- `dialogueAsset`: Dialogue to play
- `waitForDialogueCompletion`: Pause cutscene until dialogue finishes

**Example Usage:**
```csharp
// Show character dialogue
PlayDialogueAction dialogueAction = new PlayDialogueAction();
dialogueAction.dialogueAsset = characterDialogue;
dialogueAction.waitForDialogueCompletion = true;
```

#### ControlGifAction
Controls GIF animation states during cutscenes.

**Properties:**
- `targetGifPlayer`: GifPlayer component to control
- `command`: Action to perform (Play, Pause, Stop, SwitchToIdle, etc.)
- `targetGifAsset`: GIF asset for SetGif command

**Example Usage:**
```csharp
// Switch character to talking animation
ControlGifAction gifAction = new ControlGifAction();
gifAction.targetGifPlayer = characterGifPlayer;
gifAction.command = ControlGifAction.GifCommand.SwitchToTalking;
```

#### ScreenEffectAction
Applies post-processing effects or overlays.

**Properties:**
- `effectMaterial`: Material with shader effects
- `overlayTexture`: Optional overlay texture
- `effectIntensity`: Effect strength (0-1)
- `intensityCurve`: Animation curve for effect progression

## Advanced Features

### Cutscene Integration with Dialogue

The cutscene system seamlessly integrates with your existing dialogue system:

```csharp
// In your game manager or trigger script
public void StartCutsceneWithDialogue(string cutsceneID, string dialogueID)
{
    var cutsceneController = FindFirstObjectByType<CutsceneController>();

    // Play cutscene
    cutsceneController.PlayCutsceneByID(cutsceneID);

    // Listen for cutscene completion
    cutsceneController.OnCutsceneCompleted.AddListener((cutscene) =>
    {
        // Start dialogue after cutscene
        cutsceneController.StartDialogueByID(dialogueID);
    });
}
```

### Custom Completion Actions

Override the `ExecuteCustomCompletionAction()` method in your CutsceneController for custom behavior:

```csharp
public class MyCutsceneController : CutsceneController
{
    public override void ExecuteCustomCompletionAction()
    {
        // Custom logic after cutscene completes
        EnablePlayerMovement();
        UpdateQuestProgress();
        UnlockAchievement("CutsceneCompleted");
    }
}
```

### Event-Driven Cutscenes

Use the built-in events for reactive cutscene behavior:

```csharp
void Start()
{
    var cutsceneController = GetComponent<CutsceneController>();

    cutsceneController.OnStepStarted.AddListener(OnCutsceneStepStarted);
    cutsceneController.OnCutsceneProgress.AddListener(OnCutsceneProgress);
}

private void OnCutsceneStepStarted(CutsceneStep step)
{
    Debug.Log($"Starting step: {step.displayName}");
}

private void OnCutsceneProgress(float progress)
{
    // Update UI progress bar or other reactive elements
    progressBar.fillAmount = progress;
}
```

## Editor Features

### Visual Cutscene Editor

The Cutscene Editor provides a complete visual interface:

1. **Left Panel**: Cutscene selection, properties, and step management
2. **Right Panel**: Step properties, camera/audio settings, and action editing
3. **Bottom Panel**: Save, validation, and duration display

### Drag & Drop Support

- Drag CutsceneAssets from Project window to automatically load them
- Drag GameObjects, Transforms, and other assets into action properties
- Visual feedback for valid drop targets

### Validation Tools

Built-in validation checks for:
- Missing required references
- Invalid timing values
- Empty steps or actions
- Audio file compatibility

### Keyboard Shortcuts

- `Ctrl+S`: Save current cutscene
- `Ctrl+V`: Validate cutscene
- `Delete`: Remove selected step or action

## Best Practices

### Cutscene Structure

1. **Keep steps focused**: Each step should have a single, clear purpose
2. **Use delays sparingly**: Prefer overlapping actions over delays when possible
3. **Consistent timing**: Use similar durations for related actions
4. **Logical progression**: Order steps to tell a coherent story

### Performance Optimization

1. **Preload assets**: Load audio clips and textures before cutscene starts
2. **Pool objects**: Reuse GameObjects when possible instead of instantiating
3. **Limit concurrent actions**: Avoid too many simultaneous animations
4. **Use object pooling**: For frequently created/destroyed objects

### Audio Management

1. **Consistent volume levels**: Normalize audio clips to prevent jarring changes
2. **Proper spatial audio**: Use appropriate rolloff curves for 3D audio
3. **Fade transitions**: Use fade actions for smooth audio transitions
4. **Memory cleanup**: Dispose of temporary AudioSources after use

### Integration Tips

1. **Scene management**: Use scene loading actions for level transitions
2. **Player control**: Always restore player control after cutscenes
3. **UI state**: Save and restore UI visibility states
4. **Save/load**: Consider cutscene state in save/load systems

## Troubleshooting

### Common Issues

**Cutscene not playing:**
- Check that CutsceneAsset is valid (use Validate button)
- Verify CutsceneController and CutscenePlayer are properly set up
- Ensure required references (Camera, AudioSources) are assigned

**Actions not executing:**
- Check timing values (start/end times should be 0-1 range)
- Verify target objects exist and are active
- Check console for validation errors

**Audio not playing:**
- Verify AudioSource components are properly configured
- Check audio file formats and import settings
- Ensure volume levels are above 0

**Performance issues:**
- Reduce number of simultaneous actions
- Use lower resolution textures for overlays
- Limit camera movement complexity

### Debugging Tools

Use the built-in debug features:

```csharp
// Get detailed cutscene information
var controller = FindFirstObjectByType<CutsceneController>();
Debug.Log($"Current step: {controller.GetCurrentStep()?.displayName}");
Debug.Log($"Progress: {controller.GetCutsceneProgress():P}");
Debug.Log($"Is playing: {controller.IsCutscenePlaying()}");

// Validate all cutscenes
var validationErrors = controller.ValidateAllCutscenes();
foreach (string error in validationErrors)
{
    Debug.LogError($"Cutscene validation: {error}");
}
```

## Examples

### Simple Character Introduction

```csharp
// Create a simple cutscene where camera pans to character, fades in dialogue
CutsceneAsset introCutscene = ScriptableObject.CreateInstance<CutsceneAsset>();
introCutscene.displayName = "Character Introduction";

// Step 1: Camera pan to character
CutsceneStep cameraStep = new CutsceneStep();
cameraStep.displayName = "Camera Pan";
cameraStep.duration = 3f;

CameraMovementAction cameraAction = new CameraMovementAction();
cameraAction.targetCamera = mainCamera;
cameraAction.followTarget = character.transform;
cameraAction.targetPosition = new Vector3(0f, 1f, -3f);
cameraStep.actions.Add(cameraAction);

introCutscene.steps.Add(cameraStep);

// Step 2: Show dialogue
CutsceneStep dialogueStep = new CutsceneStep();
dialogueStep.displayName = "Character Dialogue";
dialogueStep.duration = 4f;

PlayDialogueAction dialogueAction = new PlayDialogueAction();
dialogueAction.dialogueAsset = characterIntroDialogue;
dialogueStep.actions.Add(dialogueAction);

introCutscene.steps.Add(dialogueStep);
```

### Complex Action Sequence

```csharp
// Advanced cutscene with multiple simultaneous actions
CutsceneStep complexStep = new CutsceneStep();
complexStep.displayName = "Multi-Action Sequence";
complexStep.duration = 5f;

// Character movement (0-60% of step)
MoveTransformAction moveAction = new MoveTransformAction();
moveAction.normalizedStartTime = 0f;
moveAction.normalizedEndTime = 0.6f;
moveAction.targetTransform = character.transform;
moveAction.startPosition = new Vector3(-5f, 0f, 0f);
moveAction.endPosition = new Vector3(0f, 0f, 0f);
complexStep.actions.Add(moveAction);

// Camera follow (0-100% of step)
CameraMovementAction cameraAction = new CameraMovementAction();
cameraAction.normalizedStartTime = 0f;
cameraAction.normalizedEndTime = 1f;
cameraAction.targetCamera = mainCamera;
cameraAction.followTarget = character.transform;
cameraAction.targetPosition = new Vector3(2f, 1f, -4f);
complexStep.actions.Add(cameraAction);

// Background music (0-100% of step)
PlayAudioAction musicAction = new PlayAudioAction();
musicAction.normalizedStartTime = 0f;
musicAction.normalizedEndTime = 1f;
musicAction.audioClip = backgroundMusic;
musicAction.loop = true;
complexStep.actions.Add(musicAction);
```

## Integration with Existing Systems

### Dialogue System Integration

The cutscene system is designed to work alongside your existing dialogue system:

```csharp
// In DialogueView or DialogueController
public void OnDialogueNodeComplete(DialogueNode node)
{
    // Check if this node should trigger a cutscene
    if (node.animationTrigger == "play_cutscene")
    {
        var cutsceneController = FindFirstObjectByType<CutsceneController>();
        cutsceneController.PlayCutsceneByID(node.dialogueTextKey); // Use text key as cutscene ID
    }
}
```

### Event System Integration

```csharp
// Listen for cutscene events in other systems
void Start()
{
    var cutsceneController = FindFirstObjectByType<CutsceneController>();

    cutsceneController.OnCutsceneStarted.AddListener((cutscene) =>
    {
        // Pause game music
        AudioManager.Instance.PauseMusic();
    });

    cutsceneController.OnCutsceneCompleted.AddListener((cutscene) =>
    {
        // Resume game music
        AudioManager.Instance.ResumeMusic();
    });
}
```

## Future Enhancements

### Potential Additions

1. **Timeline Editor**: Visual timeline view for precise timing control
2. **Particle Effects**: Built-in particle system integration
3. **Lighting Control**: Dynamic lighting changes during cutscenes
4. **Subtitle Support**: Automatic subtitle generation from voice-over
5. **Branching Logic**: Conditional cutscene paths based on game state
6. **Cutscene Recording**: Record player actions as cutscene templates
7. **Performance Profiling**: Built-in performance monitoring tools

This cutscene system provides a solid foundation for cinematic storytelling in your game while maintaining the flexibility and ease of use that makes your dialogue system successful.
