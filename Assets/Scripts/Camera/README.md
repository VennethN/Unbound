# Bounded Camera System

A flexible camera system for 2D games that follows the player while respecting map boundaries.

## Components

### BoundedCameraController
The main camera controller component that handles player following and boundary constraints.

**Features:**
- Smooth camera following with configurable speed
- Multiple follow modes (SmoothDamp, Lerp, Instant)
- Configurable camera offset from player
- Boundary constraint system
- Auto-detection of player target
- Runtime camera control methods

### CameraBounds
A serializable struct that defines rectangular boundaries for camera movement.

**Features:**
- Min/Max X and Y position constraints
- Position clamping utilities
- Boundary validation methods
- Size and center calculations

### CameraBoundsHelper
An editor utility component for easily defining camera bounds in the Unity editor.

**Features:**
- Visual bounds representation with gizmos
- Configurable bounds size and position
- Color customization for bounds visualization
- Helper methods for bounds manipulation

### CameraSetupExample
An example script showing how to set up the camera system automatically.

## Quick Setup

### Method 1: Manual Setup (Recommended)

1. **Add BoundedCameraController to your Main Camera:**
   - Select your Main Camera in the scene
   - Click "Add Component" → "Scripts" → "Unbound.Camera.BoundedCameraController"

2. **Configure the camera settings in the Inspector:**
   - **Target**: Leave empty (auto-detects player) or assign your player transform
   - **Follow Speed**: Adjust how quickly the camera follows the player (default: 5)
   - **Offset**: Set camera offset from player position (default: 0,0,0)
   - **Lock Z Position**: Keep enabled for 2D games
   - **Use Bounds**: Enable to constrain camera movement

3. **Set up camera bounds:**
   - Create an empty GameObject named "CameraBounds"
   - Add the "CameraBoundsHelper" component to it
   - Position and size the bounds to match your game map
   - In the BoundedCameraController, set the Camera Bounds field to match your bounds

4. **Configure bounds in CameraBoundsHelper:**
   - **Width/Height**: Size of the bounded area
   - **Show Bounds**: Enable for visual feedback in editor
   - Position the GameObject at the center of your desired bounds

### Method 2: Automatic Setup

1. Add the "CameraSetupExample" component to any GameObject in your scene
2. Configure the settings in the Inspector
3. Use the editor buttons to automatically set up the camera system

## Usage Examples

### Basic Camera Following
```csharp
// The camera automatically follows the player once configured
// No additional code needed for basic functionality
```

### Runtime Camera Control
```csharp
// Get reference to camera controller
BoundedCameraController cameraController = Camera.main.GetComponent<BoundedCameraController>();

// Change camera target
cameraController.SetTarget(newTargetTransform);

// Set new bounds
CameraBounds newBounds = new CameraBounds(-10f, 10f, -5f, 5f);
cameraController.SetBounds(newBounds);

// Center camera on target immediately
cameraController.CenterOnTarget();

// Check if camera is within bounds
if (cameraController.IsWithinBounds())
{
    // Camera is within the defined boundaries
}
```

### Custom Bounds Setup
```csharp
// Create bounds programmatically
CameraBounds bounds = new CameraBounds(
    minX: -15f, maxX: 15f,
    minY: -10f, maxY: 10f
);

// Apply bounds to camera
cameraController.SetBounds(bounds);

// Check if a position is within bounds
Vector3 testPosition = new Vector3(5f, 3f, 0f);
if (bounds.Contains(testPosition))
{
    // Position is within bounds
}
```

## Tips

1. **Camera Positioning**: For 2D top-down games, set the camera Z position to -10 and enable "Lock Z Position"
2. **Bounds Sizing**: Make bounds slightly larger than your visible area to prevent camera jitter at edges
3. **Follow Speed**: Higher values make the camera more responsive but can feel jarring. Lower values create smoother movement
4. **Multiple Cameras**: You can use multiple bounded cameras for different areas or gameplay modes
5. **Performance**: The camera updates in LateUpdate() to ensure smooth following after player movement

## Troubleshooting

**Camera not following player:**
- Ensure the player has a PlayerController2D component
- Check that the camera's target field is empty (for auto-detection) or points to the correct transform

**Camera going outside bounds:**
- Verify that "Use Bounds" is enabled in the camera controller
- Check that the Camera Bounds field is properly assigned
- Ensure the bounds size encompasses the desired area

**Jerky camera movement:**
- Increase the follow speed value
- Adjust the position threshold if camera seems overly sensitive
- Try different follow modes (SmoothDamp often works best)

## Integration with Existing Systems

The camera system is designed to work alongside your existing player controller and doesn't interfere with other gameplay systems. It automatically detects the player using the PlayerController2D component and follows it smoothly within the defined boundaries.
