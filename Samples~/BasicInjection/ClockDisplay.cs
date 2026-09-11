using UnityEngine;

namespace StrataDI.Samples
{
    /// <summary>
    /// Demonstrates private field injection and the post-injection callback.
    /// </summary>
    public sealed class ClockDisplay :
        MonoBehaviour,
        IInjectionCallback
    {
        [Inject]
        private IGameClock _gameClock;

        public void OnInjected()
        {
            Debug.Log(
                $"Field injection: {_gameClock.Time:0.00}",
                this);
        }
    }
}
