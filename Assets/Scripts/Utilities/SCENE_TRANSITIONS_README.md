# Scene Transition Manager

A comprehensive utility system for handling scene transitions in Unity with smooth fade effects and button integration.

## Features

- **Smooth Transitions**: Fade in/out effects with customizable duration and color
- **Button Integration**: Easy to use with UnityEvents from buttons
- **Save System Integration**: Automatic saving before scene transitions
- **Multiple Loading Methods**: Load by name, build index, next/previous scene
- **Event System**: UnityEvents for transition callbacks
- **Singleton Pattern**: Persistent across scenes with automatic management
- **Auto-Creation**: Automatically creates transition UI if not provided

## Quick Setup

### 1. Add to Scene

1. Create an empty GameObject in your scene
2. Add the `SceneTransitionManager` component to it
3. (Optional) Customize the transition settings in the Inspector:
   - **Fade Duration**: How long the fade effect takes (default: 1 second)
   - **Fade Color**: Color of the fade effect (default: black)
   - **Auto Save**: Whether to save before transitioning (default: true)

### 2. Using with Buttons

#### Option A: UnityEvents (Recommended)
1. Select a UI Button in your scene
2. In the Inspector, find the "OnClick" section
3. Click the "+" button to add a new event
4. Drag the GameObject with SceneTransitionManager to the object field
5. Select the desired method from the dropdown:
   - `SceneTransitionManager.LoadSceneByName`
   - `SceneTransitionManager.LoadSceneByIndex`
   - `SceneTransitionManager.LoadNextSceneButton`
   - `SceneTransitionManager.LoadPreviousSceneButton`
   - `SceneTransitionManager.ReloadCurrentSceneButton`
   - `SceneTransitionManager.LoadMainMenuButton`

#### Option B: Static Methods
You can call the static methods directly from any script:

```csharp
// Load by scene name
SceneTransitionManager.LoadScene("MainMenu");

// Load by build index
SceneTransitionManager.LoadScene(0);

// Load next/previous scene
SceneTransitionManager.LoadNextScene();
SceneTransitionManager.LoadPreviousScene();

// Reload current scene
SceneTransitionManager.ReloadCurrentScene();

// Load main menu
SceneTransitionManager.LoadMainMenu();
```

## Usage Examples

### Basic Button Setup

```csharp
// In a button's OnClick event, call:
// SceneTransitionManager.LoadSceneByName("TestScene")
```

### Using in Code

```csharp
using Unbound.Utilities;

public class GameController : MonoBehaviour
{
    public void StartGame()
    {
        SceneTransitionManager.LoadScene("GameScene");
    }

    public void GoToMainMenu()
    {
        SceneTransitionManager.LoadMainMenu();
    }

    public void RestartLevel()
    {
        SceneTransitionManager.ReloadCurrentScene();
    }
}
```

### Event Callbacks

```csharp
using UnityEngine;
using Unbound.Utilities;

public class TransitionHandler : MonoBehaviour
{
    private void Start()
    {
        // Find the transition manager
        SceneTransitionManager transitionManager = FindObjectOfType<SceneTransitionManager>();

        if (transitionManager != null)
        {
            // Subscribe to events
            transitionManager.OnTransitionStart.AddListener(OnTransitionStart);
            transitionManager.OnTransitionComplete.AddListener(OnTransitionComplete);
        }
    }

    private void OnTransitionStart(string sceneName)
    {
        Debug.Log($"Starting transition to: {sceneName}");
        // Play sound, show loading screen, etc.
    }

    private void OnTransitionComplete(string sceneName)
    {
        Debug.Log($"Completed transition to: {sceneName}");
        // Initialize new scene, etc.
    }
}
```

## Available Methods

### Static Methods (Call from anywhere)
- `SceneTransitionManager.LoadScene(string sceneName)` - Load scene by name
- `SceneTransitionManager.LoadScene(int sceneIndex)` - Load scene by build index
- `SceneTransitionManager.LoadNextScene()` - Load next scene in build order
- `SceneTransitionManager.LoadPreviousScene()` - Load previous scene in build order
- `SceneTransitionManager.ReloadCurrentScene()` - Reload current scene
- `SceneTransitionManager.LoadMainMenu()` - Load MainMenu scene

### Instance Methods (Call from buttons via UnityEvents)
- `LoadSceneByName(string sceneName)` - Same as static version
- `LoadSceneByIndex(int sceneIndex)` - Same as static version
- `LoadNextSceneButton()` - Same as static version
- `LoadPreviousSceneButton()` - Same as static version
- `ReloadCurrentSceneButton()` - Same as static version
- `LoadMainMenuButton()` - Same as static version

### Utility Methods
- `SceneTransitionManager.GetCurrentSceneName()` - Get current scene name
- `SceneTransitionManager.GetCurrentSceneIndex()` - Get current scene build index
- `SceneTransitionManager.SceneExists(string sceneName)` - Check if scene exists
- `SceneTransitionManager.GetSceneCount()` - Get total number of scenes in build

## Inspector Settings

### Transition Settings
- **Fade Duration**: Duration of fade in/out in seconds (default: 1.0)
- **Fade Color**: Color used for fade effect (default: black)
- **Auto Save On Transition**: Automatically save game before transitioning (default: true)

### Transition UI
- **Fade Image**: (Optional) Assign a UI Image for fade effect
- **Transition Canvas**: (Optional) Assign a Canvas for the fade effect

*Note: If not assigned, the system will automatically create these UI elements.*

### Events
- **On Transition Start**: Called when transition begins (passes scene name)
- **On Transition Complete**: Called when transition completes (passes scene name)
- **On Transition Begin**: Called at start of fade out
- **On Transition End**: Called at end of fade in

## Integration with Save System

The SceneTransitionManager automatically integrates with your existing SaveSystem:

1. When **Auto Save On Transition** is enabled (default), the system will:
   - Find the `SaveManager` in the scene
   - Call `SaveGame()` before starting the transition
   - Update the current scene in the save data

2. This ensures that:
   - Player progress is saved before scene changes
   - The save system knows which scene the player is in
   - No manual save calls are needed in your button scripts

## Tips and Best Practices

### 1. Scene Setup
- Ensure your scenes are added to Build Settings (File > Build Settings)
- Scene names in code must exactly match the scene names in Build Settings

### 2. Button Organization
- Use descriptive method names like `LoadGameScene()` instead of generic `LoadScene()`
- Consider creating wrapper methods for specific transitions

### 3. Performance
- The system uses `SceneManager.LoadSceneAsync()` for smooth loading
- Fade effects are handled with coroutines for smooth animation
- The singleton pattern ensures only one instance exists across scenes

### 4. Event Management
- Remember to unsubscribe from events when objects are destroyed
- Use events for loading screens, sound effects, or other transition-related actions

### 5. Testing
- Test transitions between all scenes in your project
- Verify save system integration works correctly
- Test with different fade durations and colors

## Troubleshooting

### Common Issues

**"SceneTransitionManager instance not found"**
- Ensure SceneTransitionManager GameObject exists in your scene
- Check that the component is attached to an active GameObject

**"Scene not found" errors**
- Verify scene names exactly match those in Build Settings
- Check for typos in scene names
- Ensure scenes are added to Build Settings

**Transitions not working**
- Check that fade duration is greater than 0
- Verify the fade image is properly set up
- Check console for error messages

**Save system not working**
- Ensure SaveManager exists in the scene
- Check that Auto Save On Transition is enabled
- Verify SaveManager has proper save functionality

## Example Project Structure

```
Assets/
├── Scripts/
│   └── Utilities/
│       ├── SceneTransitionManager.cs    # Main transition system
│       └── SceneTransitionExample.cs    # Usage examples
└── Scenes/
    ├── MainMenu.unity
    └── TestScene.unity
```

## Version History

- **v1.0**: Initial release with basic transition functionality
- **v1.1**: Added fade effects and UnityEvent support
- **v1.2**: Integrated with SaveSystem and added utility methods
