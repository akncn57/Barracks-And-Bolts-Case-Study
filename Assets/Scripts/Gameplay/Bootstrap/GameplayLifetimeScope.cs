using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Grid;
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

            RegisterGrid(builder);
        }

        private static void RegisterGrid(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<TilemapGridSource>();
            builder.Register(resolver => resolver.Resolve<TilemapGridSource>().BuildGridMap(), Lifetime.Singleton);

            // Build the grid as soon as the scene starts, so a broken tilemap fails here and not on first use.
            builder.RegisterBuildCallback(container => container.Resolve<GridMap>());
        }
    }
}
