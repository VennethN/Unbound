using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unbound.UI
{
    /// <summary>
    /// Manages a group of buttons where only one can be selected at a time (radio button behavior).
    /// Useful for tab navigation or exclusive selections.
    /// </summary>
    public class UIButtonGroup : MonoBehaviour
    {
        [System.Serializable]
        public class ButtonData
        {
            public Button button;
            public GameObject associatedPanel;
            
            [Header("Visual Feedback")]
            public bool useColorFeedback = true;
            public Color selectedColor = new Color(1f, 1f, 1f, 1f);
            public Color deselectedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            
            [Header("Optional Animation")]
            public GameObject selectionIndicator;
            
            [HideInInspector]
            public bool isSelected = false;
        }
        
        [Header("Button Configuration")]
        [SerializeField] private List<ButtonData> buttons = new List<ButtonData>();
        
        [Header("Behavior")]
        [Tooltip("Index of the button to select on start (-1 for none)")]
        [SerializeField] private int defaultSelectedIndex = 0;
        
        [Tooltip("Allow deselecting the current button (leaving none selected)")]
        [SerializeField] private bool allowDeselect = false;
        
        [Header("Layout")]
        [Tooltip("Automatically arrange buttons in a row or column")]
        [SerializeField] private bool autoLayout = false;
        
        [SerializeField] private LayoutType layoutType = LayoutType.Horizontal;
        [SerializeField] private float spacing = 10f;
        
        public enum LayoutType
        {
            Horizontal,
            Vertical
        }
        
        private int currentSelectedIndex = -1;
        
        private void Awake()
        {
            SetupButtons();
            
            if (autoLayout)
            {
                SetupLayout();
            }
        }
        
        private void Start()
        {
            if (defaultSelectedIndex >= 0 && defaultSelectedIndex < buttons.Count)
            {
                SelectButton(defaultSelectedIndex);
            }
        }
        
        private void SetupButtons()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                ButtonData buttonData = buttons[i];
                if (buttonData.button == null) continue;
                
                int index = i; // Capture for closure
                buttonData.button.onClick.AddListener(() => OnButtonClicked(index));
            }
        }
        
        private void SetupLayout()
        {
            if (layoutType == LayoutType.Horizontal)
            {
                HorizontalLayoutGroup horizontalLayout = GetComponent<HorizontalLayoutGroup>();
                if (horizontalLayout == null)
                {
                    horizontalLayout = gameObject.AddComponent<HorizontalLayoutGroup>();
                }
                horizontalLayout.spacing = spacing;
                horizontalLayout.childAlignment = TextAnchor.MiddleCenter;
                horizontalLayout.childControlHeight = false;
                horizontalLayout.childControlWidth = false;
                horizontalLayout.childForceExpandHeight = false;
                horizontalLayout.childForceExpandWidth = false;
            }
            else
            {
                VerticalLayoutGroup verticalLayout = GetComponent<VerticalLayoutGroup>();
                if (verticalLayout == null)
                {
                    verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
                }
                verticalLayout.spacing = spacing;
                verticalLayout.childAlignment = TextAnchor.MiddleCenter;
                verticalLayout.childControlHeight = false;
                verticalLayout.childControlWidth = false;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.childForceExpandWidth = false;
            }
        }
        
        private void OnButtonClicked(int index)
        {
            if (index == currentSelectedIndex && !allowDeselect)
            {
                // Already selected and can't deselect
                return;
            }
            
            if (index == currentSelectedIndex && allowDeselect)
            {
                // Deselect current
                DeselectButton(index);
                currentSelectedIndex = -1;
            }
            else
            {
                // Select new button
                SelectButton(index);
            }
        }
        
        /// <summary>
        /// Select a button by index
        /// </summary>
        public void SelectButton(int index)
        {
            if (index < 0 || index >= buttons.Count) return;
            
            // Deselect previous
            if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Count)
            {
                DeselectButton(currentSelectedIndex);
            }
            
            // Select new
            currentSelectedIndex = index;
            ButtonData buttonData = buttons[index];
            buttonData.isSelected = true;
            
            // Update visuals
            UpdateButtonVisuals(buttonData, true);
            
            // Show associated panel
            if (buttonData.associatedPanel != null)
            {
                UIPanel uiPanel = buttonData.associatedPanel.GetComponent<UIPanel>();
                if (uiPanel != null)
                {
                    uiPanel.Show();
                }
                else
                {
                    buttonData.associatedPanel.SetActive(true);
                }
            }
            
            // Show selection indicator
            if (buttonData.selectionIndicator != null)
            {
                buttonData.selectionIndicator.SetActive(true);
            }
        }
        
        private void DeselectButton(int index)
        {
            if (index < 0 || index >= buttons.Count) return;
            
            ButtonData buttonData = buttons[index];
            buttonData.isSelected = false;
            
            // Update visuals
            UpdateButtonVisuals(buttonData, false);
            
            // Hide associated panel
            if (buttonData.associatedPanel != null)
            {
                UIPanel uiPanel = buttonData.associatedPanel.GetComponent<UIPanel>();
                if (uiPanel != null)
                {
                    uiPanel.Hide();
                }
                else
                {
                    buttonData.associatedPanel.SetActive(false);
                }
            }
            
            // Hide selection indicator
            if (buttonData.selectionIndicator != null)
            {
                buttonData.selectionIndicator.SetActive(false);
            }
        }
        
        private void UpdateButtonVisuals(ButtonData buttonData, bool selected)
        {
            if (!buttonData.useColorFeedback || buttonData.button == null) return;
            
            var colors = buttonData.button.colors;
            colors.normalColor = selected ? buttonData.selectedColor : buttonData.deselectedColor;
            buttonData.button.colors = colors;
        }
        
        /// <summary>
        /// Get the currently selected button index
        /// </summary>
        public int GetSelectedIndex()
        {
            return currentSelectedIndex;
        }
        
        /// <summary>
        /// Get the currently selected button
        /// </summary>
        public Button GetSelectedButton()
        {
            if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Count)
            {
                return buttons[currentSelectedIndex].button;
            }
            return null;
        }
        
        /// <summary>
        /// Select the next button in the group
        /// </summary>
        public void SelectNext()
        {
            if (buttons.Count == 0) return;
            
            int nextIndex = (currentSelectedIndex + 1) % buttons.Count;
            SelectButton(nextIndex);
        }
        
        /// <summary>
        /// Select the previous button in the group
        /// </summary>
        public void SelectPrevious()
        {
            if (buttons.Count == 0) return;
            
            int prevIndex = currentSelectedIndex - 1;
            if (prevIndex < 0) prevIndex = buttons.Count - 1;
            SelectButton(prevIndex);
        }
        
        private void OnDestroy()
        {
            // Clean up listeners
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i].button != null)
                {
                    int index = i;
                    buttons[i].button.onClick.RemoveListener(() => OnButtonClicked(index));
                }
            }
        }
    }
}

