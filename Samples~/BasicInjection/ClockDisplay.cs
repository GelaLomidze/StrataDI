using UnityEngine;

namespace StrataDI.Samples
{
    public sealed class ClockDisplay : MonoBehaviour, IInjectionCallback
    {
        [Inject] private IGameClock _gameClock;

        public void OnInjected()
        {
            Debug.Log($"StrataDI injected the clock at time {_gameClock.Time:0.00}.", this);
        }
    }
}
