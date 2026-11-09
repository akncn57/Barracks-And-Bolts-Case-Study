using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Construction;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Raids
{
    /// <summary>
    /// Villagers keep working during a raid until an enemy comes close. Then they drop the work, run to the Castle
    /// and hide inside. When every enemy is dead they come out and go back to what they were doing.
    /// An order from the player beats all this for a while. Sandbox games don't use it.
    /// </summary>
    public sealed class ShelterSystem : ISimulationSystem, IDisposable
    {
        private const float ScanInterval = 0.25f;

        // How long a villager is left alone after a player's order, or after it couldn't reach the Castle.
        private const float LeftAloneSeconds = 10f;

        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly UnitMovementSystem _movement;
        private readonly GatheringSystem _gathering;
        private readonly ConstructionSystem _construction;
        private readonly GridSearch _search;
        private readonly GridMap _gridMap;
        private readonly RaidSettings _settings;
        private readonly NormalModeSettings _normalMode;
        private readonly IEventBus _eventBus;
        private readonly bool _isEnabled;
        private readonly CompositeDisposable _subscriptions = new();

        private readonly List<UnitModel> _fleeing = new();
        private readonly List<UnitModel> _inside = new();
        private readonly List<Vector2> _enemies = new();
        private readonly Dictionary<GameEntityId, float> _leftAloneUntil = new();
        private float _time;
        private float _scanDelayLeft;

        public ShelterSystem(UnitService units, BuildingService buildings, UnitMovementSystem movement, GatheringSystem gathering, ConstructionSystem construction, GridSearch search,
            GridMap gridMap, RaidSettings settings, NormalModeSettings normalMode, SandboxService sandbox, IEventBus eventBus)
        {
            _units = units;
            _buildings = buildings;
            _movement = movement;
            _gathering = gathering;
            _construction = construction;
            _search = search;
            _gridMap = gridMap;
            _settings = settings;
            _normalMode = normalMode;
            _eventBus = eventBus;
            _isEnabled = !sandbox.IsEnabled;

            if (_isEnabled)
                eventBus.Subscribe<UnitOrderedEvent>(e => OnUnitOrdered(e.UnitId)).AddTo(_subscriptions);
        }

        public SimulationPhase Phase => SimulationPhase.AI;

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void Tick(float deltaTime)
        {
            if (!_isEnabled)
                return;

            _time += deltaTime;
            UpdateFleeing();

            _scanDelayLeft -= deltaTime;
            if (_scanDelayLeft > 0f)
                return;

            _scanDelayLeft = ScanInterval;
            CollectEnemies();
            if (_enemies.Count == 0)
            {
                SendEveryoneBackToWork();
                return;
            }

            var castle = FindCastle();
            if (castle == null)
                return;

            foreach (var unit in _units.Units)
            {
                if (IsInDanger(unit))
                    StartFleeing(unit, castle);
            }
        }

        private void OnUnitOrdered(GameEntityId id)
        {
            _leftAloneUntil[id] = _time + LeftAloneSeconds;
            for (var i = _fleeing.Count - 1; i >= 0; i--)
            {
                if (_fleeing[i].Id != id)
                    continue;

                _fleeing.RemoveAt(i);
                CancelWork(id);
            }
        }

        private void UpdateFleeing()
        {
            // Backwards, because going inside ends the flight.
            for (var i = _fleeing.Count - 1; i >= 0; i--)
            {
                var unit = _fleeing[i];
                if (!_units.TryGet(unit.Id, out _) || unit.IsDead)
                {
                    _fleeing.RemoveAt(i);
                    CancelWork(unit.Id);
                }
                else if (!unit.IsMoving)
                {
                    _fleeing.RemoveAt(i);
                    FinishFlight(unit);
                }
            }
        }

        private void FinishFlight(UnitModel unit)
        {
            var castle = FindCastle();
            if (castle != null && IsBeside(castle.Area, unit.Position))
            {
                GoInside(unit);
                return;
            }

            // Something is in the way. Better back to work than standing around.
            _leftAloneUntil[unit.Id] = _time + LeftAloneSeconds;
            ResumeWork(unit);
        }

        private void GoInside(UnitModel unit)
        {
            if (_inside.Count == 0)
                _eventBus.Publish(new GameNoticeEvent("Villagers are hiding in the Castle", isAlert: false));

            _gathering.DeliverLoad(unit.Id);
            _units.Shelter(unit);
            _inside.Add(unit);
        }

        private void SendEveryoneBackToWork()
        {
            foreach (var unit in _fleeing)
            {
                unit.Stop();
                ResumeWork(unit);
            }

            _fleeing.Clear();
            if (_inside.Count == 0)
                return;

            var castle = FindCastle();
            foreach (var unit in _inside)
            {
                var door = _gridMap.WorldToCell(unit.Position);
                if (castle != null && _search.TryFindCellBeside(castle.Area, unit.Position, out var cell))
                    door = cell;

                _units.BringOut(unit, door);
                ResumeWork(unit);
            }

            _inside.Clear();
        }

        private void CancelWork(GameEntityId unitId)
        {
            _gathering.Cancel(unitId);
            _construction.Cancel(unitId);
        }

        // A villager has at most one of the two jobs suspended.
        private void ResumeWork(UnitModel unit)
        {
            if (!_gathering.Resume(unit))
                _construction.Resume(unit);
        }

        private void CollectEnemies()
        {
            _enemies.Clear();
            foreach (var unit in _units.Units)
            {
                if (unit.Faction == Faction.Enemy && !unit.IsDead)
                    _enemies.Add(unit.Position);
            }
        }

        private bool IsInDanger(UnitModel unit)
        {
            if (unit.Faction != Faction.Player || !unit.Definition.CanGather || unit.IsDead || _fleeing.Contains(unit))
                return false;

            if (_leftAloneUntil.TryGetValue(unit.Id, out var until) && until > _time)
                return false;

            var radius = _settings.ShelterRadius;
            foreach (var enemy in _enemies)
            {
                if ((enemy - unit.Position).sqrMagnitude <= radius * radius)
                    return true;
            }

            return false;
        }

        private void StartFleeing(UnitModel unit, BuildingModel castle)
        {
            if (!_search.TryFindCellBeside(castle.Area, unit.Position, out var door))
                return;

            _gathering.Suspend(unit.Id);
            _construction.Suspend(unit.Id);
            unit.ClearAttackTarget();
            _movement.SendTo(unit, door);
            _fleeing.Add(unit);
        }

        private bool IsBeside(RectInt area, Vector2 position)
        {
            var around = new RectInt(area.x - 1, area.y - 1, area.width + 2, area.height + 2);
            return around.Contains(_gridMap.WorldToCell(position));
        }

        private BuildingModel FindCastle()
        {
            foreach (var building in _buildings.Buildings)
            {
                if (building.Definition == _normalMode.Castle && building.Faction == Faction.Player)
                    return building;
            }

            return null;
        }
    }
}
