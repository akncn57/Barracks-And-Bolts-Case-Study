using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Construction;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    /// <summary>What the keyboard shortcuts and command buttons do: stop, hold position, attack-move, idle villagers, control groups.</summary>
    public class ShortcutTests
    {
        private const float Step = 0.1f;

        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitMovementSystem _movement;
        private CombatSystem _combatSystem;
        private AttackMoveSystem _attackMove;
        private UnitCommandService _commands;
        private UnitDefinition _knight;
        private UnitDefinition _weakEnemy;
        private UnitDefinition _villager;
        private BuildingDefinition _house;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(20, 12, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var search = new GridSearch(_map);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, search, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            var combat = new CombatService(_units, _buildings, _map, _eventBus);
            _combatSystem = new CombatSystem(_units, _movement, combat, _eventBus);

            var raids = Track(ScriptableObject.CreateInstance<RaidSettings>());
            var serialized = new SerializedObject(raids);
            serialized.FindProperty("aggroRange").floatValue = 5f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _attackMove = new AttackMoveSystem(_units, _movement, combat, _map, raids);

            var nodes = new ResourceNodeService(_map, ids, _eventBus);
            var gathering = new GatheringSystem(_units, _movement, nodes, _buildings, TestDefinitions.CreateFreeResources(_eventBus), search, _map,
                Track(ScriptableObject.CreateInstance<NormalModeSettings>()), _eventBus);
            var construction = new ConstructionSystem(_units, _movement, _buildings, _map, _eventBus);
            var farming = new FarmingSystem(_units, _movement, _buildings, TestDefinitions.CreateFreeResources(_eventBus), search, _map, _eventBus);
            _commands = new UnitCommandService(_units, _movement, combat, search, _map, gathering, construction, farming, _buildings, nodes, _eventBus);

            _knight = Track(TestDefinitions.CreateUnit("knight", maxHp: 20, damage: 5));
            _weakEnemy = Track(TestDefinitions.CreateUnit("raider", maxHp: 1));
            _villager = Track(TestDefinitions.CreateUnit("villager"));
            var villagerData = new SerializedObject(_villager);
            villagerData.FindProperty("canGather").boolValue = true;
            villagerData.ApplyModifiedPropertiesWithoutUndo();
            _house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
        }

        [TearDown]
        public void TearDown()
        {
            _movement.Dispose();
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private T Track<T>(T asset) where T : Object
        {
            _created.Add(asset);
            return asset;
        }

        private UnitModel Spawn(UnitDefinition definition, int x, int y, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_units.TrySpawn(definition, new Vector2Int(x, y), faction, out var unit));
            return unit;
        }

        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds; time += Step)
            {
                _movement.Tick(Step);
                _attackMove.Tick(Step);
                _combatSystem.Tick(Step);
            }
        }

        // ---- Stop and Hold Position

        [Test]
        public void Stop_EndsTheWalkAndTheTarget()
        {
            var knight = Spawn(_knight, 1, 1);
            var enemy = Spawn(_knight, 8, 1, Faction.Enemy);
            _commands.Attack(new[] { knight.Id }, enemy.Id, enemy.Position);
            Run(0.3f);
            Assert.IsTrue(knight.IsMoving);

            Assert.IsTrue(_commands.Stop(new[] { knight.Id }));

            Assert.IsFalse(knight.IsMoving);
            Assert.IsFalse(knight.HasAttackTarget);
        }

        [Test]
        public void Stop_EnemiesOnly_IsRefused()
        {
            var enemy = Spawn(_knight, 8, 1, Faction.Enemy);

            Assert.IsFalse(_commands.Stop(new[] { enemy.Id }));
        }

        [Test]
        public void HoldPosition_TargetOutOfRange_IsDroppedNotChased()
        {
            var knight = Spawn(_knight, 1, 1);
            var enemy = Spawn(_knight, 6, 1, Faction.Enemy);
            _commands.HoldPosition(new[] { knight.Id });

            // Hit from afar: a free unit would turn and chase.
            knight.SpotEnemy(enemy.Id);
            Run(0.3f);

            Assert.IsFalse(knight.HasAttackTarget);
            Assert.IsFalse(knight.IsMoving);
            Assert.IsTrue(knight.IsHoldingPosition);
        }

        [Test]
        public void HoldPosition_TargetInRange_IsHit()
        {
            var knight = Spawn(_knight, 1, 1);
            var enemy = Spawn(_knight, 2, 1, Faction.Enemy);
            _commands.HoldPosition(new[] { knight.Id });

            knight.SpotEnemy(enemy.Id);
            Run(0.3f);

            Assert.AreEqual(enemy.Id, knight.AttackTarget);
            Assert.Greater(knight.AttackCooldownLeft, 0f, "It struck.");
        }

        [Test]
        public void NewOrder_EndsHoldPosition()
        {
            var knight = Spawn(_knight, 1, 1);
            _commands.HoldPosition(new[] { knight.Id });

            _commands.MoveTo(new[] { knight.Id }, new Vector2(6.5f, 1.5f));

            Assert.IsFalse(knight.IsHoldingPosition);
        }

        // ---- Attack Move

        [Test]
        public void AttackMove_FightsWhatItMeets_ThenWalksOn()
        {
            var knight = Spawn(_knight, 1, 5);
            var raider = Spawn(_weakEnemy, 8, 5, Faction.Enemy);
            var attackEvents = 0;
            _eventBus.Subscribe<AttackCommandIssuedEvent>(_ => attackEvents++);

            Assert.IsTrue(_commands.AttackMove(new[] { knight.Id }, new Vector2(16.5f, 5.5f)));
            Assert.AreEqual(new Vector2Int(16, 5), knight.AttackMoveCell);
            Assert.AreEqual(1, attackEvents, "The red marker, not the move one.");

            var hasFought = false;
            for (var time = 0f; time < 10f && !raider.IsDead; time += Step)
            {
                Run(Step);
                hasFought |= knight.AttackTarget == raider.Id;
            }

            Assert.IsTrue(hasFought, "It turned on the raider on its way.");
            Assert.IsTrue(raider.IsDead);

            Run(15f);
            Assert.AreEqual(new Vector2Int(16, 5), _map.WorldToCell(knight.Position), "It walked on afterwards.");
            Assert.IsNull(knight.AttackMoveCell, "Arrived, so the attack-move is over.");
        }

        [Test]
        public void MoveTo_WalksPastEnemies()
        {
            var knight = Spawn(_knight, 1, 5);
            Spawn(_weakEnemy, 8, 5, Faction.Enemy);

            _commands.MoveTo(new[] { knight.Id }, new Vector2(16.5f, 5.5f));
            Run(3f);

            Assert.IsFalse(knight.HasAttackTarget);
            Assert.IsNull(knight.AttackMoveCell);
        }

        // ---- Idle villagers

        [Test]
        public void IdleVillagers_CountsOnlyIdlePlayerVillagers_AndCyclesThroughThem()
        {
            var first = Spawn(_villager, 1, 1);
            var second = Spawn(_villager, 3, 1);
            var busy = Spawn(_villager, 5, 1);
            busy.SetOnJob(true);
            Spawn(_villager, 7, 1, Faction.Enemy);
            Spawn(_knight, 9, 1);
            var finder = new IdleVillagerFinder(_units);

            Assert.AreEqual(2, finder.CountIdle());
            Assert.AreEqual(first, finder.FindNext());
            Assert.AreEqual(second, finder.FindNext());
            Assert.AreEqual(first, finder.FindNext(), "Starts again after the last one.");
        }

        [Test]
        public void IdleVillagers_NoneIdle_FindsNothing()
        {
            Spawn(_villager, 1, 1).SetOnJob(true);

            Assert.IsNull(new IdleVillagerFinder(_units).FindNext());
        }

        // ---- Control groups

        [Test]
        public void ControlGroup_KeepsOnlyOwnThings_AndForgetsTheDead()
        {
            var groups = new ControlGroupService(_units, _buildings, _map);
            var knight = Spawn(_knight, 1, 1);
            var other = Spawn(_knight, 2, 1);
            var enemy = Spawn(_knight, 8, 8, Faction.Enemy);
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(10, 2), Faction.Player, out var house));

            Assert.AreEqual(3, groups.Assign(3, new[] { knight.Id, other.Id, enemy.Id, house.Id }));

            _units.Remove(other.Id);
            CollectionAssert.AreEqual(new[] { knight.Id, house.Id }, groups.GetLiving(3));
            CollectionAssert.IsEmpty(groups.GetLiving(4));
        }

        [Test]
        public void ControlGroup_Center_IsTheMiddleOfItsMembers()
        {
            var groups = new ControlGroupService(_units, _buildings, _map);
            var a = Spawn(_knight, 1, 1);
            var b = Spawn(_knight, 5, 1);
            groups.Assign(1, new[] { a.Id, b.Id });

            Assert.IsTrue(groups.TryGetCenter(1, out var center));
            Assert.AreEqual((a.Position + b.Position) * 0.5f, center);
            Assert.IsFalse(groups.TryGetCenter(2, out _));
        }

        [Test]
        public void Selection_SelectList_ReplacesTheSelection()
        {
            var selection = new SelectionService(_units, _buildings, _eventBus);
            var a = Spawn(_knight, 1, 1);
            var b = Spawn(_knight, 3, 1);
            selection.Select(a.Id);

            selection.Select(new List<GameEntityId> { a.Id, b.Id });

            CollectionAssert.AreEqual(new[] { a.Id, b.Id }, selection.Selected);
            selection.Dispose();
        }
    }
}
