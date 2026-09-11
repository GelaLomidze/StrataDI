using UnityEngine;

namespace StrataDI.Samples
{
    /// <summary>
    /// Demonstrates property injection with a private setter.
    /// </summary>
    public sealed class PropertyClockDisplay :
        MonoBehaviour,
        IInjectionCallback
    {
        [Inject]
        public IGameClock GameClock
        {
            get;
            private set;
        }

        public void OnInjected()
        {
            Debug.Log(
                $"Property injection: {GameClock.Time:0.00}",
                this);
        }
    }
}
