# UI System Quick Setup Guide

## Problem You Had
When using `UIManager` with buttons, the panel animations weren't triggering. This was because `UIManager` was using `SetActive()` directly, which bypasses the `UIPanel`'s animation system.

**This has now been fixed!** Both `UIManager` and `UIButtonGroup` now automatically detect and use `UIPanel` components if they exist.

---

## How to Use UIPanel with UIManager

### Step 1: Create Your Panel GameObject

1. In Unity, create a UI Panel (GameObject → UI → Panel)
2. Name it something like "InventoryPanel"
3. Design your panel UI (add buttons, text, images, etc.)

### Step 2: Add UIPanel Component (Optional - for animations)

1. Select your panel GameObject
2. Add Component → `UIPanel` (in Unbound.UI namespace)
3. Configure the animations:
   - **Use Animation**: Check this box
   - **Animation Duration**: 0.3 seconds (or whatever you prefer)
   - **Show Animation**: Choose from:
     - `FadeIn` - Alpha fade in
     - `SlideInLeft` - Slide from left
     - `SlideInRight` - Slide from right
     - `SlideInTop` - Slide from top
     - `SlideInBottom` - Slide from bottom
     - `Scale` - Scale with bounce effect
   - **Hide Animation**: Choose the reverse (FadeOut, Scale, etc.)

4. **Deactivate the panel** in the hierarchy (uncheck the checkbox next to the name)
   - This is important! Panels should start inactive
   - UIPanel will handle activation/deactivation through UIManager

### Step 3: Create Your Button

1. Create a UI Button (GameObject → UI → Button)
2. Style it however you like
3. Position it where you want your button row/column

### Step 4: Set Up UIManager

1. Create an empty GameObject called "UIManager"
2. Add Component → `UIManager`
3. In the inspector, configure:
   - **Button Panel Pairs** → Click `+` to add an entry
     - **Button**: Drag your button here
     - **Panel**: Drag your panel GameObject here
     - **Toggle Mode**: ✓ (allows button to close panel too)
     - **Deactivate Others**: ✓ (closes other panels when opening this one)
     - **Use Color Feedback**: ✓ (optional - tints button based on state)
   - **Start All Inactive**: ✓ (all panels closed at start)

### Step 5: Test It!

Press Play and click your button. The panel should now animate in/out!

---

## Complete Example Setup

```
Hierarchy:
Canvas
├── UIManager (Empty GameObject)
│   └── UIManager component
│
├── ButtonRow (Empty GameObject with HorizontalLayoutGroup)
│   ├── InventoryButton
│   ├── SkillsButton
│   └── QuestButton
│
└── Panels (Empty GameObject)
    ├── InventoryPanel
    │   └── UIPanel component (FadeIn/FadeOut)
    ├── SkillsPanel
    │   └── UIPanel component (SlideInLeft/SlideInLeft)
    └── QuestPanel
        └── UIPanel component (SlideInRight/SlideInRight)
```

In UIManager inspector:
```
Button Panel Pairs:
  [0]
    Button: InventoryButton
    Panel: InventoryPanel
    Toggle Mode: ✓
    Deactivate Others: ✓
    
  [1]
    Button: SkillsButton
    Panel: SkillsPanel
    Toggle Mode: ✓
    Deactivate Others: ✓
    
  [2]
    Button: QuestButton
    Panel: QuestPanel
    Toggle Mode: ✓
    Deactivate Others: ✓
```

---

## UIPanel vs No UIPanel

### Without UIPanel Component
- Panel just appears/disappears instantly
- No animations
- Still works fine!

### With UIPanel Component
- Smooth animations when showing/hiding
- Unity Events (onPanelShown, onPanelHidden)
- More professional feel

**UIPanel is OPTIONAL** - UIManager works with or without it!

---

## Animation Types Explained

### FadeIn/FadeOut
- Smoothly changes opacity from 0 to 1 (or reverse)
- Best for overlays, modals, notifications

### SlideInLeft/Right/Top/Bottom
- Panel slides in from the specified edge
- Has smooth easing (not linear)
- Best for side panels, menus

### Scale
- Panel scales from 0 to 1 (or reverse)
- Has a slight bounce effect (ease-out-back)
- Best for popups, dialogs

---

## Common Use Cases

### Case 1: Exclusive Panels (Only One Open at a Time)

Use `UIManager`:
- Set `Deactivate Others` to ✓ on all button-panel pairs
- Or set `Allow Multiple Panels` to ✗

### Case 2: Tab System

Use `UIButtonGroup`:
- Better visual feedback for tabs
- Automatic "selected" state
- Optional selection indicator

### Case 3: Independent Toggles

Use `UIManager`:
- Set `Toggle Mode` to ✓
- Set `Deactivate Others` to ✗
- Set `Allow Multiple Panels` to ✓

---

## Troubleshooting

### Animations not playing?
1. Make sure `UIPanel` component is attached to your panel GameObject
2. Make sure `Use Animation` is checked
3. Make sure panel has a `CanvasGroup` (UIPanel adds it automatically)
4. Make sure panel GameObject is set to inactive at start (this is correct!)

### Getting errors about inactive panels?
- **This is now fixed!** Panels can and should be inactive at start
- UIPanel will automatically initialize components when Show() is called
- UIManager now safely handles panels that are already in the correct state
- No need to have panels active at start unless you set `Active On Start` to true

### Getting "Coroutine couldn't be started" errors?
- **Fixed!** UIManager and UIPanel now check if panels are active before trying animations
- Only panels that need state changes will trigger animations
- No more errors when panels are already inactive

### Button not responding?
1. Check that button is assigned in UIManager
2. Check that button has an EventSystem in the scene
3. Check button's interactable checkbox

### Panel not showing?
1. Check that panel GameObject is assigned
2. Check panel's Canvas is enabled
3. Check panel is not behind other UI elements

---

## Events (Advanced)

You can listen for panel state changes:

```csharp
using Unbound.UI;

public class MyScript : MonoBehaviour
{
    [SerializeField] private UIPanel myPanel;
    
    void Start()
    {
        // Add listeners
        myPanel.onPanelShown.AddListener(OnPanelOpened);
        myPanel.onPanelHidden.AddListener(OnPanelClosed);
    }
    
    void OnPanelOpened()
    {
        Debug.Log("Panel opened!");
        // Pause game, play sound, etc.
    }
    
    void OnPanelClosed()
    {
        Debug.Log("Panel closed!");
        // Resume game
    }
}
```

---

## Performance Tips

1. **Deactivate panels** (SetActive false) rather than just hiding them - saves UI rendering
2. **Use simple animations** on complex panels (fade instead of slide)
3. **Pool panels** if you're creating many dynamically
4. **Disable raycasts** on hidden panels (use CanvasGroup.blocksRaycasts)

---

## Summary

1. **UIPanel is optional** but adds nice animations
2. **UIManager automatically detects** UIPanel and uses it
3. **Multiple animation types** available (Fade, Slide, Scale)
4. **Works with or without** UIPanel component
5. **Configure once** in inspector, works everywhere

That's it! You're all set up. Check the main README.md for more advanced examples.

