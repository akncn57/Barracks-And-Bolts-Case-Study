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

        public bool TryGet(GameEntityId id, out UnitModel unit)
        {
            return _units.TryGetValue(id, out unit);
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
