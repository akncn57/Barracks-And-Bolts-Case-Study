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
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    /// <summary>A blow lands on the strike frame and an arrow when it arrives, not when the attack starts.</summary>
    public class HitTimingTests
    {
        private const float Step = 0.05f;

        private readonly List<Object> _created = new();
        private EventBus _eventBus;
        private UnitService _units;
        private CombatService _combat;
        private CombatSystem _combatSystem;
        private int _attacks;
        private float _lastFlightTime;

        [SetUp]
        public void SetUp()
        {
            var map = new GridMap(16, 16, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var buildings = new BuildingService(map, ids, _eventBus);
            _units = new UnitService(map, new GridSearch(map), ids, _eventBus);
            _combat = new CombatService(_units, buildings, map, _eventBus);
            var movement = new UnitMovementSystem(_units, map, new AStarPathfinder(map), _eventBus);
            _combatSystem = new CombatSystem(_units, movement, _combat, _eventBus);
            _attacks = 0;
            _eventBus.Subscribe<UnitAttackedEvent>(e =>
            {
                _attacks++;
                _lastFlightTime = e.FlightTime;
            });
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private UnitDefinition CreateUnit(string id, float hitDelay, float attackRange = 1f, float projectileSpeed = 0f)
        {
            var unit = TestDefinitions.CreateUnit(id, damage: 4, attackRange: attackRange, attackCooldown: 5f);
            var serialized = new SerializedObject(unit);
            serialized.FindProperty("hitDelay").floatValue = hitDelay;
            serialized.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(unit);
            return unit;
        }

        private UnitModel Spawn(UnitDefinition definition, int x, Faction faction)
        {
            _units.TrySpawn(definition, new Vector2Int(x, 5), faction, out var unit);
            return unit;
        }

        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds - 0.0001f; time += Step)
                _combatSystem.Tick(Step);
        }

        [Test]
        public void Blow_LandsOnTheStrikeFrame()
        {
            var knight = Spawn(CreateUnit("knight", hitDelay: 0.2f), 5, Faction.Player);
            var target = Spawn(CreateUnit("dummy", 0f), 6, Faction.Enemy);
            knight.SetAttackTarget(target.Id);

            _combatSystem.Tick(Step);
            Assert.AreEqual(1, _attacks, "The swing started.");
            Assert.AreEqual(10, target.CurrentHp, "Not yet hit.");

            Run(0.1f);
            Assert.AreEqual(10, target.CurrentHp, "Still swinging.");

            Run(0.15f);
            Assert.AreEqual(6, target.CurrentHp, "Hit on the strike frame.");
        }

        [Test]
        public void Blow_DiesWithTheAttacker_BeforeItLands()
        {
            var knight = Spawn(CreateUnit("knight", hitDelay: 0.3f), 5, Faction.Player);
            var target = Spawn(CreateUnit("dummy", 0f), 6, Faction.Enemy);
            knight.SetAttackTarget(target.Id);
            _combatSystem.Tick(Step);

            knight.TakeDamage(100);
            Run(0.5f);

            Assert.AreEqual(10, target.CurrentHp);
        }

        [Test]
        public void Arrow_HitsWhenItArrives()
        {
            var archer = Spawn(CreateUnit("archer", hitDelay: 0.25f, attackRange: 5f, projectileSpeed: 10f), 2, Faction.Player);
            var target = Spawn(CreateUnit("dummy", 0f), 6, Faction.Enemy);
            archer.SetAttackTarget(target.Id);

            _combatSystem.Tick(Step);
            Assert.AreEqual(0.4f, _lastFlightTime, 0.01f, "Four cells at ten cells a second.");

            Run(0.5f);
            Assert.AreEqual(10, target.CurrentHp, "Still in the air.");

            Run(0.25f);
            Assert.AreEqual(6, target.CurrentHp, "Landed after release plus flight.");
        }

        [Test]
        public void Arrow_InTheAir_StillLands_IfTheArcherDies()
        {
            var archer = Spawn(CreateUnit("archer", hitDelay: 0.1f, attackRange: 5f, projectileSpeed: 10f), 2, Faction.Player);
            var target = Spawn(CreateUnit("dummy", 0f), 6, Faction.Enemy);
            archer.SetAttackTarget(target.Id);
            _combatSystem.Tick(Step);
            Run(0.15f);

            archer.TakeDamage(100);
            Run(0.5f);

            Assert.AreEqual(6, target.CurrentHp);
        }

        [Test]
        public void DamageWithNoAttacker_MakesNobodyTurnAround()
        {
            var soldier = Spawn(CreateUnit("knight", 0f), 5, Faction.Player);

            _combat.ApplyDamage(soldier.Id, 1, GameEntityId.None);

            Assert.AreEqual(9, soldier.CurrentHp);
            Assert.IsFalse(soldier.HasAttackTarget);
            Assert.IsFalse(soldier.HasJustSpottedEnemy);
        }

        [Test]
        public void HealthBar_FillsInWholePixels_AndShowsAnyHealthLeft()
        {
            // A 0.75 unit bar is 48 pixels, with one pixel of frame on each side: 46 pixels of track.
            Assert.AreEqual(46f / 64f, HealthBarView.GetFillWidth(0.75f, 1f), 0.0001f);
            Assert.AreEqual(23f / 64f, HealthBarView.GetFillWidth(0.75f, 0.5f), 0.0001f);
            Assert.AreEqual(1f / 64f, HealthBarView.GetFillWidth(0.75f, 0.001f), 0.0001f, "Alive shows at least a pixel.");
            Assert.AreEqual(0f, HealthBarView.GetFillWidth(0.75f, 0f));
        }
    }
}
