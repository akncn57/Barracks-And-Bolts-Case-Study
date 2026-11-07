using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Bootstrap
{
    /// <summary>
    /// Scope of the gameplay scene. Everything registered here is destroyed with the scene.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Scene scoped, so no subscription can outlive the gameplay scene.
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();

            builder.Register<GameClock>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SimulationRunner>().AsSelf();
        }
    }
}
