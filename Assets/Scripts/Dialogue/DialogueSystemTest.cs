using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Test script to demonstrate the dialogue system functionality
    /// </summary>
    public class DialogueSystemTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private DialogueAsset testDialogueAsset;
        [SerializeField] private KeyCode testKey = KeyCode.T;

        private DialogueController dialogueController;

        private void Start()
        {
            dialogueController = FindFirstObjectByType<DialogueController>();
            if (dialogueController == null)
            {
                Debug.LogError("No DialogueController found in scene!");
                return;
            }

            Debug.Log("Dialogue System Test initialized. Press " + testKey + " to test dialogue.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(testKey) && dialogueController != null)
            {
                if (dialogueController.IsDialogueActive())
                {
                    dialogueController.EndDialogue();
                    Debug.Log("Ended current dialogue");
                }
                else if (testDialogueAsset != null)
                {
                    dialogueController.StartDialogue(testDialogueAsset);
                    Debug.Log("Started test dialogue: " + testDialogueAsset.displayName);
                }
                else
                {
                    Debug.LogWarning("No test dialogue asset assigned!");
                }
            }
        }

        /// <summary>
        /// Creates a simple test dialogue asset programmatically for testing
        /// </summary>
        [ContextMenu("Create Test Dialogue Asset")]
        private void CreateTestDialogueAsset()
        {
            // This would normally be done through the Unity editor or a custom editor window
            // For now, just log instructions
            Debug.Log("To create a test dialogue asset:");
            Debug.Log("1. Create a new DialogueAsset ScriptableObject (Assets > Create > Unbound > Dialogue > Dialogue Asset)");
            Debug.Log("2. Set the dialogueID and startNodeID");
            Debug.Log("3. Add DialogueNode entries with dialogue text and choices");
            Debug.Log("4. Assign the asset to the testDialogueAsset field in this component");
        }
    }
}
