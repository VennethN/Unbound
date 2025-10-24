using UnityEngine;
using UnityEngine.UI;

namespace Unbound.UI
{
    /// <summary>
    /// Example script demonstrating how to use the UIManager system.
    /// Attach this to a GameObject with UIManager to test the functionality.
    /// </summary>
    public class UIManagerExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIManager uiManager;
        
        [Header("Example Panels - for testing")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject skillsPanel;
        [SerializeField] private GameObject questPanel;
        
        private void Update()
        {
            // Example: Keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.I))
            {
                uiManager.TogglePanel(inventoryPanel);
            }
            
            if (Input.GetKeyDown(KeyCode.K))
            {
                uiManager.TogglePanel(skillsPanel);
            }
            
            if (Input.GetKeyDown(KeyCode.J))
            {
                uiManager.TogglePanel(questPanel);
            }
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                uiManager.DeactivateAllPanels();
            }
        }
        
        // Example methods that can be called from buttons
        public void OpenInventory()
        {
            uiManager.ActivatePanel(inventoryPanel);
        }
        
        public void OpenSkills()
        {
            uiManager.ActivatePanel(skillsPanel);
        }
        
        public void OpenQuests()
        {
            uiManager.ActivatePanel(questPanel);
        }
        
        public void CloseAllMenus()
        {
            uiManager.DeactivateAllPanels();
        }
        
        public void LogActivePanels()
        {
            var activePanels = uiManager.GetActivePanels();
            Debug.Log($"Currently {activePanels.Count} panels are active:");
            foreach (var panel in activePanels)
            {
                Debug.Log($"  - {panel.name}");
            }
        }
    }
}

