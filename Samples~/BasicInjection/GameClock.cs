using UnityEngine;

namespace StrataDI.Samples
{
    public sealed class GameClock : MonoBehaviour, IGameClock
    {
        public float Time => UnityEngine.Time.time;
    }
}
