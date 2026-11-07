using System;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class GridMapTests
    {
        private static readonly GameEntityId Barracks = new(1);
        private static readonly GameEntityId House = new(2);

        private GridMap _map;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 8, Vector2.zero);
        }

        [TestCase(0, 5)]
        [TestCase(5, 0)]
        [TestCase(-1, 5)]
        public void Constructor_InvalidSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GridMap(width, height, Vector2.zero));
        }

        [TestCase(0, 0, true)]
        [TestCase(9, 7, true)]
        [TestCase(10, 0, false)]
        [TestCase(0, 8, false)]
        [TestCase(-1, 0, false)]
        public void IsInBounds_Cell(int x, int y, bool expected)
        {
            Assert.AreEqual(expected, _map.IsInBounds(new Vector2Int(x, y)));
        }

        [Test]
        public void WorldToCell_UsesOrigin()
        {
            var map = new GridMap(10, 10, new Vector2(-5f, -5f));

            Assert.AreEqual(new Vector2Int(0, 0), map.WorldToCell(new Vector2(-4.5f, -4.5f)));
            Assert.AreEqual(new Vector2Int(5, 5), map.WorldToCell(new Vector2(0.2f, 0.9f)));
        }

        [Test]
        public void WorldToCell_NegativeFraction_FloorsToPreviousCell()
        {
            Assert.AreEqual(new Vector2Int(-1, -1), _map.WorldToCell(new Vector2(-0.5f, -0.1f)));
        }

        [Test]
        public void CellToWorldCenter_RoundTripsWithWorldToCell()
        {
            var cell = new Vector2Int(3, 6);

            Assert.AreEqual(cell, _map.WorldToCell(_map.CellToWorldCenter(cell)));
        }

        [Test]
        public void AreaToWorldBottomCenter_ReturnsBottomMiddleOfArea()
        {
            var position = _map.AreaToWorldBottomCenter(new RectInt(2, 3, 4, 2));

            Assert.AreEqual(new Vector2(4f, 3f), position);
        }

        [Test]
        public void IsWalkable_BlockedTerrain_ReturnsFalse()
        {
            var cell = new Vector2Int(1, 1);
            _map.SetTerrainWalkable(cell, false);

            Assert.IsFalse(_map.IsWalkable(cell));
        }

        [Test]
        public void IsWalkable_OutOfBounds_ReturnsFalse()
        {
            Assert.IsFalse(_map.IsWalkable(new Vector2Int(-1, 0)));
        }

        [Test]
        public void SetTerrainWalkable_OutOfBounds_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _map.SetTerrainWalkable(new Vector2Int(10, 0), false));
        }

        [Test]
        public void TryOccupy_FreeArea_OccupiesEveryCell()
        {
            var area = new RectInt(2, 2, 3, 2);

            Assert.IsTrue(_map.TryOccupy(area, Barracks));
            foreach (var cell in area.allPositionsWithin)
            {
                Assert.AreEqual(Barracks, _map.GetOccupant(cell));
                Assert.IsFalse(_map.IsWalkable(cell));
            }
        }

        [Test]
        public void TryOccupy_OverlappingArea_FailsWithoutPartialOccupation()
        {
            _map.TryOccupy(new RectInt(4, 4, 2, 2), Barracks);
            var overlapping = new RectInt(2, 2, 3, 3);

            Assert.IsFalse(_map.TryOccupy(overlapping, House));
            Assert.AreEqual(GameEntityId.None, _map.GetOccupant(new Vector2Int(2, 2)));
        }

        [Test]
        public void TryOccupy_PartlyOutsideMap_Fails()
        {
            Assert.IsFalse(_map.TryOccupy(new RectInt(8, 6, 3, 3), Barracks));
            Assert.AreEqual(GameEntityId.None, _map.GetOccupant(new Vector2Int(9, 7)));
        }

        [Test]
        public void TryOccupy_OnBlockedTerrain_Fails()
        {
            _map.SetTerrainWalkable(new Vector2Int(3, 3), false);

            Assert.IsFalse(_map.TryOccupy(new RectInt(2, 2, 2, 2), Barracks));
        }

        [Test]
        public void TryOccupy_EmptyArea_Fails()
        {
            Assert.IsFalse(_map.TryOccupy(new RectInt(2, 2, 0, 3), Barracks));
        }

        [Test]
        public void TryOccupy_InvalidEntity_Throws()
        {
            Assert.Throws<ArgumentException>(() => _map.TryOccupy(new RectInt(0, 0, 1, 1), GameEntityId.None));
        }

        [Test]
        public void Release_FreesOnlyCellsOfThatEntity()
        {
            _map.TryOccupy(new RectInt(0, 0, 2, 2), Barracks);
            _map.TryOccupy(new RectInt(2, 0, 2, 2), House);

            _map.Release(new RectInt(0, 0, 4, 2), Barracks);

            Assert.IsTrue(_map.IsAreaFree(new RectInt(0, 0, 2, 2)));
            Assert.AreEqual(House, _map.GetOccupant(new Vector2Int(2, 0)));
        }

        [Test]
        public void Release_AreaPartlyOutsideMap_DoesNotThrow()
        {
            _map.TryOccupy(new RectInt(8, 6, 2, 2), Barracks);

            Assert.DoesNotThrow(() => _map.Release(new RectInt(8, 6, 5, 5), Barracks));
            Assert.IsTrue(_map.IsAreaFree(new RectInt(8, 6, 2, 2)));
        }
    }
}
