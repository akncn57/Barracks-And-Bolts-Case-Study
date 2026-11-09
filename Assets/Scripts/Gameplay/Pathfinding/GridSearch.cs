using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Grid;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Pathfinding
{
    /// <summary>
    /// Nearest-cell queries on the <see cref="GridMap"/>: finding a free cell near a target
    /// and giving every unit of a group its own destination around a target.
    /// </summary>
    public sealed class GridSearch
    {
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly GridMap _grid;
        private readonly Queue<Vector2Int> _frontier = new();
        private readonly int[] _visitedStamp;
        private int _searchId;

        public GridSearch(GridMap grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));
            _visitedStamp = new int[grid.Width * grid.Height];
        }

        /// <summary>
        /// Finds the walkable cell closest to <paramref name="target"/>, searching in growing square rings.
        /// Returns the target itself if it is walkable. Used for blocked spawn points and clicks on buildings.
        /// </summary>
        public bool TryFindNearestWalkable(Vector2Int target, out Vector2Int result)
        {
            return TryFindNearestWalkable(target, null, out result);
        }

        /// <param name="isAllowed">Extra condition a walkable cell must meet, e.g. no unit standing on it. Null means none.</param>
        public bool TryFindNearestWalkable(Vector2Int target, Predicate<Vector2Int> isAllowed, out Vector2Int result)
        {
            var maxRadius = Mathf.Max(_grid.Width, _grid.Height);
            for (var radius = 0; radius <= maxRadius; radius++)
            {
                // Every cell of a ring is at the same "ring distance", so pick the one with the shortest real distance.
                var bestDistance = float.MaxValue;
                result = default;

                for (var x = target.x - radius; x <= target.x + radius; x++)
                {
                    for (var y = target.y - radius; y <= target.y + radius; y++)
                    {
                        var isOnRing = Mathf.Abs(x - target.x) == radius || Mathf.Abs(y - target.y) == radius;
                        var cell = new Vector2Int(x, y);
                        if (!isOnRing || !_grid.IsWalkable(cell) || (isAllowed != null && !isAllowed(cell)))
                            continue;

                        var distance = (cell - target).sqrMagnitude;
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            result = cell;
                        }
                    }
                }

                if (bestDistance < float.MaxValue)
                    return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Finds the walkable cell touching the area that is closest to the point, on the same level as the area.
        /// Used to stand next to something that fills its own cells.
        /// </summary>
        /// <returns>False if the area is walled in on all sides.</returns>
        public bool TryFindCellBeside(RectInt area, Vector2 from, out Vector2Int result)
        {
            var level = _grid.GetLevel(area.position);
            var bestDistance = float.MaxValue;
            result = default;

            for (var x = area.xMin - 1; x <= area.xMax; x++)
            {
                for (var y = area.yMin - 1; y <= area.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (area.Contains(cell) || !_grid.IsWalkable(cell) || _grid.GetLevel(cell) != level)
                        continue;

                    var distance = (_grid.CellToWorldCenter(cell) - from).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        result = cell;
                    }
                }
            }

            return bestDistance < float.MaxValue;
        }

        /// <summary>
        /// Picks up to <paramref name="count"/> different walkable cells around the target, nearest first.
        /// Cells are collected by flooding outward through walkable cells only, so a group never gets split
        /// across a lake, a wall or a cliff. Returns fewer cells if the reachable area is smaller than the group.
        /// </summary>
        public void CollectDestinations(Vector2Int target, int count, List<Vector2Int> results)
        {
            CollectDestinations(target, count, null, results);
        }

        /// <param name="isAllowed">
        /// Extra condition a destination must meet, e.g. no other unit going there. Null means none.
        /// A refused cell still lets the search pass through it, so the group spreads around it.
        /// </param>
        public void CollectDestinations(Vector2Int target, int count, Predicate<Vector2Int> isAllowed, List<Vector2Int> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            results.Clear();
            if (count <= 0 || !TryFindNearestWalkable(target, out var seed))
                return;

            BeginSearch();
            _frontier.Enqueue(seed);
            _visitedStamp[ToIndex(seed)] = _searchId;

            while (_frontier.Count > 0 && results.Count < count)
            {
                var cell = _frontier.Dequeue();
                if (isAllowed == null || isAllowed(cell))
                    results.Add(cell);

                foreach (var offset in Neighbours)
                {
                    var next = cell + offset;
                    if (!_grid.IsWalkable(next) || !_grid.CanStepBetween(cell, next) || _visitedStamp[ToIndex(next)] == _searchId)
                        continue;

                    _visitedStamp[ToIndex(next)] = _searchId;
                    _frontier.Enqueue(next);
                }
            }
        }

        private void BeginSearch()
        {
            _frontier.Clear();
            _searchId++;

            if (_searchId == int.MaxValue)
            {
                Array.Clear(_visitedStamp, 0, _visitedStamp.Length);
                _searchId = 1;
            }
        }

        private int ToIndex(Vector2Int cell) => cell.y * _grid.Width + cell.x;
    }
}
