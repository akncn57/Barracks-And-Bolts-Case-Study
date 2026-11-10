using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>
    /// Every morning each of the player's units eats. What can't be paid becomes a debt; while there is one, every unit
    /// slowly loses health. Food that comes in pays the debt first, and the starving stops. How much they eat and how
    /// fast they starve depends on the difficulty. Sandbox games have no hunger.
    /// </summary>
    public sealed class HungerSystem : ISimulationSystem, IDisposable
    {
        private readonly PopulationService _population;
        private readonly ResourceService _resources;
        private readonly UnitService _units;
        private readonly CombatService _combat;
        private readonly IEventBus _eventBus;
        private readonly NormalModeSettings.HungerLevel _level;
        private readonly bool _isEnabled;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<GameEntityId> _starving = new();
        private float _sinceLastHpLost;

        public HungerSystem(PopulationService population, ResourceService resources, UnitService units, CombatService combat,
            NormalModeSettings settings, GameSessionSettings session, SandboxService sandbox, IEventBus eventBus)
        {
            _population = population;
            _resources = resources;
            _units = units;
            _combat = combat;
            _eventBus = eventBus;
            _level = settings.GetHunger(session.Difficulty);
            _isEnabled = !sandbox.IsEnabled;

            if (_isEnabled)
                eventBus.Subscribe<DayStartedEvent>(e => OnDayStarted(e.Day)).AddTo(_subscriptions);
        }

        public SimulationPhase Phase => SimulationPhase.Time;

        /// <summary>Food eaten but not yet paid for.</summary>
        public int FoodOwed { get; private set; }

        public bool IsStarving => FoodOwed > 0;

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void Tick(float deltaTime)
        {
            if (!IsStarving)
                return;

            // Food that came in since goes to the debt first.
            Pay();
            if (!IsStarving)
                return;

            _sinceLastHpLost += deltaTime;
            while (_sinceLastHpLost >= _level.SecondsPerHpLost)
            {
                _sinceLastHpLost -= _level.SecondsPerHpLost;
                Starve();
            }
        }

        private void OnDayStarted(int day)
        {
            // The first morning is the start of the game; nobody has eaten yet.
            if (day <= 1)
                return;

            FoodOwed += _population.Used * _level.FoodPerUnitPerDay;
            Pay();
        }

        private void Pay()
        {
            var wasStarving = IsStarving;
            FoodOwed -= _resources.TakeUpTo(ResourceKind.Food, FoodOwed);
            if (wasStarving == IsStarving)
                return;

            _sinceLastHpLost = 0f;
            _eventBus.Publish(new HungerChangedEvent());
            _eventBus.Publish(IsStarving
                ? new GameNoticeEvent("Not enough food! Your people are starving", isAlert: true)
                : new GameNoticeEvent("There is food again", isAlert: false));
        }

        private void Starve()
        {
            // Copied first: damage doesn't remove anyone yet, but the list is read while events go out.
            _starving.Clear();
            foreach (var unit in _units.Units)
            {
                if (unit.Faction == Faction.Player && !unit.IsDead)
                    _starving.Add(unit.Id);
            }

            // No attacker, so nobody turns around looking for one.
            foreach (var id in _starving)
                _combat.ApplyDamage(id, 1, GameEntityId.None);
        }
    }

    /// <summary>Starving began or ended. Read it from <see cref="HungerSystem"/>.</summary>
    public readonly struct HungerChangedEvent : IGameEvent
    {
    }
}
