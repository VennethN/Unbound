using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Test script to demonstrate the dialogue system functionality
    /// </summary>
    public class DialogueSystemTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testDialogueID;
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
                else if (!string.IsNullOrEmpty(testDialogueID))
                {
                    dialogueController.StartDialogue(testDialogueID);
                    Debug.Log("Started test dialogue: " + testDialogueID);
                }
                else
                {
                    Debug.LogWarning("No test dialogue ID assigned!");
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
            Debug.Log("To create a test dialogue:");
            Debug.Log("1. Create a JSON file in Assets/Resources/Data/Dialogues/");
            Debug.Log("2. Set the dialogueID and startNodeID in the JSON");
            Debug.Log("3. Add DialogueNode entries with dialogue text and choices");
            Debug.Log("4. Assign the dialogueID to the testDialogueID field in this component");
        }
    }
}
