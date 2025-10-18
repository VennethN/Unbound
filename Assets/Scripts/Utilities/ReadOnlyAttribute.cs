using UnityEngine;

namespace Unbound.Utility
{
    /// <summary>
    /// Marks a serialized field as read-only in the inspector while still displaying its value.
    /// </summary>
    public sealed class ReadOnlyAttribute : PropertyAttribute
    {
    }
}

