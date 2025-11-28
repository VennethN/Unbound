using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unbound.Player;

namespace Unbound.UI
{
    /// <summary>
    /// Manages UI panels and their associated buttons.
    /// Provides flexible configuration for activating/deactivating UI elements through buttons.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [System.Serializable]
        public class UIButtonPanelPair
        {
            [Tooltip("The button that will control the panel")]
            public Button button;
            
            [Tooltip("The panel GameObject to activate/deactivate")]
            public GameObject panel;
            
            [Tooltip("Should this button toggle the panel, or only activate it?")]
            public bool toggleMode = true;
            
            [Tooltip("Should clicking this button deactivate other panels?")]
            public bool deactivateOthers = true;
            
            [Tooltip("Visual feedback - optional color tint when panel is active")]
            public Color activeColor = Color.white;
            
            [Tooltip("Visual feedback - optional color tint when panel is inactive")]
            public Color inactiveColor = Color.gray;
            
            [Tooltip("Should we apply color feedback to the button?")]
            public bool useColorFeedback = false;
            
            [Tooltip("GameObjects to hide when this panel is shown")]
            public List<GameObject> hideOnShow = new List<GameObject>();
            
            [HideInInspector]
            public bool isActive = false;
        }
        
        [Header("UI Configuration")]
        [Tooltip("List of button-panel pairs to manage")]
        [SerializeField] private List<UIButtonPanelPair> buttonPanelPairs = new List<UIButtonPanelPair>();
        
        [Header("Behavior Settings")]
        [Tooltip("Should all panels start deactivated?")]
        [SerializeField] private bool startAllInactive = true;
        
        [Tooltip("Allow multiple panels to be open at once (overrides deactivateOthers)")]
        [SerializeField] private bool allowMultiplePanels = false;
        
        [Tooltip("Play audio feedback on button click")]
        [SerializeField] private bool useAudioFeedback = false;
        
        [SerializeField] private AudioClip buttonClickSound;
        
        [Header("Player Input Control")]
        [Tooltip("Disable player movement when any panel is open")]
        [SerializeField] private bool disablePlayerMovementOnPanelOpen = true;
        
        [Tooltip("Disable player combat when any panel is open")]
        [SerializeField] private bool disablePlayerCombatOnPanelOpen = true;
        
        private AudioSource audioSource;
        private Dictionary<Button, UIButtonPanelPair> buttonLookup;
        private PlayerController2D playerController;
        private PlayerCombat playerCombat;
        
        private void Awake()
        {
            InitializeButtonLookup();
            InitializeAudio();
            SetupButtonListeners();
        }
        
        private void Start()
        {
            // Cache player references
            playerController = FindFirstObjectByType<PlayerController2D>();
            playerCombat = FindFirstObjectByType<PlayerCombat>();
            
            if (startAllInactive)
            {
                DeactivateAllPanels();
            }
            else
            {
                // Initialize visual state based on panel active state
                foreach (var pair in buttonPanelPairs)
                {
                    pair.isActive = pair.panel != null && pair.panel.activeSelf;
                    UpdateButtonVisuals(pair);
                }
                
                // Check if any panels are initially active and update player input state
                UpdatePlayerInputState();
            }
        }
        
        private void InitializeButtonLookup()
        {
            buttonLookup = new Dictionary<Button, UIButtonPanelPair>();
            foreach (var pair in buttonPanelPairs)
            {
                if (pair.button != null)
                {
                    buttonLookup[pair.button] = pair;
                }
            }
        }
        
        private void InitializeAudio()
        {
            if (useAudioFeedback)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
            }
        }
        
        private void SetupButtonListeners()
        {
            foreach (var pair in buttonPanelPairs)
            {
                if (pair.button != null)
                {
                    pair.button.onClick.AddListener(() => OnButtonClicked(pair.button));
                }
                else
                {
                    Debug.LogWarning($"[UIManager] Button is null in one of the button-panel pairs on {gameObject.name}");
                }
            }
        }
        
        private void OnButtonClicked(Button clickedButton)
        {
            if (!buttonLookup.TryGetValue(clickedButton, out UIButtonPanelPair pair))
            {
                Debug.LogWarning($"[UIManager] Clicked button not found in lookup: {clickedButton.name}");
                return;
            }
            
            PlayClickSound();
            
            bool shouldActivate = !pair.isActive;
            
            // Handle toggle vs activate-only mode
            if (!pair.toggleMode && pair.isActive)
            {
                return; // Button only activates, doesn't deactivate
            }
            
            // Handle deactivating others
            if (shouldActivate && (pair.deactivateOthers && !allowMultiplePanels))
            {
                DeactivateAllPanels();
            }
            
            // Toggle/Set the target panel
            if (pair.toggleMode)
            {
                SetPanelActive(pair, shouldActivate);
            }
            else
            {
                SetPanelActive(pair, true);
            }
        }
        
        private void SetPanelActive(UIButtonPanelPair pair, bool active)
        {
            if (pair.panel == null)
            {
                Debug.LogWarning($"[UIManager] Panel is null for button: {pair.button?.name}");
                return;
            }
            
            pair.isActive = active;
            
            // Check if panel has UIPanel component for animations
            UIPanel uiPanel = pair.panel.GetComponent<UIPanel>();
            if (uiPanel != null)
            {
                // Use UIPanel's Show/Hide methods for animations
                if (active)
                {
                    uiPanel.Show();
                }
                else
                {
                    uiPanel.Hide();
                }
            }
            else
            {
                // Fallback to simple SetActive if no UIPanel component
                pair.panel.SetActive(active);
            }
            
            // Handle hideOnShow objects - hide them when this panel is shown, show them when hidden
            if (pair.hideOnShow != null)
            {
                foreach (var obj in pair.hideOnShow)
                {
                    if (obj != null)
                    {
                        obj.SetActive(!active);
                    }
                }
            }
            
            UpdateButtonVisuals(pair);
            
            // Fire events if needed
            OnPanelStateChanged(pair.panel, active);
        }
        
        private void UpdateButtonVisuals(UIButtonPanelPair pair)
        {
            if (!pair.useColorFeedback || pair.button == null) return;
            
            var colors = pair.button.colors;
            colors.normalColor = pair.isActive ? pair.activeColor : pair.inactiveColor;
            pair.button.colors = colors;
        }
        
        private void PlayClickSound()
        {
            if (useAudioFeedback && audioSource != null && buttonClickSound != null)
            {
                audioSource.PlayOneShot(buttonClickSound);
            }
        }
        
        #region Public API
        
        /// <summary>
        /// Activate a specific panel by its GameObject reference
        /// </summary>
        public void ActivatePanel(GameObject panel)
        {
            var pair = buttonPanelPairs.Find(p => p.panel == panel);
            if (pair != null && pair.panel != null && !pair.panel.activeSelf)
            {
                SetPanelActive(pair, true);
            }
        }
        
        /// <summary>
        /// Deactivate a specific panel by its GameObject reference
        /// </summary>
        public void DeactivatePanel(GameObject panel)
        {
            var pair = buttonPanelPairs.Find(p => p.panel == panel);
            if (pair != null && pair.panel != null && pair.panel.activeSelf)
            {
                SetPanelActive(pair, false);
            }
        }
        
        /// <summary>
        /// Toggle a specific panel by its GameObject reference
        /// </summary>
        public void TogglePanel(GameObject panel)
        {
            var pair = buttonPanelPairs.Find(p => p.panel == panel);
            if (pair != null)
            {
                bool shouldActivate = !pair.isActive;
                // Only toggle if there's an actual state change needed
                if (shouldActivate && !pair.panel.activeSelf)
                {
                    SetPanelActive(pair, true);
                }
                else if (!shouldActivate && pair.panel.activeSelf)
                {
                    SetPanelActive(pair, false);
                }
                else
                {
                    // State is already correct, just update the tracking
                    pair.isActive = pair.panel.activeSelf;
                }
            }
        }
        
        /// <summary>
        /// Deactivate all managed panels
        /// </summary>
        public void DeactivateAllPanels()
        {
            foreach (var pair in buttonPanelPairs)
            {
                // Only deactivate if the panel is currently active
                if (pair.panel != null && pair.panel.activeSelf)
                {
                    SetPanelActive(pair, false);
                }
                else
                {
                    // Panel is already inactive, just update the state
                    pair.isActive = false;
                }
            }
        }
        
        /// <summary>
        /// Activate all managed panels
        /// </summary>
        public void ActivateAllPanels()
        {
            foreach (var pair in buttonPanelPairs)
            {
                // Only activate if the panel is currently inactive
                if (pair.panel != null && !pair.panel.activeSelf)
                {
                    SetPanelActive(pair, true);
                }
                else
                {
                    // Panel is already active, just update the state
                    pair.isActive = true;
                }
            }
        }
        
        /// <summary>
        /// Check if a specific panel is currently active
        /// </summary>
        public bool IsPanelActive(GameObject panel)
        {
            var pair = buttonPanelPairs.Find(p => p.panel == panel);
            return pair != null && pair.isActive;
        }
        
        /// <summary>
        /// Get a list of all currently active panels
        /// </summary>
        public List<GameObject> GetActivePanels()
        {
            List<GameObject> activePanels = new List<GameObject>();
            foreach (var pair in buttonPanelPairs)
            {
                if (pair.isActive && pair.panel != null)
                {
                    activePanels.Add(pair.panel);
                }
            }
            return activePanels;
        }
        
        /// <summary>
        /// Add a new button-panel pair at runtime
        /// </summary>
        public void AddButtonPanelPair(Button button, GameObject panel, bool toggleMode = true, bool deactivateOthers = true)
        {
            var newPair = new UIButtonPanelPair
            {
                button = button,
                panel = panel,
                toggleMode = toggleMode,
                deactivateOthers = deactivateOthers,
                isActive = panel.activeSelf
            };
            
            buttonPanelPairs.Add(newPair);
            
            if (button != null)
            {
                buttonLookup[button] = newPair;
                button.onClick.AddListener(() => OnButtonClicked(button));
            }
        }
        
        /// <summary>
        /// Remove a button-panel pair by button reference
        /// </summary>
        public void RemoveButtonPanelPair(Button button)
        {
            if (buttonLookup.TryGetValue(button, out UIButtonPanelPair pair))
            {
                buttonPanelPairs.Remove(pair);
                buttonLookup.Remove(button);
                button.onClick.RemoveListener(() => OnButtonClicked(button));
            }
        }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Override this method to add custom behavior when a panel state changes
        /// </summary>
        protected virtual void OnPanelStateChanged(GameObject panel, bool isActive)
        {
            // Update player input state when any panel changes
            UpdatePlayerInputState();
        }
        
        #endregion
        
        #region Player Input Control
        
        /// <summary>
        /// Updates player movement and combat based on whether any panels are open
        /// </summary>
        private void UpdatePlayerInputState()
        {
            bool anyPanelActive = GetActivePanels().Count > 0;
            
            // Disable/enable player movement
            if (disablePlayerMovementOnPanelOpen)
            {
                if (playerController == null)
                {
                    playerController = FindFirstObjectByType<PlayerController2D>();
                }
                
                if (playerController != null)
                {
                    playerController.SetMovementEnabled(!anyPanelActive);
                }
            }
            
            // Disable/enable player combat
            if (disablePlayerCombatOnPanelOpen)
            {
                if (playerCombat == null)
                {
                    playerCombat = FindFirstObjectByType<PlayerCombat>();
                }
                
                if (playerCombat != null)
                {
                    playerCombat.SetCombatEnabled(!anyPanelActive);
                }
            }
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up button listeners
            foreach (var pair in buttonPanelPairs)
            {
                if (pair.button != null)
                {
                    pair.button.onClick.RemoveListener(() => OnButtonClicked(pair.button));
                }
            }
        }
    }
}

