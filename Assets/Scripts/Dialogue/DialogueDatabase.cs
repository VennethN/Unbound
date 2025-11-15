using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Singleton manager that loads and manages dialogues from JSON files
    /// </summary>
    public class DialogueDatabase : MonoBehaviour
    {
        private static DialogueDatabase _instance;
        
        [Header("Dialogue Data Path")]
        [Tooltip("Path to the dialogues folder relative to Resources folder (e.g., 'Data/Dialogues')")]
        [SerializeField] private string dialoguesPath = "Data/Dialogues";
        
        private Dictionary<string, DialogueData> _dialogues = new Dictionary<string, DialogueData>();
        private bool _isLoaded = false;
        
        public static DialogueDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DialogueDatabase>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("DialogueDatabase");
                        _instance = go.AddComponent<DialogueDatabase>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadDialogues();
        }
        
        /// <summary>
        /// Loads all dialogues from JSON files in the Resources folder
        /// </summary>
        public void LoadDialogues()
        {
            if (_isLoaded) return;
            
            _dialogues.Clear();
            
            // Load all JSON files from the Resources folder
            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(dialoguesPath);
            
            foreach (TextAsset jsonFile in jsonFiles)
            {
                try
                {
                    DialogueData dialogueData = JsonUtility.FromJson<DialogueData>(jsonFile.text);
                    
                    if (dialogueData != null && dialogueData.IsValid())
                    {
                        if (_dialogues.ContainsKey(dialogueData.dialogueID))
                        {
                            Debug.LogWarning($"Duplicate dialogue ID found: {dialogueData.dialogueID} in file {jsonFile.name}. Skipping.");
                            continue;
                        }
                        
                        _dialogues[dialogueData.dialogueID] = dialogueData;
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to validate dialogue from file {jsonFile.name}: {dialogueData?.GetValidationErrors() ?? "null data"}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading dialogue from {jsonFile.name}: {e.Message}");
                }
            }
            
            _isLoaded = true;
            Debug.Log($"Loaded {_dialogues.Count} dialogues from database");
        }
        
        /// <summary>
        /// Gets a dialogue by its ID
        /// </summary>
        public DialogueData GetDialogue(string dialogueID)
        {
            if (string.IsNullOrEmpty(dialogueID))
                return null;
            
            if (!_isLoaded)
                LoadDialogues();
            
            _dialogues.TryGetValue(dialogueID, out DialogueData dialogue);
            return dialogue;
        }
        
        /// <summary>
        /// Checks if a dialogue exists in the database
        /// </summary>
        public bool HasDialogue(string dialogueID)
        {
            if (string.IsNullOrEmpty(dialogueID))
                return false;
            
            if (!_isLoaded)
                LoadDialogues();
            
            return _dialogues.ContainsKey(dialogueID);
        }
        
        /// <summary>
        /// Gets all loaded dialogue IDs
        /// </summary>
        public IEnumerable<string> GetAllDialogueIDs()
        {
            if (!_isLoaded)
                LoadDialogues();
            
            return _dialogues.Keys;
        }
        
        /// <summary>
        /// Reloads all dialogues from JSON files
        /// </summary>
        public void ReloadDialogues()
        {
            _isLoaded = false;
            LoadDialogues();
        }
    }
}

