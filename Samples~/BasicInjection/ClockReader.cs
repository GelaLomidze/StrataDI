namespace StrataDI.Samples
{
    /// <summary>
    /// Plain C# class created through explicit constructor injection.
    /// </summary>
    public sealed class ClockReader
    {
        private readonly IGameClock _gameClock;

        [Inject]
        private ClockReader(
            IGameClock gameClock)
        {
            _gameClock = gameClock;
        }

        public float ReadTime()
        {
            return _gameClock.Time;
        }
    }
}
