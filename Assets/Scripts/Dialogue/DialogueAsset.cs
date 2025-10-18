using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// ScriptableObject containing complete dialogue data for a conversation or interaction.
    /// </summary>
    [CreateAssetMenu(menuName = "Unbound/Dialogue/Dialogue Asset")]
    public class DialogueAsset : ScriptableObject
    {
        [Header("Dialogue Metadata")]
        public string dialogueID;
        public string displayName;

        [Header("Dialogue Flow")]
        public string startNodeID;
        public List<DialogueNode> nodes = new List<DialogueNode>();

        [Header("Localization")]
        public string localizationTable = "Dialogue";

        /// <summary>
        /// Gets a dialogue node by ID
        /// </summary>
        public DialogueNode GetNode(string nodeID)
        {
            return nodes.Find(node => node.nodeID == nodeID);
        }

        /// <summary>
        /// Gets all dialogue nodes
        /// </summary>
        public IReadOnlyList<DialogueNode> GetAllNodes()
        {
            return nodes.AsReadOnly();
        }

        /// <summary>
        /// Validates the dialogue structure
        /// </summary>
        public bool IsValid()
        {
            return string.IsNullOrEmpty(GetValidationErrors());
        }

        /// <summary>
        /// Gets detailed validation errors for this dialogue asset
        /// </summary>
        public string GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(dialogueID))
            {
                errors.Add("Dialogue ID is null or empty");
            }

            if (string.IsNullOrEmpty(startNodeID))
            {
                errors.Add("Start node ID is null or empty");
            }
            else if (GetNode(startNodeID) == null)
            {
                errors.Add($"Start node '{startNodeID}' does not exist in nodes list");
            }

            if (nodes.Count == 0)
            {
                errors.Add("Dialogue has no nodes");
            }
            else
            {
                // Validate all nodes reference existing nodes
                foreach (var node in nodes)
                {
                    string nodeError = node.GetValidationErrors(this);
                    if (!string.IsNullOrEmpty(nodeError))
                    {
                        errors.Add(nodeError);
                    }
                }
            }

            return errors.Count > 0 ? $"DialogueAsset '{name}' validation failed: {string.Join("; ", errors)}" : string.Empty;
        }

        private void OnValidate()
        {
            // Auto-generate ID if empty
            if (string.IsNullOrEmpty(dialogueID))
            {
                dialogueID = $"dialogue_{name.ToLower().Replace(" ", "_")}";
            }

            // Ensure display name matches asset name if empty
            if (string.IsNullOrEmpty(displayName))
            {
                displayName = name;
            }
        }
    }
}
