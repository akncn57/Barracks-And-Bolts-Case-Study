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

        /// <summary>Takes the node off the map and frees its cells.</summary>
        public void Remove(ResourceNodeModel node)
        {
            if (!_nodes.Remove(node))
                return;

            _gridMap.Release(node.Area, node.Id);
            _eventBus.Publish(new ResourceNodeRemovedEvent(node));
            _eventBus.Publish(new GridChangedEvent(node.Area));
        }

        /// <summary>The node whose visible area covers the point. If several do, the one drawn in front: the lowest on screen.</summary>
        public ResourceNodeModel GetNodeAt(Vector2 worldPosition)
        {
            ResourceNodeModel result = null;
            foreach (var node in _nodes)
            {
                if ((result == null || node.Area.yMin < result.Area.yMin) && GetVisibleArea(node).Contains(worldPosition))
                    result = node;
            }

            return result;
        }

        /// <summary>The nearest node of that kind which still has something left.</summary>
        /// <param name="ignored">Left out of the search. Null means none.</param>
        public ResourceNodeModel FindNearest(ResourceKind kind, Vector2 from, ResourceNodeModel ignored)
        {
            ResourceNodeModel nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var node in _nodes)
            {
                if (node == ignored || node.IsDepleted || node.Definition.Kind != kind)
                    continue;

                var distance = (node.Area.center + _gridMap.Origin - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        /// <summary>The footprint plus what rises above it, in world space.</summary>
        public Rect GetVisibleArea(ResourceNodeModel node)
        {
            var corner = _gridMap.Origin + node.Area.min + node.Offset;
            return new Rect(corner.x, corner.y, node.Area.width, node.Definition.VisualHeight);
        }
    }
}
