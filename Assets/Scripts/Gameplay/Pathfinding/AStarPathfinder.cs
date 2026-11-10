using System;
using System.Collections.Generic;
using System.Diagnostics;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Utils.Collections;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Pathfinding
{
    /// <summary>
    /// A* on the <see cref="GridMap"/> with 8-directional movement.
    /// Search buffers are allocated once and reused, so a search doesn't create garbage.
    /// </summary>
    public sealed class AStarPathfinder
    {
        private const float StraightCost = 1f;
        private const float DiagonalCost = 1.41421356f;

        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1)
        };

        private readonly GridMap _grid;
        private readonly MinHeap<int> _open;
        private readonly float[] _costFromStart;
        private readonly int[] _cameFrom;

        // Instead of clearing the arrays before every search, each search gets a new id.
        // A cell's data is valid only if its stamp equals the current search id.
        private readonly int[] _visitedStamp;
        private readonly int[] _closedStamp;
        private int _searchId;

        // Timing for the sandbox stats, in stopwatch ticks. Reading the clock twice per search costs next to nothing.
        private int _timedSearches;
        private long _timedTicks;
        private long _longestTicks;

        public AStarPathfinder(GridMap grid)
        {
            _grid = grid ?? throw new ArgumentNullException(nameof(grid));

            var cellCount = grid.Width * grid.Height;
            _open = new MinHeap<int>(cellCount / 4);
            _costFromStart = new float[cellCount];
            _cameFrom = new int[cellCount];
            _visitedStamp = new int[cellCount];
            _closedStamp = new int[cellCount];
        }

        /// <summary>
        /// Fills <paramref name="path"/> with the cells to walk, excluding the start and including the last cell.
        /// The start cell itself doesn't have to be walkable, so a unit standing on a blocked cell can still leave it.
        /// </summary>
        public PathStatus FindPath(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            var startedAt = Stopwatch.GetTimestamp();
            var status = Search(start, goal, path);
            var ticks = Stopwatch.GetTimestamp() - startedAt;
            _timedSearches++;
            _timedTicks += ticks;
            _longestTicks = Math.Max(_longestTicks, ticks);
            return status;
        }

        /// <summary>How many searches ran since the last call, their total and longest time. Starts counting again.</summary>
        public void TakeStats(out int searches, out double totalMilliseconds, out double longestMilliseconds)
        {
            var ticksToMilliseconds = 1000.0 / Stopwatch.Frequency;
            searches = _timedSearches;
            totalMilliseconds = _timedTicks * ticksToMilliseconds;
            longestMilliseconds = _longestTicks * ticksToMilliseconds;
            _timedSearches = 0;
            _timedTicks = 0;
            _longestTicks = 0;
        }

        private PathStatus Search(Vector2Int start, Vector2Int goal, List<Vector2Int> path)
        {
            path.Clear();
            if (!_grid.IsInBounds(start))
                return PathStatus.NotFound;

            // A click outside the map means "go as far as you can in that direction".
            goal = ClampToGrid(goal);
            if (start == goal)
                return PathStatus.Found;

            BeginSearch();
            var startIndex = ToIndex(start);
            var goalIndex = ToIndex(goal);
            var startHeuristic = Heuristic(start, goal);
            Visit(startIndex, 0f, startIndex);
            _open.Push(startIndex, startHeuristic, startHeuristic);

            // Tracks the cell nearest to the goal, used when the goal turns out to be unreachable.
            var closestIndex = startIndex;
            var closestHeuristic = startHeuristic;

            while (_open.Count > 0)
            {
                var currentIndex = _open.Pop();

                // The heap may hold older, more expensive entries of a cell that is already done.
                if (_closedStamp[currentIndex] == _searchId)
                    continue;

                _closedStamp[currentIndex] = _searchId;
                if (currentIndex == goalIndex)
                {
                    BuildPath(startIndex, goalIndex, path);
                    return PathStatus.Found;
                }

                var current = ToCell(currentIndex);
                var heuristic = Heuristic(current, goal);
                if (heuristic < closestHeuristic)
                {
                    closestHeuristic = heuristic;
                    closestIndex = currentIndex;
                }

                ExpandNeighbours(current, currentIndex, goal);
            }

            BuildPath(startIndex, closestIndex, path);
            return PathStatus.Partial;
        }

        private void ExpandNeighbours(Vector2Int current, int currentIndex, Vector2Int goal)
        {
            foreach (var direction in Directions)
            {
                var next = current + direction;
                if (!CanStep(current, next))
                    continue;

                var isDiagonal = direction.x != 0 && direction.y != 0;

                // No corner cutting: a diagonal step needs both side cells free,
                // otherwise units would clip through the corner of a building or a plateau.
                if (isDiagonal &&
                    (!CanStep(current, new Vector2Int(next.x, current.y)) || !CanStep(current, new Vector2Int(current.x, next.y))))
                    continue;

                var nextIndex = ToIndex(next);
                if (_closedStamp[nextIndex] == _searchId)
                    continue;

                var cost = _costFromStart[currentIndex] + (isDiagonal ? DiagonalCost : StraightCost);
                if (_visitedStamp[nextIndex] == _searchId && cost >= _costFromStart[nextIndex])
                    continue;

                Visit(nextIndex, cost, currentIndex);

                // Ties are broken by the heuristic, so the search prefers cells closer to the goal and expands less.
                var heuristic = Heuristic(next, goal);
                _open.Push(nextIndex, cost + heuristic, heuristic);
            }
        }

        private bool CanStep(Vector2Int from, Vector2Int to)
        {
            return _grid.IsWalkable(to) && _grid.CanStepBetween(from, to);
        }

        private void BeginSearch()
        {
            _open.Clear();
            _searchId++;

            // After int.MaxValue searches the counter would wrap and old stamps could look valid again.
            if (_searchId == int.MaxValue)
            {
                Array.Clear(_visitedStamp, 0, _visitedStamp.Length);
                Array.Clear(_closedStamp, 0, _closedStamp.Length);
                _searchId = 1;
            }
        }

        private void Visit(int index, float cost, int cameFrom)
        {
            _costFromStart[index] = cost;
            _cameFrom[index] = cameFrom;
            _visitedStamp[index] = _searchId;
        }

        private void BuildPath(int startIndex, int endIndex, List<Vector2Int> path)
        {
            for (var index = endIndex; index != startIndex; index = _cameFrom[index])
                path.Add(ToCell(index));

            path.Reverse();
        }

        /// <summary>Octile distance: the exact cost on an empty 8-directional grid, so A* stays optimal.</summary>
        private static float Heuristic(Vector2Int from, Vector2Int to)
        {
            var dx = Mathf.Abs(from.x - to.x);
            var dy = Mathf.Abs(from.y - to.y);
            return StraightCost * (dx + dy) + (DiagonalCost - 2f * StraightCost) * Mathf.Min(dx, dy);
        }

        private Vector2Int ClampToGrid(Vector2Int cell)
        {
            return new Vector2Int(Mathf.Clamp(cell.x, 0, _grid.Width - 1), Mathf.Clamp(cell.y, 0, _grid.Height - 1));
        }

        private int ToIndex(Vector2Int cell) => cell.y * _grid.Width + cell.x;
        private Vector2Int ToCell(int index) => new(index % _grid.Width, index / _grid.Width);
    }
}
