namespace Unbound.Dialogue
{
    /// <summary>
    /// Interface for classes that provide dialogue data (DialogueAsset and DialogueData)
    /// </summary>
    public interface IDialogueDataProvider
    {
        DialogueNode GetNode(string nodeID);
    }
}
