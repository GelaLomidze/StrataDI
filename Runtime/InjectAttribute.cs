using System;

namespace StrataDI
{
    /// <summary>
    /// Marks an instance field, property, or method for dependency injection.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field |
        AttributeTargets.Property |
        AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
