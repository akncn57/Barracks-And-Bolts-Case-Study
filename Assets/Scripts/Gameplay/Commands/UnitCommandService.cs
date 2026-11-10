using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Construction;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>Turns the player's orders into unit actions: attack an enemy, build a site, gather a resource, or move.</summary>
    public sealed class UnitCommandService
    {
        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly CombatService _combat;
        private readonly GridSearch _gridSearch;
        private readonly GridMap _gridMap;
        private readonly GatheringSystem _gathering;
        private readonly ConstructionSystem _construction;
        private readonly FarmingSystem _farming;
        private readonly BuildingService _buildings;
        private readonly ResourceNodeService _nodes;
        private readonly IEventBus _eventBus;

        private readonly List<UnitModel> _group = new();
        private readonly HashSet<GameEntityId> _groupIds = new();
        private readonly List<Vector2Int> _destinations = new();
        private readonly List<GameEntityId> _nonWorkers = new();

        // Kept in a field so a command doesn't allocate a new delegate.
        private readonly Predicate<Vector2Int> _isDestinationFree;

        public UnitCommandService(UnitService units, UnitMovementSystem movement, CombatService combat, GridSearch gridSearch, GridMap gridMap,
            GatheringSystem gathering, ConstructionSystem construction, FarmingSystem farming, BuildingService buildings, ResourceNodeService nodes,
            IEventBus eventBus)
        {
            _units = units;
            _movement = movement;
            _combat = combat;
            _gridSearch = gridSearch;
            _gridMap = gridMap;
            _gathering = gathering;
            _construction = construction;
            _farming = farming;
            _buildings = buildings;
            _nodes = nodes;
            _eventBus = eventBus;
            _isDestinationFree = cell => !_units.IsDestinationTaken(cell, _groupIds);
        }

        /// <summary>The right click order: attack the enemy under the point, build the player's site under it, gather the resource under it, or move there.</summary>
        /// <returns>False if the list holds no unit that can take the order.</returns>
        public bool CommandAt(IReadOnlyList<GameEntityId> ids, Vector2 worldPosition)
        {
            var enemy = _combat.GetHostileAt(worldPosition, Faction.Player);
            if (enemy.IsValid)
                return Attack(ids, enemy, worldPosition);

            if (_buildings.TryGet(_buildings.GetBuildingAt(worldPosition), out var building) && building.Faction == Faction.Player)
            {
                if (!building.IsBuilt)
                    return Build(ids, building, worldPosition);

                if (FarmingSystem.IsFarm(building))
                    return Farm(ids, building, worldPosition);
            }

            var node = _nodes.GetNodeAt(worldPosition);
            return node != null ? Gather(ids, node, worldPosition) : MoveTo(ids, worldPosition);
        }

        /// <summary>Villagers among the ids start gathering the node. Everyone else just walks there.</summary>
        /// <returns>False if the list holds no unit that can take the order.</returns>
        public bool Gather(IReadOnlyList<GameEntityId> ids, ResourceNodeModel node, Vector2 worldPosition)
        {
            CollectGroup(ids);
            AnnounceOrder();
            _nonWorkers.Clear();
            var hasGatherer = false;
            foreach (var unit in _group)
            {
                _construction.Cancel(unit.Id);
                _farming.Cancel(unit.Id);
                if (_gathering.TryOrder(unit, node))
                    hasGatherer = true;
                else
                    _nonWorkers.Add(unit.Id);
            }

            if (_nonWorkers.Count > 0)
                return MoveTo(_nonWorkers, worldPosition) || hasGatherer;

            if (hasGatherer)
                _eventBus.Publish(new MoveCommandIssuedEvent(worldPosition));

            return hasGatherer;
        }

        /// <summary>Villagers among the ids go to work on the site. Everyone else walks up to it.</summary>
        /// <returns>False if the list holds no unit that can take the order.</returns>
        public bool Build(IReadOnlyList<GameEntityId> ids, BuildingModel site, Vector2 worldPosition)
        {
            CollectGroup(ids);
            AnnounceOrder();
            _nonWorkers.Clear();
            var hasBuilder = false;
            foreach (var unit in _group)
            {
                _gathering.Cancel(unit.Id);
                _farming.Cancel(unit.Id);
                if (_construction.TryOrder(unit, site))
                    hasBuilder = true;
                else
                    _nonWorkers.Add(unit.Id);
            }

            if (_nonWorkers.Count > 0)
                return MoveTo(_nonWorkers, worldPosition) || hasBuilder;

            if (hasBuilder)
                _eventBus.Publish(new MoveCommandIssuedEvent(worldPosition));

            return hasBuilder;
        }

        /// <summary>Villagers among the ids work the farm while it has room. Everyone else walks up to it.</summary>
        /// <returns>False if the list holds no unit that can take the order.</returns>
        public bool Farm(IReadOnlyList<GameEntityId> ids, BuildingModel farm, Vector2 worldPosition)
        {
            CollectGroup(ids);
            AnnounceOrder();
            _nonWorkers.Clear();
            var hasFarmer = false;
            var turnedAway = false;
            foreach (var unit in _group)
            {
                _gathering.Cancel(unit.Id);
                _construction.Cancel(unit.Id);
                if (_farming.TryOrder(unit, farm))
                {
                    hasFarmer = true;
                    continue;
                }

                turnedAway |= unit.Definition.CanGather;
                _nonWorkers.Add(unit.Id);
            }

            if (turnedAway)
                _eventBus.Publish(new FloatingTextRequestedEvent("The farm has all the workers it can take", worldPosition, FloatingTextKind.Warning));

            if (_nonWorkers.Count > 0)
                return MoveTo(_nonWorkers, worldPosition) || hasFarmer;

            if (hasFarmer)
                _eventBus.Publish(new MoveCommandIssuedEvent(worldPosition));

            return hasFarmer;
        }

        /// <summary>Orders the player's units among the ids to attack the target. There is no friendly fire.</summary>
        /// <returns>False if the target isn't an enemy or no unit can take the order.</returns>
        public bool Attack(IReadOnlyList<GameEntityId> ids, GameEntityId targetId, Vector2 markerPosition)
        {
            if (!_combat.TryGetTarget(targetId, markerPosition, out var target) || target.Faction == Faction.Player)
                return false;

            CollectGroup(ids);
            if (_group.Count == 0)
                return false;

            AnnounceOrder();
            foreach (var unit in _group)
            {
                CancelJobs(unit.Id);
                unit.SetAttackTarget(targetId);
            }

            _eventBus.Publish(new AttackCommandIssuedEvent(markerPosition));
            return true;
        }

        /// <summary>
        /// Sends the player's units among the ids to the point, each to its own cell around it.
        /// Buildings and enemy units in the list are ignored.
        /// </summary>
        /// <returns>False if the list holds no unit that can take the order.</returns>
        public bool MoveTo(IReadOnlyList<GameEntityId> ids, Vector2 worldPosition)
        {
            CollectGroup(ids);
            if (_group.Count == 0)
                return false;

            var target = ClampToMap(_gridMap.WorldToCell(worldPosition));
            _gridSearch.CollectDestinations(target, _group.Count, _isDestinationFree, _destinations);

            // Every free cell is taken by other units: sharing a cell beats refusing the order.
            if (_destinations.Count == 0)
                _gridSearch.CollectDestinations(target, _group.Count, null, _destinations);

            if (_destinations.Count == 0)
                return false;

            AnnounceOrder();
            foreach (var unit in _group)
                CancelJobs(unit.Id);

            AssignDestinations();
            _eventBus.Publish(new MoveCommandIssuedEvent(worldPosition));
            return true;
        }

        // A click past the edge means "go as far as you can in that direction".
        private Vector2Int ClampToMap(Vector2Int cell)
        {
            return new Vector2Int(Mathf.Clamp(cell.x, 0, _gridMap.Width - 1), Mathf.Clamp(cell.y, 0, _gridMap.Height - 1));
        }

        private void CollectGroup(IReadOnlyList<GameEntityId> ids)
        {
            _group.Clear();
            _groupIds.Clear();
            foreach (var id in ids)
            {
                if (!_units.TryGet(id, out var unit) || unit.Faction != Faction.Player)
                    continue;

                _group.Add(unit);
                _groupIds.Add(id);
            }
        }

        private void CancelJobs(GameEntityId unitId)
        {
            _gathering.Cancel(unitId);
            _construction.Cancel(unitId);
            _farming.Cancel(unitId);
        }

        private void AnnounceOrder()
        {
            foreach (var unit in _group)
                _eventBus.Publish(new UnitOrderedEvent(unit.Id));
        }

        private void AssignDestinations()
        {
            // Destinations come nearest to the target first. Giving each to the closest unit left
            // keeps units from crossing through each other more than needed.
            var index = 0;
            while (_group.Count > 0)
            {
                var destination = _destinations[index % _destinations.Count];
                index++;

                var closest = FindClosestInGroup(_gridMap.CellToWorldCenter(destination));
                _group[closest].ClearAttackTarget();
                _movement.SendTo(_group[closest], destination);

                _group[closest] = _group[_group.Count - 1];
                _group.RemoveAt(_group.Count - 1);
            }
        }

        private int FindClosestInGroup(Vector2 point)
        {
            var closest = 0;
            var closestDistance = float.MaxValue;
            for (var i = 0; i < _group.Count; i++)
            {
                var distance = (_group[i].Position - point).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = i;
                }
            }

            return closest;
        }
    }

    /// <summary>The player gave this unit an order.</summary>
    public readonly struct UnitOrderedEvent : IGameEvent
    {
        public UnitOrderedEvent(GameEntityId unitId)
        {
            UnitId = unitId;
        }

        public GameEntityId UnitId { get; }
    }
}
