using UnityEngine;

namespace StrataDI
{
    /// <summary>
    /// Base component for declaring dependency bindings in a context.
    /// </summary>
    public abstract class DependencyInstaller : MonoBehaviour
    {
        public abstract void InstallBindings(DependencyContainer container);
    }
}
