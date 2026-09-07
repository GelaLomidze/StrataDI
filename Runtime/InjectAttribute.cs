using System;

namespace StrataDI
{
    /// <summary>
    /// Marks an instance field or instance method for dependency injection.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Field | AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public sealed class InjectAttribute : Attribute
    {
    }
}
