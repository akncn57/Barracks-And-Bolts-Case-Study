using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class CombatTests
    {
        private const float StepSeconds = 0.05f;

        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitMovementSystem _movement;
        private CombatService _combat;
        private SelectionService _selection;
        private SimulationRunner _runner;
        private UnitDefinition _knight;
        private UnitDefinition _archer;
        private UnitDefinition _tank;
        private BuildingDefinition _house;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(14, 14, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _combat = new CombatService(_units, _buildings, _map, _eventBus);
            _selection = new SelectionService(_map, _units, _eventBus);

            _runner = new SimulationRunner(new GameClock());
            _runner.Add(_movement);
            _runner.Add(new CombatSystem(_units, _movement, _combat, _eventBus));
            _runner.Add(new DeathCleanupSystem(_combat));

            _knight = TestDefinitions.CreateUnit("knight", maxHp: 10, damage: 10, attackRange: 1f, attackCooldown: 1f);
            _archer = TestDefinitions.CreateUnit("archer", maxHp: 10, damage: 2, attackRange: 5f, attackCooldown: 1f);
            _tank = TestDefinitions.CreateUnit("tank", maxHp: 25, damage: 1, attackRange: 1f, attackCooldown: 1f);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2), maxHp: 20);
        }

        [TearDown]
        public void TearDown()
        {
            _movement.Dispose();
            _selection.Dispose();
            Object.DestroyImmediate(_knight);
            Object.DestroyImmediate(_archer);
            Object.DestroyImmediate(_tank);
            Object.DestroyImmediate(_house);
        }

        private UnitModel Spawn(UnitDefinition definition, Vector2Int cell, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_units.TrySpawn(definition, cell, faction, out var unit));
            return unit;
        }

        private BuildingModel Place(Vector2Int origin, Faction faction = Faction.Enemy)
        {
            Assert.IsTrue(_buildings.TryPlace(_house, origin, faction, out var building));
            return building;
        }

        private void Run(float seconds)
        {
            for (var elapsed = 0f; elapsed < seconds; elapsed += StepSeconds)
                _runner.Step(StepSeconds);
        }

        [Test]
        public void InRange_HitsAtOnceThenWaitsForCooldown()
        {
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var target = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);
            attacker.SetAttackTarget(target.Id);

            Run(StepSeconds);
            Assert.AreEqual(15, target.CurrentHp, "The first blow doesn't wait.");

            Run(0.5f);
            Assert.AreEqual(15, target.CurrentHp, "Still on cooldown.");

            Run(0.6f);
            Assert.AreEqual(5, target.CurrentHp);
        }

        [Test]
        public void OutOfRange_WalksUntilInRangeThenStops()
        {
            var attacker = Spawn(_knight, new Vector2Int(1, 5));
            var target = Spawn(_tank, new Vector2Int(9, 5), Faction.Enemy);
            attacker.SetAttackTarget(target.Id);

            Run(5f);

            Assert.Less(target.CurrentHp, 25);
            Assert.IsFalse(attacker.IsMoving);
            Assert.LessOrEqual(Vector2.Distance(attacker.Position, target.Position), 1.01f);
        }

        [Test]
        public void Ranged_ShootsFromAfarWithoutWalking()
        {
            var attacker = Spawn(_archer, new Vector2Int(2, 5));
            var start = attacker.Position;
            var target = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);
            attacker.SetAttackTarget(target.Id);

            Run(StepSeconds);

            Assert.AreEqual(23, target.CurrentHp);
            Assert.AreEqual(start, attacker.Position);
        }

        [Test]
        public void TargetWalksAway_AttackerFollows()
        {
            var attacker = Spawn(_knight, new Vector2Int(1, 5));
            var target = Spawn(_tank, new Vector2Int(3, 5), Faction.Enemy);
            attacker.SetAttackTarget(target.Id);
            _movement.SendTo(target, new Vector2Int(12, 5));

            Run(8f);

            Assert.Less(target.CurrentHp, 25, "Catches the target after it stops.");
            Assert.LessOrEqual(Vector2.Distance(attacker.Position, target.Position), 1.01f);
        }

        [Test]
        public void Kill_RemovesTheUnitOnceAndAttackerLetsGo()
        {
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var target = Spawn(_knight, new Vector2Int(6, 5), Faction.Enemy);
            var diedCount = 0;
            _eventBus.Subscribe<UnitDiedEvent>(_ => diedCount++);
            attacker.SetAttackTarget(target.Id);

            Run(StepSeconds * 2f);
            Run(3f);

            Assert.AreEqual(1, diedCount);
            Assert.IsFalse(_units.TryGet(target.Id, out _));
            Assert.IsFalse(attacker.HasAttackTarget);
            Assert.IsFalse(attacker.IsMoving);
        }

        [Test]
        public void ChasedTargetDies_ChaserStops()
        {
            var chaser = Spawn(_knight, new Vector2Int(0, 0));
            var target = Spawn(_knight, new Vector2Int(12, 12), Faction.Enemy);
            chaser.SetAttackTarget(target.Id);
            Run(0.3f);
            Assert.IsTrue(chaser.IsMoving);

            _combat.ApplyDamage(target.Id, 100, GameEntityId.None);
            Run(StepSeconds * 2f);

            Assert.IsFalse(chaser.HasAttackTarget);
            Assert.IsFalse(chaser.IsMoving);
        }

        [Test]
        public void ApplyDamage_HealthStopsAtZero()
        {
            var target = Spawn(_knight, new Vector2Int(5, 5), Faction.Enemy);

            _combat.ApplyDamage(target.Id, 1000, GameEntityId.None);

            Assert.AreEqual(0, target.CurrentHp);
            Assert.IsTrue(target.IsDead);
        }

        [Test]
        public void ApplyDamage_AlreadyDead_DoesNothingAndDiesOnce()
        {
            var target = Spawn(_knight, new Vector2Int(5, 5), Faction.Enemy);
            var damaged = 0;
            var died = 0;
            _eventBus.Subscribe<EntityDamagedEvent>(_ => damaged++);
            _eventBus.Subscribe<UnitDiedEvent>(_ => died++);

            _combat.ApplyDamage(target.Id, 100, GameEntityId.None);
            _combat.ApplyDamage(target.Id, 100, GameEntityId.None);
            _combat.RemoveDead();
            _combat.RemoveDead();

            Assert.AreEqual(1, damaged);
            Assert.AreEqual(1, died);
        }

        [Test]
        public void DeadUnit_WaitingForRemoval_IsNotATarget()
        {
            var target = Spawn(_knight, new Vector2Int(5, 5), Faction.Enemy);
            _combat.ApplyDamage(target.Id, 100, GameEntityId.None);

            Assert.IsFalse(_combat.TryGetTarget(target.Id, Vector2.zero, out _));
        }

        [Test]
        public void IdleVictim_TurnsOnItsAttacker()
        {
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var victim = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);

            _combat.ApplyDamage(victim.Id, 1, attacker.Id);

            Assert.AreEqual(attacker.Id, victim.AttackTarget);
        }

        [Test]
        public void BusyVictim_KeepsWhatItWasDoing()
        {
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var walker = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);
            var fighter = Spawn(_tank, new Vector2Int(7, 5), Faction.Enemy);
            var other = Spawn(_tank, new Vector2Int(8, 5));
            _movement.SendTo(walker, new Vector2Int(6, 12));
            fighter.SetAttackTarget(other.Id);

            _combat.ApplyDamage(walker.Id, 1, attacker.Id);
            _combat.ApplyDamage(fighter.Id, 1, attacker.Id);

            Assert.IsFalse(walker.HasAttackTarget, "A unit on the move keeps walking.");
            Assert.AreEqual(other.Id, fighter.AttackTarget);
        }

        [Test]
        public void TwoUnits_FightEachOtherToTheEnd()
        {
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var defender = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);
            attacker.SetAttackTarget(defender.Id);

            Run(StepSeconds);
            Assert.AreEqual(attacker.Id, defender.AttackTarget);

            Run(4f);
            Assert.IsFalse(_units.TryGet(defender.Id, out _));
            Assert.Less(attacker.CurrentHp, 10, "The defender hit back before dying.");
        }

        [Test]
        public void Building_IsHitFromItsEdge()
        {
            var building = Place(new Vector2Int(5, 5));
            var attacker = Spawn(_knight, new Vector2Int(4, 5));
            attacker.SetAttackTarget(building.Id);

            Run(StepSeconds);

            Assert.AreEqual(10, building.CurrentHp);
            Assert.IsFalse(attacker.IsMoving);
        }

        [Test]
        public void Building_FarAway_AttackerWalksToItsSide()
        {
            var building = Place(new Vector2Int(9, 9));
            var attacker = Spawn(_knight, new Vector2Int(1, 1));
            attacker.SetAttackTarget(building.Id);

            Run(8f);

            Assert.Less(building.CurrentHp, 20);
            Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(attacker.Position)));
        }

        [Test]
        public void Building_Destroyed_FreesItsCellsAndTellsEveryone()
        {
            var building = Place(new Vector2Int(5, 5));
            var attacker = Spawn(_knight, new Vector2Int(4, 5));
            var destroyed = 0;
            RectInt? changedArea = null;
            _eventBus.Subscribe<BuildingDestroyedEvent>(_ => destroyed++);
            _eventBus.Subscribe<GridChangedEvent>(e => changedArea = e.Area);
            attacker.SetAttackTarget(building.Id);

            Run(2f);

            Assert.AreEqual(1, destroyed);
            Assert.IsFalse(_buildings.TryGet(building.Id, out _));
            Assert.IsTrue(_map.IsAreaFree(building.Area));
            Assert.AreEqual(building.Area, changedArea);
            Assert.IsFalse(attacker.HasAttackTarget);
        }

        [Test]
        public void Distance_ToBuilding_IsToTheNearestEdge()
        {
            var building = Place(new Vector2Int(5, 5));

            Assert.IsTrue(_combat.TryGetTarget(building.Id, new Vector2(3f, 6f), out var target));

            Assert.AreEqual(2f, target.Distance, 0.001f);
            Assert.AreEqual(new Vector2(5f, 6f), target.AimPoint);
            Assert.AreEqual(new Vector2Int(5, 6), target.ChaseCell, "The building's cell nearest to the attacker.");
        }

        [Test]
        public void SelectedTarget_Dies_LeavesTheSelection()
        {
            var unit = Spawn(_knight, new Vector2Int(5, 5), Faction.Enemy);
            var changed = 0;
            _selection.Select(unit.Id);
            _eventBus.Subscribe<SelectionChangedEvent>(_ => changed++);

            _combat.ApplyDamage(unit.Id, 100, GameEntityId.None);
            _combat.RemoveDead();

            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void SelectedBuilding_Destroyed_LeavesTheSelection()
        {
            var building = Place(new Vector2Int(5, 5));
            _selection.Select(building.Id);

            _combat.ApplyDamage(building.Id, 100, GameEntityId.None);
            _combat.RemoveDead();

            Assert.IsFalse(_selection.HasSelection);
        }

        [Test]
        public void GroupMember_Dies_OthersStaySelected()
        {
            var first = Spawn(_knight, new Vector2Int(2, 2));
            var second = Spawn(_knight, new Vector2Int(3, 3));
            _selection.UpdateBox(Vector2.zero, new Vector2(5f, 5f));
            _selection.ConfirmBox();

            _combat.ApplyDamage(first.Id, 100, GameEntityId.None);
            _combat.RemoveDead();

            Assert.AreEqual(1, _selection.Selected.Count);
            Assert.IsTrue(_selection.IsSelected(second.Id));
        }

        [Test]
        public void GetHostileAt_FindsEnemiesOnly()
        {
            var enemyUnit = Spawn(_knight, new Vector2Int(2, 2), Faction.Enemy);
            var ownUnit = Spawn(_knight, new Vector2Int(8, 2));
            var enemyBuilding = Place(new Vector2Int(5, 8));
            Place(new Vector2Int(9, 8), Faction.Player);

            Assert.AreEqual(enemyUnit.Id, _combat.GetHostileAt(enemyUnit.Position, Faction.Player));
            Assert.AreEqual(GameEntityId.None, _combat.GetHostileAt(ownUnit.Position, Faction.Player));
            Assert.AreEqual(enemyBuilding.Id, _combat.GetHostileAt(new Vector2(5.5f, 8.5f), Faction.Player));
            Assert.AreEqual(GameEntityId.None, _combat.GetHostileAt(new Vector2(9.5f, 8.5f), Faction.Player));
            Assert.AreEqual(GameEntityId.None, _combat.GetHostileAt(new Vector2(0.5f, 12.5f), Faction.Player));
        }

        [Test]
        public void Pause_StopsTheFight()
        {
            var clock = new GameClock();
            var runner = new SimulationRunner(clock);
            runner.Add(new CombatSystem(_units, _movement, _combat, _eventBus));
            var attacker = Spawn(_knight, new Vector2Int(5, 5));
            var target = Spawn(_tank, new Vector2Int(6, 5), Faction.Enemy);
            attacker.SetAttackTarget(target.Id);

            clock.SetPaused(true);
            runner.Step(StepSeconds);

            Assert.AreEqual(25, target.CurrentHp);
        }
    }
}
