using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Bootstrap
{
    /// <summary>
    /// Scope of the gameplay scene. Everything registered here is destroyed with the scene.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameDatabase gameDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            if (gameDatabase == null)
                throw new InvalidOperationException($"{name}: game database is not assigned.");

            // Scene scoped, so no subscription can outlive the gameplay scene.
            builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();

            builder.Register<GameClock>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SimulationRunner>().AsSelf();

            // One shared instance; VContainer disposes it with the scope.
            builder.Register<GameInputActions>(Lifetime.Singleton);
            builder.RegisterEntryPoint<InputModeController>();

            builder.RegisterInstance(gameDatabase);
            builder.Register<GameEntityIdGenerator>(Lifetime.Singleton);
            builder.Register<TeamService>(Lifetime.Singleton);
            builder.Register<SandboxService>(Lifetime.Singleton);

            RegisterGrid(builder);
            RegisterUnits(builder);
            RegisterBuildings(builder);
            RegisterResourceNodes(builder);
            RegisterEconomy(builder);
            builder.RegisterComponentInHierarchy<CameraController>();
        }

        private void RegisterResourceNodes(IContainerBuilder builder)
        {
            if (gameDatabase.ResourceDistribution == null)
                throw new InvalidOperationException($"{gameDatabase.name}: resource distribution is not assigned.");

            builder.RegisterInstance(gameDatabase.ResourceDistribution);
            builder.Register<ResourceNodeService>(Lifetime.Singleton);
            builder.Register<ResourceDistributor>(Lifetime.Singleton);
            builder.RegisterEntryPoint<ResourceMapInitializer>();
            builder.RegisterComponentInHierarchy<ResourceNodeViewSpawner>();
        }

        private void RegisterEconomy(IContainerBuilder builder)
        {
            if (gameDatabase.NormalMode == null)
                throw new InvalidOperationException($"{gameDatabase.name}: normal mode settings are not assigned.");

            if (gameDatabase.Raids == null)
                throw new InvalidOperationException($"{gameDatabase.name}: raid settings are not assigned.");

            builder.RegisterInstance(gameDatabase.NormalMode);
            builder.RegisterInstance(gameDatabase.Raids);
            builder.Register<ResourceService>(Lifetime.Singleton);
            builder.Register<PopulationService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<NormalModeStarter>();
            builder.Register<ResourceRespawnService>(Lifetime.Singleton);
            builder.Register<GameOutcomeService>(Lifetime.Singleton);
            builder.RegisterSimulationSystem<GatheringSystem>();
            builder.RegisterSimulationSystem<CalendarService>();
            builder.RegisterSimulationSystem<RaidService>();
            builder.RegisterSimulationSystem<BattleAiSystem>();
            builder.RegisterSimulationSystem<ShelterSystem>();
            builder.RegisterSimulationSystem<ResourceRoamSystem>();

            // Nothing asks for these two; they only listen to events, so they are created here.
            builder.RegisterBuildCallback(container =>
            {
                container.Resolve<ResourceRespawnService>();
                container.Resolve<GameOutcomeService>();
            });
        }

        private static void RegisterUnits(IContainerBuilder builder)
        {
            builder.Register<UnitService>(Lifetime.Singleton);
            builder.RegisterSimulationSystem<UnitMovementSystem>();
            builder.RegisterComponentInHierarchy<UnitViewSpawner>();

            builder.Register<CombatService>(Lifetime.Singleton);
            builder.RegisterSimulationSystem<CombatSystem>();
            builder.RegisterSimulationSystem<BuildingDefenseSystem>();
            builder.RegisterSimulationSystem<DeathCleanupSystem>();
            builder.RegisterComponentInHierarchy<ProjectileSpawner>();

            builder.Register<UnitCommandService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<CommandInputHandler>();
            builder.RegisterComponentInHierarchy<MoveMarkerView>();
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

        private static void RegisterBuildings(IContainerBuilder builder)
        {
            builder.Register<BuildingService>(Lifetime.Singleton);
            builder.RegisterComponentInHierarchy<BuildingViewSpawner>();
            builder.Register<UnitProductionService>(Lifetime.Singleton);

            builder.Register<PlacementService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<PlacementInputHandler>();
            builder.RegisterComponentInHierarchy<PlacementPreviewView>();

            builder.Register<SelectionService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<SelectionInputHandler>();
            builder.RegisterComponentInHierarchy<BoxSelectionView>();

            builder.Register<HoverService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<HoverInputHandler>();
            builder.RegisterComponentInHierarchy<HoverHighlightView>();
        }
    }
}
