using UnityEngine;

namespace StrataDI.Samples
{
    /// <summary>
    /// Demonstrates DependencyContainer.Create<T>() for a plain C# class.
    /// </summary>
    public sealed class ConstructorInjectionExample :
        MonoBehaviour,
        IInjectionCallback
    {
        [Inject]
        private DependencyContext _context;

        public void OnInjected()
        {
            ClockReader reader =
                _context.Container.Create<ClockReader>();

            Debug.Log(
                $"Constructor injection: {reader.ReadTime():0.00}",
                this);
        }
    }
}
