using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
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
    public class UnitMovementTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitMovementSystem _movement;
        private UnitDefinition _knight;
        private BuildingDefinition _wall;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _knight = TestDefinitions.CreateUnit("knight");
            _wall = TestDefinitions.CreateBuilding("wall", new Vector2Int(1, 5));
        }

        [TearDown]
        public void TearDown()
        {
            _movement.Dispose();
            Object.DestroyImmediate(_knight);
            Object.DestroyImmediate(_wall);
        }

        private UnitModel Spawn(Vector2Int cell)
        {
            Assert.IsTrue(_units.TrySpawn(_knight, cell, Faction.Player, out var unit));
            return unit;
        }

        private void RunUntilStopped(UnitModel unit)
        {
            for (var i = 0; i < 2000 && unit.IsMoving; i++)
                _movement.Tick(0.05f);
        }

        [Test]
        public void SendTo_WalksToTheCellCenterAndStops()
        {
            var unit = Spawn(new Vector2Int(1, 1));

            _movement.SendTo(unit, new Vector2Int(6, 4));
            Assert.IsTrue(unit.IsMoving);
            RunUntilStopped(unit);

            Assert.IsFalse(unit.IsMoving);
            Assert.AreEqual(new Vector2(6.5f, 4.5f), unit.Position);
        }

        [Test]
        public void Tick_MovesAtTheUnitsSpeed()
        {
            var unit = Spawn(new Vector2Int(0, 0));
            var start = unit.Position;

            _movement.SendTo(unit, new Vector2Int(9, 0));
            _movement.Tick(1f);

            Assert.AreEqual(_knight.MoveSpeed, unit.Position.x - start.x, 0.001f);
            Assert.AreEqual(start.y, unit.Position.y, 0.001f);
        }

        [Test]
        public void Tick_LongFrame_CarriesOverWaypointsWithoutOvershooting()
        {
            var unit = Spawn(new Vector2Int(0, 0));

            _movement.SendTo(unit, new Vector2Int(2, 0));
            _movement.Tick(100f);

            Assert.AreEqual(new Vector2(2.5f, 0.5f), unit.Position);
            Assert.IsFalse(unit.IsMoving);
        }

        [Test]
        public void SendTo_WalksAroundBuildings()
        {
            Assert.IsTrue(_buildings.TryPlace(_wall, new Vector2Int(5, 0), Faction.Player, out _));
            var unit = Spawn(new Vector2Int(2, 2));

            _movement.SendTo(unit, new Vector2Int(8, 2));

            for (var i = 0; i < 2000 && unit.IsMoving; i++)
            {
                _movement.Tick(0.05f);
                Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(unit.Position)), $"Inside the wall at {unit.Position}.");
            }

            Assert.AreEqual(new Vector2(8.5f, 2.5f), unit.Position);
        }

        [Test]
        public void SendTo_BlockedCell_StopsAtTheClosestReachableCell()
        {
            Assert.IsTrue(_buildings.TryPlace(_wall, new Vector2Int(5, 0), Faction.Player, out _));
            var unit = Spawn(new Vector2Int(2, 2));

            _movement.SendTo(unit, new Vector2Int(5, 2));
            RunUntilStopped(unit);

            Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(unit.Position)));
            Assert.AreEqual(1, Mathf.Abs(_map.WorldToCell(unit.Position).x - 5));
        }

        [Test]
        public void SendTo_SameCellFromOffCenter_WalksBackToCenter()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            _movement.SendTo(unit, new Vector2Int(9, 1));
            _movement.Tick(0.05f);
            Assert.AreEqual(new Vector2Int(1, 1), _map.WorldToCell(unit.Position));

            _movement.SendTo(unit, new Vector2Int(1, 1));
            RunUntilStopped(unit);

            Assert.AreEqual(new Vector2(1.5f, 1.5f), unit.Position);
        }

        [Test]
        public void NewOrder_ReplacesTheOldPath()
        {
            var unit = Spawn(new Vector2Int(1, 1));

            _movement.SendTo(unit, new Vector2Int(9, 1));
            _movement.Tick(0.5f);
            _movement.SendTo(unit, new Vector2Int(1, 8));
            RunUntilStopped(unit);

            Assert.AreEqual(new Vector2(1.5f, 8.5f), unit.Position);
        }

        [Test]
        public void BuildingOnThePath_MakesTheUnitGoAround()
        {
            var unit = Spawn(new Vector2Int(0, 2));
            _movement.SendTo(unit, new Vector2Int(9, 2));
            Assert.IsTrue(unit.IsPathThrough(new RectInt(5, 0, 1, 5), _map.WorldToCell), "The straight path crosses x = 5.");

            Assert.IsTrue(_buildings.TryPlace(_wall, new Vector2Int(5, 0), Faction.Player, out _));

            Assert.IsFalse(unit.IsPathThrough(new RectInt(5, 0, 1, 5), _map.WorldToCell));
            RunUntilStopped(unit);
            Assert.AreEqual(new Vector2(9.5f, 2.5f), unit.Position);
        }

        [Test]
        public void BuildingOffThePath_LeavesThePathAlone()
        {
            var unit = Spawn(new Vector2Int(0, 8));
            _movement.SendTo(unit, new Vector2Int(9, 8));
            var destination = unit.Destination;

            Assert.IsTrue(_buildings.TryPlace(_wall, new Vector2Int(5, 0), Faction.Player, out _));

            Assert.AreEqual(destination, unit.Destination);
            Assert.IsTrue(unit.IsMoving);
        }

        [Test]
        public void Facing_FollowsSidewaysMovementOnly()
        {
            var unit = Spawn(new Vector2Int(5, 1));
            Assert.IsFalse(unit.IsFacingLeft);

            _movement.SendTo(unit, new Vector2Int(2, 1));
            _movement.Tick(0.1f);
            Assert.IsTrue(unit.IsFacingLeft);

            RunUntilStopped(unit);
            _movement.SendTo(unit, new Vector2Int(2, 6));
            _movement.Tick(0.1f);
            Assert.IsTrue(unit.IsFacingLeft, "Walking straight up keeps the last facing.");
        }

        [Test]
        public void Destination_IsPathEndOrCurrentPosition()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            Assert.AreEqual(unit.Position, unit.Destination);

            _movement.SendTo(unit, new Vector2Int(4, 1));

            Assert.AreEqual(new Vector2(4.5f, 1.5f), unit.Destination);
        }

        [Test]
        public void Stop_EndsTheMoveWhereTheUnitIs()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            _movement.SendTo(unit, new Vector2Int(8, 1));
            _movement.Tick(0.5f);
            var position = unit.Position;

            unit.Stop();
            _movement.Tick(1f);

            Assert.AreEqual(position, unit.Position);
            Assert.IsFalse(unit.IsMoving);
        }
    }
}
