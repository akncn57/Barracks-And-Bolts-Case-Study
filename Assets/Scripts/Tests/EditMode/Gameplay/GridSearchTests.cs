using System;
using System.Collections.Generic;
using System.Linq;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class GridSearchTests
    {
        private readonly List<Vector2Int> _results = new();

        [Test]
        public void TryFindNearestWalkable_TargetWalkable_ReturnsTarget()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));

            Assert.IsTrue(search.TryFindNearestWalkable(new Vector2Int(4, 4), out var cell));
            Assert.AreEqual(new Vector2Int(4, 4), cell);
        }

        [Test]
        public void TryFindNearestWalkable_WithCondition_SkipsCellsThatFailIt()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));
            var taken = new Vector2Int(4, 4);

            Assert.IsTrue(search.TryFindNearestWalkable(taken, cell => cell != taken, out var result));

            Assert.AreEqual(1, (result - taken).sqrMagnitude);
        }

        [Test]
        public void TryFindNearestWalkable_ConditionFailsEverywhere_ReturnsFalse()
        {
            var search = new GridSearch(new GridMap(4, 4, Vector2.zero));

            Assert.IsFalse(search.TryFindNearestWalkable(Vector2Int.zero, _ => false, out _));
        }

        [Test]
        public void TryFindNearestWalkable_TargetUnderBuilding_ReturnsClosestFreeCell()
        {
            var map = new GridMap(10, 10, Vector2.zero);
            map.TryOccupy(new RectInt(4, 4, 3, 3), new GameEntityId(1));

            new GridSearch(map).TryFindNearestWalkable(new Vector2Int(5, 5), out var cell);

            Assert.IsTrue(map.IsWalkable(cell));
            Assert.AreEqual(4, (cell - new Vector2Int(5, 5)).sqrMagnitude, "Closest free cells are 2 straight steps away.");
        }

        [Test]
        public void TryFindNearestWalkable_TargetOutsideMap_ReturnsCellInsideMap()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));

            search.TryFindNearestWalkable(new Vector2Int(-5, -5), out var cell);

            Assert.AreEqual(Vector2Int.zero, cell);
        }

        [Test]
        public void TryFindNearestWalkable_NoWalkableCells_ReturnsFalse()
        {
            var map = TestMaps.FromRows("##", "##");

            Assert.IsFalse(new GridSearch(map).TryFindNearestWalkable(Vector2Int.zero, out _));
        }

        [Test]
        public void CollectDestinations_OpenField_ReturnsDistinctCellsNearestFirst()
        {
            var target = new Vector2Int(5, 5);
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));

            search.CollectDestinations(target, 5, _results);

            Assert.AreEqual(5, _results.Count);
            Assert.AreEqual(target, _results[0]);
            Assert.AreEqual(_results.Count, _results.Distinct().Count(), "Every unit needs its own cell.");
            Assert.IsTrue(_results.All(cell => Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y) <= 1));
        }

        [Test]
        public void CollectDestinations_WithCondition_SkipsCellsButSearchesThroughThem()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));
            var target = new Vector2Int(5, 5);

            search.CollectDestinations(target, 3, cell => cell != target, _results);

            Assert.AreEqual(3, _results.Count);
            CollectionAssert.DoesNotContain(_results, target);
        }

        [Test]
        public void CollectDestinations_TargetUnderBuilding_OnlyReturnsFreeCells()
        {
            var map = new GridMap(10, 10, Vector2.zero);
            map.TryOccupy(new RectInt(4, 4, 2, 2), new GameEntityId(1));

            new GridSearch(map).CollectDestinations(new Vector2Int(4, 4), 6, _results);

            Assert.AreEqual(6, _results.Count);
            Assert.IsTrue(_results.All(map.IsWalkable));
        }

        [Test]
        public void CollectDestinations_DoesNotSpillDownPlateauEdge()
        {
            var map = TestMaps.FromRows(
                "..^^",
                "..^^");

            new GridSearch(map).CollectDestinations(new Vector2Int(2, 0), 8, _results);

            Assert.AreEqual(4, _results.Count);
            Assert.IsTrue(_results.All(cell => cell.x >= 2));
        }

        [Test]
        public void CollectDestinations_DoesNotCrossWater()
        {
            var map = TestMaps.FromRows(
                "...#...",
                "...#...",
                "...#...");

            new GridSearch(map).CollectDestinations(new Vector2Int(1, 1), 20, _results);

            Assert.AreEqual(9, _results.Count, "Only the 3x3 area left of the water is reachable.");
            Assert.IsTrue(_results.All(cell => cell.x < 3));
        }

        [Test]
        public void CollectDestinations_ZeroCount_ReturnsNothing()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));
            _results.Add(Vector2Int.one);

            search.CollectDestinations(new Vector2Int(5, 5), 0, _results);

            Assert.IsEmpty(_results);
        }

        [Test]
        public void CollectDestinations_NullList_Throws()
        {
            var search = new GridSearch(new GridMap(10, 10, Vector2.zero));

            Assert.Throws<ArgumentNullException>(() => search.CollectDestinations(Vector2Int.zero, 3, null));
        }
    }
}
