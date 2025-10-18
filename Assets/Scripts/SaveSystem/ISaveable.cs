using System;

/// <summary>
/// Interface for objects that can be saved and loaded.
/// Implement this interface to make any game object saveable.
/// </summary>
public interface ISaveable
{
    /// <summary>
    /// Unique identifier for this saveable object
    /// </summary>
    string SaveID { get; }
    
    /// <summary>
    /// Captures the current state of the object and returns serializable data
    /// </summary>
    /// <returns>Object containing the state data to be saved</returns>
    object CaptureState();
    
    /// <summary>
    /// Restores the object's state from saved data
    /// </summary>
    /// <param name="state">The saved state data to restore from</param>
    void RestoreState(object state);
}

/// <summary>
/// Generic interface for type-safe saving and loading
/// </summary>
/// <typeparam name="T">The type of data this object saves</typeparam>
public interface ISaveable<T> : ISaveable
{
    /// <summary>
    /// Captures the current state with type safety
    /// </summary>
    new T CaptureState();
    
    /// <summary>
    /// Restores the state with type safety
    /// </summary>
    void RestoreState(T state);
}

/// <summary>
/// Attribute to mark fields/properties that should be saved
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class SaveableAttribute : Attribute
{
    public string CustomKey { get; set; }
    
    public SaveableAttribute(string customKey = null)
    {
        CustomKey = customKey;
    }
}

/// <summary>
/// Attribute to mark fields/properties that should NOT be saved
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
public class NonSaveableAttribute : Attribute
{
}