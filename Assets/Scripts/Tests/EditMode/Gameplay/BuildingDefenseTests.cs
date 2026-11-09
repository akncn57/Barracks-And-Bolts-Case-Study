using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class BuildingDefenseTests
    {
        private const int Damage = 2;
        private const float Range = 5f;

        // The tower's footprint is cells (10,10) to (11,11), so its middle is the corner point (11,11).
        private static readonly Vector2Int TowerCell = new(10, 10);

        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private CombatService _combat;
        private BuildingDefenseSystem _defense;
        private DeathCleanupSystem _cleanup;
        private BuildingDefinition _tower;
        private BuildingDefinition _house;
        private UnitDefinition _soldier;
        private List<BuildingFiredEvent> _shots;

        [SetUp]
        public void SetUp()
        {
            var map = new GridMap(30, 30, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(map, ids, _eventBus);
            _units = new UnitService(map, new GridSearch(map), ids, _eventBus);
            _combat = new CombatService(_units, _buildings, map, _eventBus);
            _defense = new BuildingDefenseSystem(_buildings, _combat, map, _eventBus);
            _cleanup = new DeathCleanupSystem(_combat);

            _tower = TestDefinitions.CreateTower("tower", Range, Damage);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
            _soldier = TestDefinitions.CreateUnit("soldier", maxHp: 10);

            _shots = new List<BuildingFiredEvent>();
            _eventBus.Subscribe<BuildingFiredEvent>(e => _shots.Add(e));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tower);
            Object.DestroyImmediate(_house);
            Object.DestroyImmediate(_soldier);
        }

        private BuildingModel PlaceTower(Faction faction = Faction.Player)
        {
            Assert.IsTrue(_buildings.TryPlace(_tower, TowerCell, faction, out var tower));
            return tower;
        }

        private UnitModel Spawn(Vector2Int cell, Faction faction = Faction.Enemy)
        {
            Assert.IsTrue(_units.TrySpawn(_soldier, cell, faction, out var unit));
            return unit;
        }

        [Test]
        public void EnemyInRange_GetsShot()
        {
            var tower = PlaceTower();
            var enemy = Spawn(new Vector2Int(13, 11));

            _defense.Tick(0.1f);

            Assert.AreEqual(10 - Damage, enemy.CurrentHp);
            Assert.AreEqual(1, _shots.Count);
            Assert.AreEqual(tower, _shots[0].Building);
        }

        [Test]
        public void EnemyOutOfRange_IsLeftAlone()
        {
            PlaceTower();
            var enemy = Spawn(new Vector2Int(25, 11));

            _defense.Tick(0.1f);

            Assert.AreEqual(10, enemy.CurrentHp);
            Assert.AreEqual(0, _shots.Count);
        }

        [Test]
        public void OwnUnit_IsLeftAlone()
        {
            PlaceTower();
            var friend = Spawn(new Vector2Int(13, 11), Faction.Player);

            _defense.Tick(0.1f);

            Assert.AreEqual(10, friend.CurrentHp);
        }

        [Test]
        public void EnemyTower_ShootsThePlayer()
        {
            PlaceTower(Faction.Enemy);
            var player = Spawn(new Vector2Int(13, 11), Faction.Player);

            _defense.Tick(0.1f);

            Assert.AreEqual(10 - Damage, player.CurrentHp);
        }

        [Test]
        public void Shots_WaitForTheCooldown()
        {
            PlaceTower();
            Spawn(new Vector2Int(13, 11));

            _defense.Tick(0.1f);
            _defense.Tick(0.5f);
            Assert.AreEqual(1, _shots.Count);

            _defense.Tick(0.5f);
            Assert.AreEqual(2, _shots.Count);
        }

        [Test]
        public void Target_IsKeptWhenSomeoneCloserComes()
        {
            var tower = PlaceTower();
            var first = Spawn(new Vector2Int(14, 11));
            _defense.Tick(0.1f);
            Assert.AreEqual(first.Id, tower.DefenseTarget);

            Spawn(new Vector2Int(12, 11));
            _defense.Tick(1f);

            Assert.AreEqual(first.Id, tower.DefenseTarget);
        }

        [Test]
        public void Target_ThatDies_IsReplaced()
        {
            var tower = PlaceTower();
            var first = Spawn(new Vector2Int(12, 11));
            var second = Spawn(new Vector2Int(14, 11));
            _defense.Tick(0.1f);
            first.TakeDamage(100);

            _defense.Tick(1f);

            Assert.AreEqual(second.Id, tower.DefenseTarget);
        }

        [Test]
        public void EnemyBuilding_IsNotATarget()
        {
            var tower = PlaceTower();
            Assert.IsTrue(_buildings.TryPlace(_house, new Vector2Int(13, 10), Faction.Enemy, out var house));

            _defense.Tick(0.1f);

            Assert.AreEqual(GameEntityId.None, tower.DefenseTarget);
            Assert.AreEqual(house.Definition.MaxHp, house.CurrentHp);
        }

        [Test]
        public void BuildingWithoutDefense_NeverShoots()
        {
            Assert.IsTrue(_buildings.TryPlace(_house, TowerCell, Faction.Player, out _));
            Spawn(new Vector2Int(13, 11));

            _defense.Tick(1f);

            Assert.AreEqual(0, _shots.Count);
        }

        [Test]
        public void IdleEnemy_TurnsOnTheTower()
        {
            var tower = PlaceTower();
            var enemy = Spawn(new Vector2Int(13, 11));

            _defense.Tick(0.1f);

            Assert.AreEqual(tower.Id, enemy.AttackTarget);
        }

        [Test]
        public void KillingShot_RemovesTheEnemy()
        {
            PlaceTower();
            var enemy = Spawn(new Vector2Int(13, 11));

            for (var i = 0; i < 5; i++)
                _defense.Tick(1f);
            _cleanup.Tick(0f);

            Assert.IsFalse(_units.TryGet(enemy.Id, out _));
        }

        [Test]
        public void Tooltip_ShowsTheNumbersOfATowerOnly()
        {
            StringAssert.Contains("Range: 5", TooltipText.ForBuilding(_tower));
            StringAssert.DoesNotContain("Range", TooltipText.ForBuilding(_house));
        }
    }
}
