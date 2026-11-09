using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using UnityEngine;
using Random = System.Random;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>
    /// Scatters the resources over the map. The same seed always gives the same result.
    /// A node is refused if it would touch the start area, crowd a ramp or cut part of the map off.
    /// </summary>
    public sealed class ResourceDistributor
    {
        private const int DepositAttempts = 200;

        // Leaves holes in a forest or a quarry, so it doesn't look like a solid block.
        private const double ClusterGapChance = 0.25;

        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly GridMap _grid;
        private readonly GridSearch _search;
        private readonly ResourceNodeService _nodes;
        private readonly ResourceDistributionSettings _settings;
        private readonly Queue<Vector2Int> _frontier = new();
        private readonly bool[] _visited;
        private readonly Vector2 _center;

        private Random _random;
        private Vector2Int _start;
        private int _reachableCount;

        public ResourceDistributor(GridMap grid, GridSearch search, ResourceNodeService nodes, ResourceDistributionSettings settings)
        {
            _grid = grid;
            _search = search;
            _nodes = nodes;
            _settings = settings;
            _visited = new bool[grid.Width * grid.Height];
            _center = new Vector2(grid.Width * 0.5f, grid.Height * 0.5f);
        }

        public void Distribute(int seed)
        {
            if (!_search.TryFindNearestWalkable(Vector2Int.FloorToInt(_center), out _start))
                return;

            _reachableCount = CountReachable(default);

            _random = new Random(seed);
            foreach (var group in _settings.Groups)
            {
                if (group.Definitions.Count > 0)
                    PlaceGroup(group, _random);
            }
        }

        /// <summary>
        /// Adds nodes of that kind during the game, next to one of its kind if there is any left.
        /// Follows the same rules as the first distribution. Call <see cref="Distribute"/> first.
        /// </summary>
        public void AddNodes(ResourceKind kind, int count)
        {
            var group = FindGroup(kind);
            if (group == null || !_search.TryFindNearestWalkable(Vector2Int.FloorToInt(_center), out _start))
                return;

            // Buildings have changed the map since the last count.
            _reachableCount = CountReachable(default);
            _random ??= new Random(0);

            for (var i = 0; i < count; i++)
            {
                var definition = group.Definitions[_random.Next(group.Definitions.Count)];
                if (!TryPlaceBesideSameKind(definition, kind) &&
                    TryPickDepositCenter(group, false, new List<Vector2Int>(), _random, out var center))
                    TryPlace(definition, center);
            }
        }

        private ResourceDistributionSettings.Group FindGroup(ResourceKind kind)
        {
            foreach (var group in _settings.Groups)
            {
                if (group.Definitions.Count > 0 && group.Definitions[0].Kind == kind)
                    return group;
            }

            return null;
        }

        private bool TryPlaceBesideSameKind(ResourceNodeDefinition definition, ResourceKind kind)
        {
            var sameKind = new List<ResourceNodeModel>();
            foreach (var node in _nodes.Nodes)
            {
                if (node.Definition.Kind == kind)
                    sameKind.Add(node);
            }

            if (sameKind.Count == 0)
                return false;

            var neighbour = sameKind[_random.Next(sameKind.Count)];
            var from = neighbour.Area.position + new Vector2(_random.Next(-2, 3), _random.Next(-2, 3));
            return _search.TryFindCellBeside(neighbour.Area, from, out var cell) && TryPlace(definition, cell);
        }

        private void PlaceGroup(ResourceDistributionSettings.Group group, Random random)
        {
            var depositCenters = new List<Vector2Int>();
            for (var deposit = 0; deposit < group.DepositCount; deposit++)
            {
                var isStartDeposit = deposit == 0 && group.HasStartDeposit;
                if (!TryPickDepositCenter(group, isStartDeposit, depositCenters, random, out var center))
                    continue;

                depositCenters.Add(center);
                GrowDeposit(group, center, random);
            }
        }

        private bool TryPickDepositCenter(ResourceDistributionSettings.Group group, bool isStartDeposit,
            List<Vector2Int> depositCenters, Random random, out Vector2Int center)
        {
            for (var attempt = 0; attempt < DepositAttempts; attempt++)
            {
                center = new Vector2Int(random.Next(_grid.Width), random.Next(_grid.Height));
                if (isStartDeposit && DistanceToCenter(center) > _settings.StartDepositRadius)
                    continue;

                if (IsFarFromOthers(center, depositCenters, group.MinSpacing) && CanHold(group.Definitions[0], center))
                    return true;
            }

            center = default;
            return false;
        }

        private void GrowDeposit(ResourceDistributionSettings.Group group, Vector2Int center, Random random)
        {
            var target = random.Next(group.MinNodesPerDeposit, group.MaxNodesPerDeposit + 1);
            var placed = 0;
            var open = new List<Vector2Int> { center };
            var seen = new HashSet<Vector2Int> { center };

            while (placed < target && open.Count > 0)
            {
                var index = random.Next(open.Count);
                var cell = open[index];
                open[index] = open[^1];
                open.RemoveAt(open.Count - 1);

                // The first node always goes on the deposit's center.
                var isGap = placed > 0 && random.NextDouble() < ClusterGapChance;
                var definition = group.Definitions[random.Next(group.Definitions.Count)];
                if (!isGap && TryPlace(definition, cell))
                    placed++;

                foreach (var offset in Neighbours)
                {
                    var next = cell + offset;
                    if (_grid.IsWalkable(next) && seen.Add(next))
                        open.Add(next);
                }
            }
        }

        private bool TryPlace(ResourceNodeDefinition definition, Vector2Int origin)
        {
            if (!CanHold(definition, origin))
                return false;

            var area = new RectInt(origin, definition.Size);
            var reachable = CountReachable(area);
            if (reachable != _reachableCount - area.width * area.height)
                return false;

            _reachableCount = reachable;
            return _nodes.TryPlace(definition, origin, out _);
        }

        private bool CanHold(ResourceNodeDefinition definition, Vector2Int origin)
        {
            var area = new RectInt(origin, definition.Size);
            if (!_grid.IsAreaFree(area))
                return false;

            foreach (var cell in area.allPositionsWithin)
            {
                if (DistanceToCenter(cell) < _settings.StartClearRadius)
                    return false;
            }

            return !IsNearRamp(area);
        }

        private bool IsNearRamp(RectInt area)
        {
            var clearance = _settings.RampClearance;
            for (var x = area.xMin - clearance; x < area.xMax + clearance; x++)
            {
                for (var y = area.yMin - clearance; y < area.yMax + clearance; y++)
                {
                    if (_grid.IsRamp(new Vector2Int(x, y)))
                        return true;
                }
            }

            return false;
        }

        private float DistanceToCenter(Vector2Int cell)
        {
            return Vector2.Distance(cell + Vector2.one * 0.5f, _center);
        }

        private static bool IsFarFromOthers(Vector2Int cell, List<Vector2Int> others, float minDistance)
        {
            foreach (var other in others)
            {
                if (Vector2Int.Distance(cell, other) < minDistance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Walkable cells a unit at the start can reach if <paramref name="excluded"/> were taken.
        /// Four directions are enough: A* allows a diagonal step only when both side cells are free.
        /// </summary>
        private int CountReachable(RectInt excluded)
        {
            if (excluded.Contains(_start))
                return -1;

            Array.Clear(_visited, 0, _visited.Length);
            _frontier.Clear();
            _frontier.Enqueue(_start);
            _visited[ToIndex(_start)] = true;

            var count = 0;
            while (_frontier.Count > 0)
            {
                var cell = _frontier.Dequeue();
                count++;

                foreach (var offset in Neighbours)
                {
                    var next = cell + offset;
                    if (!_grid.IsWalkable(next) || excluded.Contains(next) || _visited[ToIndex(next)] || !_grid.CanStepBetween(cell, next))
                        continue;

                    _visited[ToIndex(next)] = true;
                    _frontier.Enqueue(next);
                }
            }

            return count;
        }

        private int ToIndex(Vector2Int cell) => cell.y * _grid.Width + cell.x;
    }
}
