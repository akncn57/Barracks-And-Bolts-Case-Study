using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>Owns every resource on the map and keeps the grid in sync with them.</summary>
    public sealed class ResourceNodeService
    {
        private readonly GridMap _gridMap;
        private readonly GameEntityIdGenerator _idGenerator;
        private readonly IEventBus _eventBus;
        private readonly List<ResourceNodeModel> _nodes = new();

        public ResourceNodeService(GridMap gridMap, GameEntityIdGenerator idGenerator, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _idGenerator = idGenerator;
            _eventBus = eventBus;
        }

        public IReadOnlyList<ResourceNodeModel> Nodes => _nodes;

        /// <param name="origin">Bottom-left cell of the footprint.</param>
        /// <returns>False if any footprint cell is outside the map, blocked or taken.</returns>
        public bool TryPlace(ResourceNodeDefinition definition, Vector2Int origin, out ResourceNodeModel node)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            var area = new RectInt(origin, definition.Size);
            var id = _idGenerator.Next();
            if (!_gridMap.TryOccupy(area, id))
            {
                node = null;
                return false;
            }

            node = new ResourceNodeModel(id, definition, area);
            _nodes.Add(node);

            _eventBus.Publish(new ResourceNodePlacedEvent(node));
            _eventBus.Publish(new GridChangedEvent(area));
            return true;
        }
    }
}
