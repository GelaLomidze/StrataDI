using UnityEngine;

namespace StrataDI.Samples
{
    /// <summary>
    /// Demonstrates private method injection.
    /// </summary>
    public sealed class MethodClockDisplay :
        MonoBehaviour,
        IInjectionCallback
    {
        private IGameClock _gameClock;

        [Inject]
        private void Construct(
            IGameClock gameClock)
        {
            _gameClock = gameClock;
        }

        public void OnInjected()
        {
            Debug.Log(
                $"Method injection: {_gameClock.Time:0.00}",
                this);
        }
    }
}
