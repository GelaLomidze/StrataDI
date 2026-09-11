using UnityEngine;

namespace StrataDI.Samples
{
    public sealed class GameInstaller : DependencyInstaller
    {
        [SerializeField]
        private GameClock _gameClock;

        public override void InstallBindings(
            DependencyContainer container)
        {
            container.Bind<IGameClock>(_gameClock);
        }
    }
}
