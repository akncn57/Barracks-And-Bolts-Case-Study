using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Commands;
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
    public class UnitCommandServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitMovementSystem _movement;
        private UnitCommandService _commands;
        private UnitDefinition _knight;
        private BuildingDefinition _house;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(12, 12, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var search = new GridSearch(_map);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, search, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _commands = new UnitCommandService(_units, _movement, new BarracksAndBolts.Gameplay.Combat.CombatService(_units, _buildings, _map, _eventBus), search, _map, _eventBus);
            _knight = TestDefinitions.CreateUnit("knight");
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
        }

        [TearDown]
        public void TearDown()
        {
            _movement.Dispose();
            Object.DestroyImmediate(_knight);
            Object.DestroyImmediate(_house);
        }

        private UnitModel Spawn(Vector2Int cell, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_units.TrySpawn(_knight, cell, faction, out var unit));
            return unit;
        }

        private List<GameEntityId> SpawnGroup(int count)
        {
            var ids = new List<GameEntityId>();
            for (var i = 0; i < count; i++)
                ids.Add(Spawn(new Vector2Int(i, 0)).Id);
            return ids;
        }

        private HashSet<Vector2Int> DestinationCells(IEnumerable<GameEntityId> ids)
        {
            var cells = new HashSet<Vector2Int>();
            foreach (var id in ids)
            {
                Assert.IsTrue(_units.TryGet(id, out var unit));
                cells.Add(_map.WorldToCell(unit.Destination));
            }

            return cells;
        }

        [Test]
        public void CommandAt_OnEnemyUnit_OrdersAttack()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            var enemy = Spawn(new Vector2Int(6, 6), Faction.Enemy);
            var attackEvents = 0;
            var moveEvents = 0;
            _eventBus.Subscribe<AttackCommandIssuedEvent>(_ => attackEvents++);
            _eventBus.Subscribe<MoveCommandIssuedEvent>(_ => moveEvents++);

            Assert.IsTrue(_commands.CommandAt(new[] { unit.Id }, enemy.Position));

            Assert.AreEqual(enemy.Id, unit.AttackTarget);
            Assert.AreEqual(1, attackEvents);
            Assert.AreEqual(0, moveEvents);
        }

        [Test]
        public void CommandAt_OnEnemyBuilding_OrdersAttack()
        {
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(6, 6), Faction.Enemy, out var building));
            var unit = Spawn(new Vector2Int(1, 1));

            _commands.CommandAt(new[] { unit.Id }, new Vector2(7f, 7f));

            Assert.AreEqual(building.Id, unit.AttackTarget);
        }

        [Test]
        public void CommandAt_OnOwnBuilding_MovesNextToIt()
        {
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(6, 6), Faction.Player, out _));
            var unit = Spawn(new Vector2Int(1, 1));

            _commands.CommandAt(new[] { unit.Id }, new Vector2(7f, 7f));

            Assert.IsFalse(unit.HasAttackTarget, "No friendly fire.");
            Assert.IsTrue(unit.IsMoving);
        }

        [Test]
        public void CommandAt_OnOwnUnit_Moves()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            var friend = Spawn(new Vector2Int(6, 6));

            _commands.CommandAt(new[] { unit.Id }, friend.Position);

            Assert.IsFalse(unit.HasAttackTarget);
            Assert.IsTrue(unit.IsMoving);
        }

        [Test]
        public void Attack_FriendlyTarget_IsRefused()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            var friend = Spawn(new Vector2Int(6, 6));

            Assert.IsFalse(_commands.Attack(new[] { unit.Id }, friend.Id, friend.Position));

            Assert.IsFalse(unit.HasAttackTarget);
        }

        [Test]
        public void Attack_EnemiesInTheSelection_GetNoOrder()
        {
            var enemy = Spawn(new Vector2Int(1, 1), Faction.Enemy);
            var otherEnemy = Spawn(new Vector2Int(6, 6), Faction.Enemy);

            Assert.IsFalse(_commands.CommandAt(new[] { enemy.Id }, otherEnemy.Position));

            Assert.IsFalse(enemy.HasAttackTarget);
        }

        [Test]
        public void MoveTo_ClearsTheAttackOrder()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            var enemy = Spawn(new Vector2Int(6, 6), Faction.Enemy);
            _commands.CommandAt(new[] { unit.Id }, enemy.Position);

            _commands.MoveTo(new[] { unit.Id }, new Vector2(1.5f, 8.5f));

            Assert.IsFalse(unit.HasAttackTarget);
        }

        [Test]
        public void MoveTo_OneUnit_GoesToTheClickedCell()
        {
            var unit = Spawn(new Vector2Int(1, 1));

            Assert.IsTrue(_commands.MoveTo(new[] { unit.Id }, new Vector2(6.2f, 7.8f)));

            Assert.AreEqual(new Vector2(6.5f, 7.5f), unit.Destination);
        }

        [Test]
        public void MoveTo_Group_GetsDifferentCellsAroundTheTarget()
        {
            var ids = SpawnGroup(6);

            _commands.MoveTo(ids, new Vector2(8.5f, 8.5f));

            var cells = DestinationCells(ids);
            Assert.AreEqual(6, cells.Count, "Every unit has its own cell.");
            Assert.IsTrue(cells.Contains(new Vector2Int(8, 8)), "Someone takes the clicked cell.");
            foreach (var cell in cells)
                Assert.LessOrEqual((cell - new Vector2Int(8, 8)).sqrMagnitude, 4, "The group stays around the target.");
        }

        [Test]
        public void MoveTo_ClickOnBuilding_GoesNextToIt()
        {
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(6, 6), Faction.Player, out _));
            var unit = Spawn(new Vector2Int(1, 1));

            _commands.MoveTo(new[] { unit.Id }, new Vector2(7f, 7f));

            Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(unit.Destination)));
        }

        [Test]
        public void MoveTo_OutsideMap_StaysOnTheMap()
        {
            var unit = Spawn(new Vector2Int(1, 1));

            Assert.IsTrue(_commands.MoveTo(new[] { unit.Id }, new Vector2(40f, 3.5f)));

            Assert.AreEqual(new Vector2Int(11, 3), _map.WorldToCell(unit.Destination), "Goes to the edge in that direction.");
        }

        [Test]
        public void MoveTo_AvoidsCellsOfOtherUnits()
        {
            var standing = Spawn(new Vector2Int(8, 8));
            var walker = Spawn(new Vector2Int(1, 1));
            _commands.MoveTo(new[] { walker.Id }, new Vector2(4.5f, 4.5f));
            var mover = Spawn(new Vector2Int(1, 3));

            _commands.MoveTo(new[] { mover.Id }, new Vector2(8.5f, 8.5f));
            Assert.AreNotEqual(_map.WorldToCell(standing.Position), _map.WorldToCell(mover.Destination), "A standing unit keeps its cell.");

            _commands.MoveTo(new[] { mover.Id }, new Vector2(4.5f, 4.5f));
            Assert.AreNotEqual(_map.WorldToCell(walker.Destination), _map.WorldToCell(mover.Destination), "A cell someone is walking to is taken.");
        }

        [Test]
        public void MoveTo_SameGroupAgain_CanReuseItsOwnCells()
        {
            var ids = SpawnGroup(3);
            _commands.MoveTo(ids, new Vector2(8.5f, 8.5f));
            var first = DestinationCells(ids);

            _commands.MoveTo(ids, new Vector2(8.5f, 8.5f));

            CollectionAssert.AreEquivalent(first, DestinationCells(ids));
        }

        [Test]
        public void MoveTo_IgnoresEnemiesAndBuildings()
        {
            var enemy = Spawn(new Vector2Int(5, 5), Faction.Enemy);
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(1, 1), Faction.Player, out var building));

            var result = _commands.MoveTo(new[] { enemy.Id, building.Id }, new Vector2(8.5f, 8.5f));

            Assert.IsFalse(result);
            Assert.IsFalse(enemy.IsMoving);
        }

        [Test]
        public void MoveTo_PublishesTheClickedPoint()
        {
            var unit = Spawn(new Vector2Int(1, 1));
            var published = Vector2.zero;
            var count = 0;
            _eventBus.Subscribe<MoveCommandIssuedEvent>(e => { published = e.Position; count++; });

            _commands.MoveTo(new[] { unit.Id }, new Vector2(6.2f, 7.8f));

            Assert.AreEqual(1, count);
            Assert.AreEqual(new Vector2(6.2f, 7.8f), published);
        }

        [Test]
        public void MoveTo_NothingToMove_PublishesNothing()
        {
            var count = 0;
            _eventBus.Subscribe<MoveCommandIssuedEvent>(_ => count++);

            _commands.MoveTo(new List<GameEntityId>(), new Vector2(6f, 7f));

            Assert.AreEqual(0, count);
        }

        [Test]
        public void MoveTo_UnitsNearestTheCellsTakeThem()
        {
            var left = Spawn(new Vector2Int(0, 5));
            var right = Spawn(new Vector2Int(11, 5));

            _commands.MoveTo(new[] { right.Id, left.Id }, new Vector2(1.5f, 5.5f));

            Assert.AreEqual(new Vector2Int(1, 5), _map.WorldToCell(left.Destination), "The closer unit takes the clicked cell.");
        }
    }
}
