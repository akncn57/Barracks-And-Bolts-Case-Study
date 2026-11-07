using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class AStarPathfinderTests
    {
        private readonly List<Vector2Int> _path = new();

        private static AStarPathfinder OpenField(out GridMap map)
        {
            map = new GridMap(10, 10, Vector2.zero);
            return new AStarPathfinder(map);
        }

        [Test]
        public void FindPath_StartEqualsGoal_FoundWithEmptyPath()
        {
            var pathfinder = OpenField(out _);

            Assert.AreEqual(PathStatus.Found, pathfinder.FindPath(new Vector2Int(3, 3), new Vector2Int(3, 3), _path));
            Assert.IsEmpty(_path);
        }

        [Test]
        public void FindPath_StraightLine_TakesDirectRoute()
        {
            var pathfinder = OpenField(out _);

            pathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 0), _path);

            CollectionAssert.AreEqual(new[] { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) }, _path);
        }

        [Test]
        public void FindPath_OpenField_UsesDiagonalsForShortestPath()
        {
            var pathfinder = OpenField(out var map);
            var start = new Vector2Int(0, 0);

            var status = pathfinder.FindPath(start, new Vector2Int(5, 3), _path);

            Assert.AreEqual(PathStatus.Found, status);
            Assert.AreEqual(5, _path.Count, "With diagonals the step count is max(dx, dy).");
            Assert.AreEqual(new Vector2Int(5, 3), _path[^1]);
            TestMaps.AssertWalkableAndContiguous(map, start, _path);
        }

        [Test]
        public void FindPath_WallInTheWay_GoesThroughTheGap()
        {
            var map = TestMaps.FromRows(
                ".......",
                "...#...",
                "...#...",
                "...#...",
                "...#...");
            var start = new Vector2Int(0, 0);

            var status = new AStarPathfinder(map).FindPath(start, new Vector2Int(6, 0), _path);

            Assert.AreEqual(PathStatus.Found, status);
            CollectionAssert.Contains(_path, new Vector2Int(3, 4));
            TestMaps.AssertWalkableAndContiguous(map, start, _path);
        }

        [Test]
        public void FindPath_DiagonalPastBlockedCorner_IsNotTaken()
        {
            var map = TestMaps.FromRows(
                "...",
                "...",
                ".#.");

            new AStarPathfinder(map).FindPath(new Vector2Int(0, 0), new Vector2Int(1, 1), _path);

            CollectionAssert.AreEqual(new[] { new Vector2Int(0, 1), new Vector2Int(1, 1) }, _path);
        }

        [Test]
        public void FindPath_AroundBuilding_AvoidsOccupiedCells()
        {
            var pathfinder = OpenField(out var map);
            map.TryOccupy(new RectInt(3, 0, 2, 8), new GameEntityId(1));
            var start = new Vector2Int(0, 2);

            var status = pathfinder.FindPath(start, new Vector2Int(8, 2), _path);

            Assert.AreEqual(PathStatus.Found, status);
            TestMaps.AssertWalkableAndContiguous(map, start, _path);
        }

        [Test]
        public void FindPath_GoalIsBuilding_EndsNextToIt()
        {
            var pathfinder = OpenField(out var map);
            map.TryOccupy(new RectInt(5, 5, 2, 2), new GameEntityId(1));
            var start = new Vector2Int(0, 0);
            var goal = new Vector2Int(5, 5);

            var status = pathfinder.FindPath(start, goal, _path);

            Assert.AreEqual(PathStatus.Partial, status);
            var last = _path[^1];
            Assert.AreEqual(1, Mathf.Max(Mathf.Abs(last.x - goal.x), Mathf.Abs(last.y - goal.y)), "Path should stop right next to the goal.");
            TestMaps.AssertWalkableAndContiguous(map, start, _path);
        }

        [Test]
        public void FindPath_GoalWalledOff_EndsAtClosestReachableCell()
        {
            var map = TestMaps.FromRows(
                ".......",
                "..###..",
                "..#.#..",
                "..###..",
                ".......");
            var start = new Vector2Int(0, 2);

            var status = new AStarPathfinder(map).FindPath(start, new Vector2Int(3, 2), _path);

            Assert.AreEqual(PathStatus.Partial, status);
            Assert.AreEqual(new Vector2Int(1, 2), _path[^1]);
        }

        [Test]
        public void FindPath_StartBoxedIn_PartialWithEmptyPath()
        {
            var map = TestMaps.FromRows(
                "###..",
                "#.#..",
                "###..");

            var status = new AStarPathfinder(map).FindPath(new Vector2Int(1, 1), new Vector2Int(4, 1), _path);

            Assert.AreEqual(PathStatus.Partial, status);
            Assert.IsEmpty(_path);
        }

        [Test]
        public void FindPath_StartOnBlockedCell_CanStillLeave()
        {
            var pathfinder = OpenField(out var map);
            map.SetTerrainWalkable(new Vector2Int(2, 2), false);

            var status = pathfinder.FindPath(new Vector2Int(2, 2), new Vector2Int(6, 2), _path);

            Assert.AreEqual(PathStatus.Found, status);
        }

        [Test]
        public void FindPath_StartOutsideMap_NotFound()
        {
            var pathfinder = OpenField(out _);

            Assert.AreEqual(PathStatus.NotFound, pathfinder.FindPath(new Vector2Int(-1, 0), new Vector2Int(5, 5), _path));
            Assert.IsEmpty(_path);
        }

        [Test]
        public void FindPath_GoalOutsideMap_GoesToNearestEdge()
        {
            var pathfinder = OpenField(out _);

            var status = pathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(50, 0), _path);

            Assert.AreEqual(PathStatus.Found, status);
            Assert.AreEqual(new Vector2Int(9, 0), _path[^1]);
        }

        [Test]
        public void FindPath_RepeatedSearches_DoNotAffectEachOther()
        {
            var pathfinder = OpenField(out var map);
            map.TryOccupy(new RectInt(4, 4, 2, 2), new GameEntityId(1));
            pathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(4, 4), _path);

            var status = pathfinder.FindPath(new Vector2Int(9, 9), new Vector2Int(9, 5), _path);

            Assert.AreEqual(PathStatus.Found, status);
            CollectionAssert.AreEqual(new[] { new Vector2Int(9, 8), new Vector2Int(9, 7), new Vector2Int(9, 6), new Vector2Int(9, 5) }, _path);
        }

        [Test]
        public void FindPath_ClearsPreviousContentsOfList()
        {
            var pathfinder = OpenField(out _);
            _path.Add(new Vector2Int(99, 99));

            pathfinder.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 0), _path);

            CollectionAssert.AreEqual(new[] { new Vector2Int(1, 0) }, _path);
        }

        [Test]
        public void FindPath_NullList_Throws()
        {
            var pathfinder = OpenField(out _);

            Assert.Throws<ArgumentNullException>(() => pathfinder.FindPath(Vector2Int.zero, Vector2Int.one, null));
        }
    }
}
