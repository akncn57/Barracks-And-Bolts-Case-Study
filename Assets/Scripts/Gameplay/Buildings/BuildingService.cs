using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>Owns every building in the session and keeps the grid in sync with them.</summary>
    public sealed class BuildingService
    {
        private readonly GridMap _gridMap;
        private readonly GameEntityIdGenerator _idGenerator;
        private readonly IEventBus _eventBus;
        private readonly Dictionary<GameEntityId, BuildingModel> _buildings = new();

        public BuildingService(GridMap gridMap, GameEntityIdGenerator idGenerator, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _idGenerator = idGenerator;
            _eventBus = eventBus;
        }

        public int Count => _buildings.Count;

        /// <summary>Every building. The concrete type lets a foreach run without allocating.</summary>
        public Dictionary<GameEntityId, BuildingModel>.ValueCollection Buildings => _buildings.Values;

        /// <param name="origin">Bottom-left cell of the footprint.</param>
        /// <returns>False if any footprint cell is outside the map, blocked or taken.</returns>
        public bool TryPlace(BuildingDefinition definition, Vector2Int origin, Faction faction, out BuildingModel building)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var area = new RectInt(origin, definition.Size);
            var id = _idGenerator.Next();
            if (!_gridMap.TryOccupy(area, id))
            {
                // The unused id is simply skipped; ids only need to be unique, not continuous.
                building = null;
                return false;
            }

            building = new BuildingModel(id, definition, faction, area);
            _buildings.Add(id, building);

            _eventBus.Publish(new BuildingPlacedEvent(building));
            _eventBus.Publish(new GridChangedEvent(area));
            return true;
        }

        /// <summary>Takes the building off the map and frees its cells.</summary>
        /// <returns>False if there is no such building.</returns>
        public bool Remove(GameEntityId id)
        {
            if (!_buildings.Remove(id, out var building))
                return false;

            _gridMap.Release(building.Area, id);
            _eventBus.Publish(new BuildingDestroyedEvent(building));
            _eventBus.Publish(new GridChangedEvent(building.Area));
            return true;
        }

        /// <summary>The building whose visible area covers the point. If several do, the one drawn in front: the lowest on screen.</summary>
        public GameEntityId GetBuildingAt(Vector2 worldPosition)
        {
            var result = GameEntityId.None;
            var lowestBase = int.MaxValue;
            foreach (var building in _buildings.Values)
            {
                if (building.Area.yMin < lowestBase && GetVisibleArea(building).Contains(worldPosition))
                {
                    lowestBase = building.Area.yMin;
                    result = building.Id;
                }
            }

            return result;
        }

        /// <summary>The footprint plus the roof above it, in world space. Roofs are taller than the cells they stand on.</summary>
        public Rect GetVisibleArea(BuildingModel building)
        {
            var area = building.Area;
            var corner = _gridMap.Origin + area.min;
            return new Rect(corner.x, corner.y, area.width, building.Definition.VisualHeight);
        }

        public bool TryGet(GameEntityId id, out BuildingModel building)
        {
            return _buildings.TryGetValue(id, out building);
        }
    }
}
