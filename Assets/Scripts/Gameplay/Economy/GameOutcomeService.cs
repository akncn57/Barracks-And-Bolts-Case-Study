using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>
    /// Decides when a normal game ends. Lost: the Castle falls, or nobody is left and a new villager can't be paid for.
    /// Won: the last raid is beaten. The game stops when it ends. A sandbox game never ends.
    /// </summary>
    public sealed class GameOutcomeService : IDisposable
    {
        private readonly NormalModeSettings _settings;
        private readonly ResourceService _resources;
        private readonly PopulationService _population;
        private readonly GameClock _clock;
        private readonly IEventBus _eventBus;
        private readonly CompositeDisposable _subscriptions = new();

        public GameOutcomeService(SandboxService sandbox, NormalModeSettings settings, ResourceService resources,
            PopulationService population, GameClock clock, IEventBus eventBus)
        {
            _settings = settings;
            _resources = resources;
            _population = population;
            _clock = clock;
            _eventBus = eventBus;

            if (sandbox.IsEnabled)
                return;

            eventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed).AddTo(_subscriptions);
            eventBus.Subscribe<UnitDiedEvent>(OnUnitDied).AddTo(_subscriptions);
            eventBus.Subscribe<RaidEndedEvent>(OnRaidEnded).AddTo(_subscriptions);
        }

        public bool IsOver { get; private set; }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent destroyedEvent)
        {
            var building = destroyedEvent.Building;
            if (building.Definition == _settings.Castle && building.Faction == Faction.Player)
                End(false, "Your Castle has fallen.");
        }

        private void OnUnitDied(UnitDiedEvent diedEvent)
        {
            if (diedEvent.Unit.Faction == Faction.Player && _population.Used == 0 && !_resources.CanAfford(_settings.Villager.Cost))
                End(false, "Nobody is left, and there is no food for a new villager.");
        }

        private void OnRaidEnded(RaidEndedEvent endedEvent)
        {
            if (endedEvent.IsLastRaid)
                End(true, "You survived every raid.");
        }

        private void End(bool isVictory, string message)
        {
            if (IsOver)
                return;

            IsOver = true;
            _clock.SetPaused(true);
            _eventBus.Publish(new GameEndedEvent(isVictory, message));
        }
    }

    public readonly struct GameEndedEvent : IGameEvent
    {
        public GameEndedEvent(bool isVictory, string message)
        {
            IsVictory = isVictory;
            Message = message;
        }

        public bool IsVictory { get; }
        public string Message { get; }
    }
}
