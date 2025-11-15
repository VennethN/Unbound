# UI Management System

A flexible and comprehensive UI management system for Unity that handles button-panel relationships, panel animations, and UI navigation.

## Overview

The UI Management System provides three main components:
1. **UIManager** - Manages multiple button-panel pairs with flexible activation/deactivation
2. **UIPanel** - Optional enhanced panel component with animations and events
3. **UIButtonGroup** - Radio button behavior for exclusive selections (tabs, navigation)

---

## Table of Contents

- [Quick Start](#quick-start)
- [Components](#components)
  - [UIManager](#uimanager)
  - [UIPanel](#uipanel)
  - [UIButtonGroup](#uibuttongroup)
- [Usage Examples](#usage-examples)
- [Advanced Features](#advanced-features)

---

## Quick Start

### Basic Setup (UIManager)

1. Create a new GameObject and add the `UIManager` component
2. Create your UI buttons and panels in the scene
3. In the UIManager inspector, add button-panel pairs:
   - Assign the button
   - Assign the panel GameObject
   - Configure toggle mode and other options
4. Run the scene - buttons will now control their associated panels

### Radio Button Setup (UIButtonGroup)

1. Create a GameObject for your button group and add the `UIButtonGroup` component
2. Add buttons as children (or assign existing buttons)
3. In the UIButtonGroup inspector, add your buttons and their associated panels
4. Set the default selected index
5. Run the scene - only one panel will be active at a time

---

## Components

### UIManager

The main UI management component that handles button-to-panel relationships.

#### Features
- **Toggle or Activate-Only Modes**: Choose whether buttons toggle panels or only activate them
- **Exclusive Panel Mode**: Automatically deactivate other panels when one is activated
- **Multiple Panel Support**: Allow multiple panels to be open simultaneously
- **Visual Feedback**: Optional color tinting for active/inactive states
- **Audio Feedback**: Optional click sounds
- **Runtime Management**: Add/remove button-panel pairs at runtime
- **Public API**: Programmatic control over panel states

#### Inspector Properties

**UI Configuration:**
- `Button Panel Pairs`: List of button-panel relationships
  - `Button`: The button that controls the panel
  - `Panel`: The GameObject to activate/deactivate
  - `Toggle Mode`: If true, button toggles the panel; if false, only activates
  - `Deactivate Others`: When activating this panel, deactivate all others
  - `Active Color`: Visual tint when panel is active (requires Use Color Feedback)
  - `Inactive Color`: Visual tint when panel is inactive
  - `Use Color Feedback`: Enable/disable color feedback for this button

**Behavior Settings:**
- `Start All Inactive`: Deactivate all panels on start
- `Allow Multiple Panels`: Allow multiple panels to be open at once (overrides Deactivate Others)
- `Use Audio Feedback`: Play sound on button click
- `Button Click Sound`: Audio clip to play on click

#### Public Methods

```csharp
// Activate/deactivate specific panels
void ActivatePanel(GameObject panel)
void DeactivatePanel(GameObject panel)
void TogglePanel(GameObject panel)

// Batch operations
void ActivateAllPanels()
void DeactivateAllPanels()

// Query state
bool IsPanelActive(GameObject panel)
List<GameObject> GetActivePanels()

// Runtime management
void AddButtonPanelPair(Button button, GameObject panel, bool toggleMode = true, bool deactivateOthers = true)
void RemoveButtonPanelPair(Button button)
```

#### Example Usage

```csharp
using Unbound.UI;

public class GameMenuController : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject inventoryPanel;
    
    public void OpenSettings()
    {
        uiManager.ActivatePanel(settingsPanel);
    }
    
    public void CloseAllMenus()
    {
        uiManager.DeactivateAllPanels();
    }
    
    public void ToggleInventory()
    {
        uiManager.TogglePanel(inventoryPanel);
    }
}
```

---

### UIPanel

Optional enhanced panel component that adds animations, events, and additional functionality.

#### Features
- **Animations**: Fade, slide, and scale animations for showing/hiding
- **Unity Events**: Trigger events when panel is shown/hidden
- **Panel ID**: Unique identifier for easy lookup
- **Auto-start State**: Configure initial active state

#### Inspector Properties

**Panel Settings:**
- `Panel Id`: Unique identifier for this panel
- `Active On Start`: Should this panel be active on start

**Animation Settings:**
- `Use Animation`: Enable animations for show/hide
- `Animation Duration`: Duration in seconds
- `Show Animation`: Animation type when showing (FadeIn, SlideInLeft, SlideInRight, SlideInTop, SlideInBottom, Scale)
- `Hide Animation`: Animation type when hiding (FadeOut, SlideOut directions, Scale)

**Events:**
- `On Panel Shown`: Invoked when panel is shown
- `On Panel Hidden`: Invoked when panel is hidden

#### Animation Types

- `None`: No animation
- `FadeIn/FadeOut`: Alpha fade in/out
- `SlideInLeft/Right/Top/Bottom`: Slide from edges
- `Scale`: Scale from/to zero with bounce effect

#### Public Methods

```csharp
void Show()           // Show the panel with animation
void Hide()           // Hide the panel with animation
void Toggle()         // Toggle visibility
bool IsActive { get; } // Check if panel is active
string PanelId { get; } // Get panel ID
```

#### Example Usage

```csharp
using Unbound.UI;

public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private UIPanel settingsPanel;
    
    private void Start()
    {
        // Listen for panel events
        settingsPanel.onPanelShown.AddListener(OnSettingsOpened);
        settingsPanel.onPanelHidden.AddListener(OnSettingsClosed);
    }
    
    public void OpenSettings()
    {
        settingsPanel.Show(); // Animated show
    }
    
    private void OnSettingsOpened()
    {
        Debug.Log("Settings menu opened!");
        // Pause game, play sound, etc.
    }
    
    private void OnSettingsClosed()
    {
        Debug.Log("Settings menu closed!");
        // Resume game
    }
}
```

---

### UIButtonGroup

Manages a group of buttons with radio button behavior (only one selected at a time).

#### Features
- **Exclusive Selection**: Only one button can be selected at a time
- **Visual Feedback**: Color tinting for selected/deselected states
- **Selection Indicators**: Optional GameObjects to show/hide for selection state
- **Navigation**: Next/Previous selection methods
- **Auto Layout**: Optional automatic horizontal/vertical layout
- **Optional Deselection**: Allow deselecting to have no selection

#### Inspector Properties

**Button Configuration:**
- `Buttons`: List of button data
  - `Button`: The button component
  - `Associated Panel`: Panel to show when selected
  - `Use Color Feedback`: Enable color feedback
  - `Selected Color`: Color when selected
  - `Deselected Color`: Color when deselected
  - `Selection Indicator`: Optional GameObject to show when selected

**Behavior:**
- `Default Selected Index`: Index to select on start (-1 for none)
- `Allow Deselect`: Allow clicking selected button to deselect it

**Layout:**
- `Auto Layout`: Automatically arrange buttons
- `Layout Type`: Horizontal or Vertical
- `Spacing`: Space between buttons

#### Public Methods

```csharp
void SelectButton(int index)    // Select button by index
int GetSelectedIndex()          // Get currently selected index
Button GetSelectedButton()      // Get currently selected button
void SelectNext()              // Select next button
void SelectPrevious()          // Select previous button
```

#### Example Usage

```csharp
using Unbound.UI;

public class TabNavigationExample : MonoBehaviour
{
    [SerializeField] private UIButtonGroup tabGroup;
    
    private void Update()
    {
        // Navigate with keyboard
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (Input.GetKey(KeyCode.LeftShift))
                tabGroup.SelectPrevious();
            else
                tabGroup.SelectNext();
        }
    }
    
    public void SelectInventoryTab()
    {
        tabGroup.SelectButton(0);
    }
    
    public void SelectSkillsTab()
    {
        tabGroup.SelectButton(1);
    }
}
```

---

## Usage Examples

### Example 1: Settings Menu with Multiple Panels

```csharp
using Unbound.UI;
using UnityEngine;

public class SettingsMenuExample : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject controlsPanel;
    
    public void ShowVideoSettings()
    {
        uiManager.ActivatePanel(videoPanel);
    }
    
    public void ShowAudioSettings()
    {
        uiManager.ActivatePanel(audioPanel);
    }
    
    public void ShowControlsSettings()
    {
        uiManager.ActivatePanel(controlsPanel);
    }
    
    public void CloseSettings()
    {
        uiManager.DeactivateAllPanels();
    }
}
```

### Example 2: Tab System with UIButtonGroup

```csharp
using Unbound.UI;
using UnityEngine;

public class InventoryTabs : MonoBehaviour
{
    [SerializeField] private UIButtonGroup tabGroup;
    
    private void Start()
    {
        // Select the first tab by default
        tabGroup.SelectButton(0);
    }
    
    private void Update()
    {
        // Allow keyboard navigation
        if (Input.GetKeyDown(KeyCode.Q))
        {
            tabGroup.SelectPrevious();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            tabGroup.SelectNext();
        }
    }
}
```

### Example 3: Dynamic UI with Runtime Panel Management

```csharp
using Unbound.UI;
using UnityEngine;
using UnityEngine.UI;

public class DynamicMenuExample : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Button buttonPrefab;
    [SerializeField] private GameObject panelPrefab;
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private Transform panelContainer;
    
    public void AddNewMenuOption(string optionName)
    {
        // Create button
        Button newButton = Instantiate(buttonPrefab, buttonContainer);
        newButton.GetComponentInChildren<Text>().text = optionName;
        
        // Create panel
        GameObject newPanel = Instantiate(panelPrefab, panelContainer);
        newPanel.name = optionName + " Panel";
        
        // Add to UI manager
        uiManager.AddButtonPanelPair(newButton, newPanel, toggleMode: true, deactivateOthers: true);
    }
}
```

### Example 4: Animated Panels with Events

```csharp
using Unbound.UI;
using UnityEngine;

public class AnimatedMenuExample : MonoBehaviour
{
    [SerializeField] private UIPanel mainMenuPanel;
    [SerializeField] private UIPanel pauseMenuPanel;
    [SerializeField] private AudioSource menuAudioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    
    private void Start()
    {
        // Setup event listeners
        mainMenuPanel.onPanelShown.AddListener(() => OnMenuOpened(mainMenuPanel));
        mainMenuPanel.onPanelHidden.AddListener(() => OnMenuClosed(mainMenuPanel));
        
        pauseMenuPanel.onPanelShown.AddListener(() => OnMenuOpened(pauseMenuPanel));
        pauseMenuPanel.onPanelHidden.AddListener(() => OnMenuClosed(pauseMenuPanel));
    }
    
    private void OnMenuOpened(UIPanel panel)
    {
        Debug.Log($"{panel.PanelId} opened!");
        menuAudioSource.PlayOneShot(openSound);
        Time.timeScale = 0f; // Pause game
    }
    
    private void OnMenuClosed(UIPanel panel)
    {
        Debug.Log($"{panel.PanelId} closed!");
        menuAudioSource.PlayOneShot(closeSound);
        Time.timeScale = 1f; // Resume game
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            pauseMenuPanel.Toggle();
        }
    }
}
```

---

## Advanced Features

### Extending UIManager with Custom Behavior

You can extend `UIManager` to add custom behavior when panels change state:

```csharp
using Unbound.UI;
using UnityEngine;

public class CustomUIManager : UIManager
{
    protected override void OnPanelStateChanged(GameObject panel, bool isActive)
    {
        base.OnPanelStateChanged(panel, isActive);
        
        // Custom behavior
        if (isActive)
        {
            Debug.Log($"Panel {panel.name} was activated!");
            // Save to preferences, trigger analytics, etc.
        }
        else
        {
            Debug.Log($"Panel {panel.name} was deactivated!");
        }
    }
}
```

### Combining UIManager and UIPanel

For best results, combine both components:

1. Attach `UIPanel` to your panel GameObjects
2. Configure animations on the UIPanel
3. Use `UIManager` to handle button-panel relationships
4. The UIManager will activate/deactivate GameObjects, which triggers UIPanel animations

### Keyboard Navigation for Button Groups

```csharp
using Unbound.UI;
using UnityEngine;

public class KeyboardNavigationExample : MonoBehaviour
{
    [SerializeField] private UIButtonGroup buttonGroup;
    
    private void Update()
    {
        // Arrow key navigation
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            buttonGroup.SelectNext();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            buttonGroup.SelectPrevious();
        }
        
        // Number key selection
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                buttonGroup.SelectButton(i);
            }
        }
    }
}
```

---

## Best Practices

1. **Organization**: Keep UI hierarchy clean by grouping related panels under a common parent
2. **Performance**: Deactivate panels instead of just hiding them (SetActive false) to save on UI updates
3. **Animations**: Use UIPanel animations sparingly on complex panels to maintain performance
4. **Events**: Use UnityEvents on UIPanel for loose coupling between systems
5. **Runtime Creation**: When creating UI at runtime, always add proper cleanup in OnDestroy

---

## Notes

- All components use the `Unbound.UI` namespace
- UIManager automatically adds AudioSource if audio feedback is enabled
- UIPanel automatically adds CanvasGroup if animations are enabled
- Button listeners are properly cleaned up in OnDestroy
- All public methods are safe to call even if references are null (with appropriate warnings)

---

## Support

For issues, questions, or feature requests, please refer to the main project documentation.









