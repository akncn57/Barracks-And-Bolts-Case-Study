using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Pathfinding;
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

            // One shared instance; VContainer disposes it with the scope.
            builder.Register<GameInputActions>(Lifetime.Singleton);

            RegisterGrid(builder);
            builder.RegisterComponentInHierarchy<CameraController>();
        }

        private static void RegisterGrid(IContainerBuilder builder)
        {
            builder.RegisterComponentInHierarchy<TilemapGridSource>();
            builder.Register(resolver => resolver.Resolve<TilemapGridSource>().BuildGridMap(), Lifetime.Singleton);

            // Build the grid as soon as the scene starts, so a broken tilemap fails here and not on first use.
            builder.RegisterBuildCallback(container => container.Resolve<GridMap>());

            builder.Register<AStarPathfinder>(Lifetime.Singleton);
            builder.Register<GridSearch>(Lifetime.Singleton);
        }
    }
}
