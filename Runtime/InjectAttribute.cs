using System;

namespace StrataDI
{
    /// <summary>
    /// Marks a constructor, instance field, property, or method
    /// for dependency injection.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Constructor |
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
