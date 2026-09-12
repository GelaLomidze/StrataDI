using System;
using UnityEngine.Scripting;

namespace StrataDI
{
    /// <summary>
    /// Marks a constructor, instance field, property, or method
    /// for dependency injection and preserves it from managed code stripping.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Constructor |
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class InjectAttribute : PreserveAttribute
    {
    }
}
