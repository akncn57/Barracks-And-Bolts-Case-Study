using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Owns every unit in the session. Units are not written into the grid: only buildings and terrain block paths,
    /// so units can never lock each other in. "Is a unit standing here?" is answered from their positions.
    /// </summary>
    public sealed class UnitService
    {
        private readonly GridMap _gridMap;
        private readonly GridSearch _gridSearch;
        private readonly GameEntityIdGenerator _idGenerator;
        private readonly IEventBus _eventBus;
        private readonly Dictionary<GameEntityId, UnitModel> _units = new();
        private readonly Dictionary<GameEntityId, UnitModel> _sheltered = new();

        // Kept in a field so a spawn search doesn't allocate a new delegate.
        private readonly Predicate<Vector2Int> _hasNoUnit;

        public UnitService(GridMap gridMap, GridSearch gridSearch, GameEntityIdGenerator idGenerator, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _gridSearch = gridSearch;
            _idGenerator = idGenerator;
            _eventBus = eventBus;
            _hasNoUnit = cell => !HasUnitAt(cell);
        }

        public int Count => _units.Count;

        /// <summary>Every unit. The concrete type lets a foreach run without allocating.</summary>
        public Dictionary<GameEntityId, UnitModel>.ValueCollection Units => _units.Values;

        /// <summary>Units inside a building. They aren't in <see cref="Units"/>: nothing can see, select or hit them.</summary>
        public Dictionary<GameEntityId, UnitModel>.ValueCollection Sheltered => _sheltered.Values;

        public bool TryGet(GameEntityId id, out UnitModel unit)
        {
            return _units.TryGetValue(id, out unit);
        }

        /// <returns>False if there is no such unit.</returns>
        public bool Remove(GameEntityId id)
        {
            if (!_units.Remove(id, out var unit))
                return false;

            _eventBus.Publish(new UnitDiedEvent(unit));
            return true;
        }

        /// <summary>Takes the unit off the map. It stays alive and keeps its place in the population.</summary>
        public void Shelter(UnitModel unit)
        {
            if (!_units.Remove(unit.Id))
                return;

            unit.Stop();
            unit.ClearAttackTarget();
            _sheltered.Add(unit.Id, unit);
            _eventBus.Publish(new UnitShelteredEvent(unit));
        }

        /// <summary>Puts a sheltered unit back on the map, on the cell or the nearest one without a unit.</summary>
        public void BringOut(UnitModel unit, Vector2Int cell)
        {
            if (!_sheltered.Remove(unit.Id))
                return;

            if (_gridSearch.TryFindNearestWalkable(cell, _hasNoUnit, out var freeCell))
                unit.PlaceAt(_gridMap.CellToWorldCenter(freeCell));

            _units.Add(unit.Id, unit);
            _eventBus.Publish(new UnitLeftShelterEvent(unit));
        }

        public bool HasUnitAt(Vector2Int cell)
        {
            foreach (var unit in _units.Values)
            {
                if (_gridMap.WorldToCell(unit.Position) == cell)
                    return true;
            }

            return false;
        }

        public bool HasUnitIn(RectInt area)
        {
            foreach (var unit in _units.Values)
            {
                if (area.Contains(_gridMap.WorldToCell(unit.Position)))
                    return true;
            }

            return false;
        }

        /// <summary>True if a unit outside <paramref name="ignored"/> stands on the cell or is walking to it.</summary>
        public bool IsDestinationTaken(Vector2Int cell, HashSet<GameEntityId> ignored)
        {
            foreach (var unit in _units.Values)
            {
                if (!ignored.Contains(unit.Id) && _gridMap.WorldToCell(unit.Destination) == cell)
                    return true;
            }

            return false;
        }

        /// <summary>The unit whose body covers the point. If several do, the one drawn in front: the lowest on screen.</summary>
        public GameEntityId GetUnitAt(Vector2 worldPosition)
        {
            var result = GameEntityId.None;
            var lowestFeet = float.MaxValue;
            foreach (var unit in _units.Values)
            {
                if (unit.Position.y < lowestFeet && unit.Body.Contains(worldPosition))
                {
                    lowestFeet = unit.Position.y;
                    result = unit.Id;
                }
            }

            return result;
        }

        /// <summary>Fills <paramref name="results"/> with the faction's units whose body touches the area.</summary>
        public void CollectUnitsIn(Rect area, Faction faction, List<GameEntityId> results)
        {
            results.Clear();
            foreach (var unit in _units.Values)
            {
                if (unit.Faction == faction && unit.Body.Overlaps(area))
                    results.Add(unit.Id);
            }
        }

        /// <summary>Creates a unit on the cell, or on the nearest walkable cell without a unit if that one is taken.</summary>
        /// <returns>False if the whole map is full.</returns>
        public bool TrySpawn(UnitDefinition definition, Vector2Int cell, Faction faction, out UnitModel unit)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (!_gridSearch.TryFindNearestWalkable(cell, _hasNoUnit, out var freeCell))
            {
                unit = null;
                return false;
            }

            unit = new UnitModel(_idGenerator.Next(), definition, faction, _gridMap.CellToWorldCenter(freeCell));
            _units.Add(unit.Id, unit);
            _eventBus.Publish(new UnitSpawnedEvent(unit));
            return true;
        }
    }
}
