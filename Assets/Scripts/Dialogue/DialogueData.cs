using System.Collections.Generic;
using UnityEngine;

namespace Unbound.Dialogue
{
    /// <summary>
    /// Data class containing complete dialogue data for a conversation or interaction.
    /// Loaded from JSON files instead of ScriptableObjects.
    /// </summary>
    [System.Serializable]
    public class DialogueData : IDialogueDataProvider
    {
        [Header("Dialogue Metadata")]
        public string dialogueID;
        public string displayName;

        [Header("Dialogue Flow")]
        [Tooltip("Legacy: Single start node ID (for backward compatibility). If startNodeConditions is empty, this will be used.")]
        public string startNodeID;

        [Tooltip("Multiple start nodes with flag conditions. First matching condition will be used. If none match and startNodeID is set, that will be used as fallback.")]
        public List<StartNodeCondition> startNodeConditions = new List<StartNodeCondition>();

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
        /// Gets the appropriate start node ID based on conditions
        /// Returns the first matching start node condition, or falls back to startNodeID
        /// </summary>
        public string GetStartNodeID(IDialogueConditionEvaluator evaluator)
        {
            // Check start node conditions first
            if (startNodeConditions != null && startNodeConditions.Count > 0)
            {
                foreach (var startCondition in startNodeConditions)
                {
                    if (startCondition.EvaluateConditions(evaluator))
                    {
                        if (!string.IsNullOrEmpty(startCondition.nodeID))
                        {
                            return startCondition.nodeID;
                        }
                    }
                }
            }

            // Fallback to legacy startNodeID
            return startNodeID;
        }

        /// <summary>
        /// Checks if any start node conditions are satisfied (for indicator visibility)
        /// Returns true if at least one start node condition is met, or if there are no conditions
        /// </summary>
        public bool HasValidStartNode(IDialogueConditionEvaluator evaluator)
        {
            // If no start node conditions are defined, check if legacy startNodeID exists
            if (startNodeConditions == null || startNodeConditions.Count == 0)
            {
                return !string.IsNullOrEmpty(startNodeID);
            }

            // Check if any start node condition is satisfied
            foreach (var startCondition in startNodeConditions)
            {
                if (startCondition.EvaluateConditions(evaluator))
                {
                    if (!string.IsNullOrEmpty(startCondition.nodeID))
                    {
                        return true;
                    }
                }
            }

            // If no conditions matched but legacy startNodeID exists, return true (fallback)
            return !string.IsNullOrEmpty(startNodeID);
        }

        /// <summary>
        /// Validates the dialogue structure
        /// </summary>
        public bool IsValid()
        {
            return string.IsNullOrEmpty(GetValidationErrors());
        }

        /// <summary>
        /// Gets detailed validation errors for this dialogue data
        /// </summary>
        public string GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(dialogueID))
            {
                errors.Add("Dialogue ID is null or empty");
            }

            // Validate start node conditions
            if (startNodeConditions != null && startNodeConditions.Count > 0)
            {
                foreach (var startCondition in startNodeConditions)
                {
                    if (string.IsNullOrEmpty(startCondition.nodeID))
                    {
                        errors.Add("Start node condition has empty node ID");
                    }
                    else if (GetNode(startCondition.nodeID) == null)
                    {
                        errors.Add($"Start node condition references non-existent node '{startCondition.nodeID}'");
                    }
                }
            }
            else if (string.IsNullOrEmpty(startNodeID))
            {
                // Only require startNodeID if no start node conditions are defined
                errors.Add("Start node ID is null or empty and no start node conditions are defined");
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

            return errors.Count > 0 ? $"DialogueData '{dialogueID}' validation failed: {string.Join("; ", errors)}" : string.Empty;
        }
    }
}

