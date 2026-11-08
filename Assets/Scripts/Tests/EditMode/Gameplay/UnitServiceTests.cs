using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class UnitServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private UnitService _units;
        private UnitDefinition _knight;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            _units = new UnitService(_map, new GridSearch(_map), new GameEntityIdGenerator(), _eventBus);
            _knight = TestDefinitions.CreateUnit("knight", maxHp: 25);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_knight);
        }

        private UnitModel Spawn(Vector2Int cell)
        {
            Assert.IsTrue(_units.TrySpawn(_knight, cell, Faction.Player, out var unit));
            return unit;
        }

        [Test]
        public void TrySpawn_FreeCell_CreatesUnitInCellCenter()
        {
            var unit = Spawn(new Vector2Int(3, 4));

            Assert.AreEqual(new Vector2(3.5f, 4.5f), unit.Position);
            Assert.AreEqual(25, unit.CurrentHp);
            Assert.AreEqual(Faction.Player, unit.Faction);
            Assert.AreEqual(1, _units.Count);
            Assert.IsTrue(_units.TryGet(unit.Id, out var found));
            Assert.AreSame(unit, found);
        }

        [Test]
        public void TrySpawn_PublishesEvent()
        {
            UnitModel published = null;
            _eventBus.Subscribe<UnitSpawnedEvent>(e => published = e.Unit);

            var unit = Spawn(new Vector2Int(3, 4));

            Assert.AreSame(unit, published);
        }

        [Test]
        public void TrySpawn_CellHasUnit_UsesNearestFreeCell()
        {
            Spawn(new Vector2Int(3, 4));

            var second = Spawn(new Vector2Int(3, 4));

            var cell = _map.WorldToCell(second.Position);
            Assert.AreNotEqual(new Vector2Int(3, 4), cell);
            Assert.AreEqual(1, (cell - new Vector2Int(3, 4)).sqrMagnitude, "A straight neighbour is the closest free cell.");
        }

        [Test]
        public void TrySpawn_CellUnderBuilding_UsesNearestFreeCell()
        {
            _map.TryOccupy(new RectInt(2, 2, 3, 3), new GameEntityId(99));

            var unit = Spawn(new Vector2Int(3, 3));

            Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(unit.Position)));
        }

        [Test]
        public void TrySpawn_CellOutsideMap_UsesNearestCellInside()
        {
            var unit = Spawn(new Vector2Int(3, -1));

            Assert.AreEqual(new Vector2Int(3, 0), _map.WorldToCell(unit.Position));
        }

        [Test]
        public void TrySpawn_ManyUnits_NeverShareACell()
        {
            for (var i = 0; i < 20; i++)
                Spawn(new Vector2Int(5, 5));

            var taken = 0;
            for (var x = 0; x < _map.Width; x++)
            {
                for (var y = 0; y < _map.Height; y++)
                    taken += _units.HasUnitAt(new Vector2Int(x, y)) ? 1 : 0;
            }

            Assert.AreEqual(20, taken);
        }

        [Test]
        public void TrySpawn_MapFull_ReturnsFalseAndCreatesNothing()
        {
            var map = new GridMap(1, 2, Vector2.zero);
            var units = new UnitService(map, new GridSearch(map), new GameEntityIdGenerator(), _eventBus);
            var published = 0;
            _eventBus.Subscribe<UnitSpawnedEvent>(_ => published++);
            units.TrySpawn(_knight, Vector2Int.zero, Faction.Player, out _);
            units.TrySpawn(_knight, Vector2Int.zero, Faction.Player, out _);

            var result = units.TrySpawn(_knight, Vector2Int.zero, Faction.Player, out var unit);

            Assert.IsFalse(result);
            Assert.IsNull(unit);
            Assert.AreEqual(2, units.Count);
            Assert.AreEqual(2, published);
        }

        [Test]
        public void TrySpawn_NullDefinition_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _units.TrySpawn(null, Vector2Int.zero, Faction.Player, out _));
        }

        [Test]
        public void HasUnitIn_ChecksOnlyTheArea()
        {
            Spawn(new Vector2Int(3, 4));

            Assert.IsTrue(_units.HasUnitIn(new RectInt(2, 3, 2, 2)));
            Assert.IsFalse(_units.HasUnitIn(new RectInt(4, 4, 2, 2)), "The unit is one cell left of this area.");
            Assert.IsFalse(_units.HasUnitIn(new RectInt(0, 0, 3, 3)));
        }

        [Test]
        public void GetUnitAt_InsideBody_ReturnsTheUnit()
        {
            var unit = Spawn(new Vector2Int(3, 4));

            Assert.AreEqual(unit.Id, _units.GetUnitAt(unit.Position + new Vector2(0.2f, 1f)));
        }

        [Test]
        public void GetUnitAt_OutsideBody_ReturnsNone()
        {
            var unit = Spawn(new Vector2Int(3, 4));

            Assert.AreEqual(GameEntityId.None, _units.GetUnitAt(unit.Position + new Vector2(1f, 0f)));
            Assert.AreEqual(GameEntityId.None, _units.GetUnitAt(unit.Position + new Vector2(0f, 2f)));
        }

        [Test]
        public void GetUnitAt_Overlapping_ReturnsTheOneInFront()
        {
            var behind = Spawn(new Vector2Int(3, 5));
            var front = Spawn(new Vector2Int(3, 4));
            var point = new Vector2(front.Position.x, behind.Position.y);

            Assert.AreEqual(front.Id, _units.GetUnitAt(point));
        }

        [Test]
        public void CollectUnitsIn_TouchingTheBody_Counts()
        {
            var unit = Spawn(new Vector2Int(3, 4));
            var results = new System.Collections.Generic.List<GameEntityId> { new(99) };

            // The box only reaches the unit's head, not its feet.
            var area = Rect.MinMaxRect(unit.Position.x - 0.1f, unit.Position.y + 1f, unit.Position.x + 0.1f, unit.Position.y + 3f);
            _units.CollectUnitsIn(area, Faction.Player, results);

            CollectionAssert.AreEqual(new[] { unit.Id }, results);
        }

        [Test]
        public void CollectUnitsIn_OtherFaction_IsSkipped()
        {
            Spawn(new Vector2Int(3, 4));
            var results = new System.Collections.Generic.List<GameEntityId>();

            _units.CollectUnitsIn(new Rect(0f, 0f, 10f, 10f), Faction.Enemy, results);

            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void Remove_TakesTheUnitOutAndTells()
        {
            var unit = Spawn(new Vector2Int(3, 4));
            UnitModel died = null;
            _eventBus.Subscribe<UnitDiedEvent>(e => died = e.Unit);

            Assert.IsTrue(_units.Remove(unit.Id));

            Assert.AreSame(unit, died);
            Assert.AreEqual(0, _units.Count);
            Assert.IsFalse(_units.HasUnitAt(new Vector2Int(3, 4)));
        }

        [Test]
        public void Remove_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_units.Remove(new GameEntityId(5)));
        }

        [Test]
        public void TryGet_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_units.TryGet(new GameEntityId(5), out _));
        }

        [Test]
        public void Spawn_DoesNotBlockTheGrid()
        {
            Spawn(new Vector2Int(3, 4));

            Assert.IsTrue(_map.IsWalkable(new Vector2Int(3, 4)), "Only buildings and terrain block paths.");
        }
    }
}
