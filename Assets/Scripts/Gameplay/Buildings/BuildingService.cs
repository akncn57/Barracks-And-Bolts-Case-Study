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

        public bool TryGet(GameEntityId id, out BuildingModel building)
        {
            return _buildings.TryGetValue(id, out building);
        }
    }
}
