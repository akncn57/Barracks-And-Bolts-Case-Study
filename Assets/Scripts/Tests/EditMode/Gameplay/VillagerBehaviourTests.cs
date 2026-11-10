using System;
using System.Collections.Generic;
using System.Linq;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Construction;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class VillagerBehaviourTests
    {
        private const float Step = 0.1f;

        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private GridSearch _search;
        private SandboxService _sandbox;
        private ResourceService _resources;
        private BuildingService _buildings;
        private UnitService _units;
        private ResourceNodeService _nodes;
        private UnitMovementSystem _movement;
        private GatheringSystem _gathering;
        private ConstructionSystem _construction;
        private FarmingSystem _farming;
        private CombatService _combat;
        private ShelterSystem _shelter;
        private ResourceRoamSystem _roam;
        private RaidSettings _raids;
        private NormalModeSettings _normal;
        private UnitDefinition _villager;
        private UnitDefinition _knight;
        private BuildingDefinition _castle;
        private ResourceNodeDefinition _tree;
        private ResourceNodeDefinition _sheep;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(60, 60, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _search = new GridSearch(_map);
            _sandbox = new SandboxService(new GameSessionSettings { Mode = GameMode.Normal }, new EventBus());
            _resources = new ResourceService(_sandbox, _eventBus);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, _search, ids, _eventBus);
            _nodes = new ResourceNodeService(_map, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _combat = new CombatService(_units, _buildings, _map, _eventBus);

            _villager = Track(TestDefinitions.CreateUnit("villager"));
            SetBool(_villager, "canGather", true);
            _knight = Track(TestDefinitions.CreateUnit("knight"));
            _castle = Track(TestDefinitions.CreateBuilding("castle", new Vector2Int(3, 3)));
            SetBool(_castle, "isDropOff", true);
            _tree = CreateNode("tree", ResourceKind.Wood, 100);
            _sheep = CreateNode("sheep", ResourceKind.Food, 30);
            SetBool(_sheep, "isOneCut", true);
            var sheep = new SerializedObject(_sheep);
            sheep.FindProperty("roamRadius").floatValue = 0.35f;
            sheep.ApplyModifiedPropertiesWithoutUndo();

            _normal = Track(ScriptableObject.CreateInstance<NormalModeSettings>());
            var normal = new SerializedObject(_normal);
            normal.FindProperty("castle").objectReferenceValue = _castle;
            normal.FindProperty("villager").objectReferenceValue = _villager;
            normal.FindProperty("carryCapacity").intValue = 10;
            normal.FindProperty("harvestDuration").floatValue = 1f;
            normal.FindProperty("oneCutDuration").floatValue = 0.5f;
            normal.ApplyModifiedPropertiesWithoutUndo();

            _raids = Track(ScriptableObject.CreateInstance<RaidSettings>());
            var raids = new SerializedObject(_raids);
            raids.FindProperty("shelterRadius").floatValue = 7f;
            raids.ApplyModifiedPropertiesWithoutUndo();

            _gathering = new GatheringSystem(_units, _movement, _nodes, _buildings, _resources, _search, _map, _normal, _eventBus);
            _construction = new ConstructionSystem(_units, _movement, _buildings, _map, _eventBus);
            _farming = new FarmingSystem(_units, _movement, _buildings, _resources, _search, _map, _eventBus);
            _shelter = CreateShelter(_sandbox);
            _roam = new ResourceRoamSystem(_nodes, new GameSessionSettings { Seed = 4 });
        }

        [TearDown]
        public void TearDown()
        {
            _shelter.Dispose();
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private ShelterSystem CreateShelter(SandboxService sandbox)
        {
            return new ShelterSystem(_units, _buildings, _movement, _gathering, _construction, _farming, _search, _map, _raids, _normal, sandbox, _eventBus);
        }

        private T Track<T>(T asset) where T : Object
        {
            _created.Add(asset);
            return asset;
        }

        private static void SetBool(Object asset, string field, bool value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private ResourceNodeDefinition CreateNode(string id, ResourceKind kind, int amount)
        {
            var definition = Track(ScriptableObject.CreateInstance<ResourceNodeDefinition>());
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = id;
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("amount").intValue = amount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private UnitModel SpawnVillager(int x, int y)
        {
            _units.TrySpawn(_villager, new Vector2Int(x, y), Faction.Player, out var unit);
            return unit;
        }

        private UnitModel SpawnEnemy(int x, int y)
        {
            _units.TrySpawn(_knight, new Vector2Int(x, y), Faction.Enemy, out var unit);
            return unit;
        }

        private ResourceNodeModel PlaceNode(int x, int y, ResourceNodeDefinition definition = null)
        {
            _nodes.TryPlace(definition != null ? definition : _tree, new Vector2Int(x, y), out var node);
            return node;
        }

        private void PlaceCastle()
        {
            _buildings.TryPlace(_castle, new Vector2Int(30, 30), Faction.Player, out _);
        }

        private void Tick(ShelterSystem shelter)
        {
            _movement.Tick(Step);
            _gathering.Tick(Step);
            _construction.Tick(Step);
            shelter.Tick(Step);
        }

        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds; time += Step)
                Tick(_shelter);
        }

        private void RunUntil(Func<bool> condition, float maxSeconds = 60f)
        {
            for (var time = 0f; time < maxSeconds && !condition(); time += Step)
                Tick(_shelter);

            Assert.IsTrue(condition(), "It never happened.");
        }

        // ---- Where a villager works

        [Test]
        public void Work_StandsRightAtTheNode()
        {
            var node = PlaceNode(10, 10);
            var villager = SpawnVillager(4, 10);

            _gathering.TryOrder(villager, node);
            RunUntil(() => villager.HarvestingKind != null);

            var distance = Vector2.Distance(villager.Position, _map.AreaToWorldBottomCenter(node.Area));
            Assert.Less(distance, 0.7f);
            Assert.AreEqual(new Vector2Int(9, 10), _map.WorldToCell(villager.Position), "It stays on a walkable cell.");
        }

        [Test]
        public void Work_ComingFromBelow_StillPrefersTheSide()
        {
            var node = PlaceNode(10, 10);
            var villager = SpawnVillager(10, 4);

            _gathering.TryOrder(villager, node);
            RunUntil(() => villager.HarvestingKind != null);

            Assert.AreEqual(10, _map.WorldToCell(villager.Position).y);
        }

        [Test]
        public void Work_BothSidesBlocked_StandsBelow()
        {
            PlaceNode(9, 10);
            PlaceNode(11, 10);
            var node = PlaceNode(10, 10);
            var villager = SpawnVillager(10, 4);

            _gathering.TryOrder(villager, node);
            RunUntil(() => villager.HarvestingKind != null);

            Assert.AreEqual(new Vector2Int(10, 9), _map.WorldToCell(villager.Position));
        }

        [Test]
        public void Work_TwoVillagers_TakeDifferentCells()
        {
            var node = PlaceNode(10, 10);
            var first = SpawnVillager(4, 10);
            var second = SpawnVillager(4, 11);

            _gathering.TryOrder(first, node);
            _gathering.TryOrder(second, node);

            // Each cell is read when its villager starts work. With no drop-off the first one is done before the second
            // arrives, so they are never at work at the same moment.
            Vector2Int? firstCell = null;
            Vector2Int? secondCell = null;
            RunUntil(() =>
            {
                if (firstCell == null && first.HarvestingKind != null)
                    firstCell = _map.WorldToCell(first.Position);

                if (secondCell == null && second.HarvestingKind != null)
                    secondCell = _map.WorldToCell(second.Position);

                return firstCell.HasValue && secondCell.HasValue;
            });

            Assert.AreNotEqual(firstCell, secondCell);
        }

        [Test]
        public void Work_AfterAHarvest_FindsTheWayToTheDropOff()
        {
            PlaceCastle();
            var node = PlaceNode(25, 31);
            var villager = SpawnVillager(20, 31);

            _gathering.TryOrder(villager, node);
            RunUntil(() => _resources.Get(ResourceKind.Wood) >= 10);

            Assert.IsTrue(_gathering.IsGathering(villager.Id));
        }

        // ---- Sheep

        [Test]
        public void Sheep_OneCut_KillsItAndCarriesEverything()
        {
            PlaceCastle();
            var sheep = PlaceNode(25, 31, _sheep);
            var villager = SpawnVillager(20, 31);

            _gathering.TryOrder(villager, sheep);
            RunUntil(() => villager.HarvestingKind != null);
            Assert.AreEqual(1, _nodes.Nodes.Count);

            Run(0.7f);
            Assert.IsEmpty(_nodes.Nodes, "One cut is enough.");
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(25, 31)));

            RunUntil(() => _resources.Get(ResourceKind.Food) > 0);
            Assert.AreEqual(30, _resources.Get(ResourceKind.Food), "More than the carry capacity of 10.");
        }

        [Test]
        public void Sheep_Roams_InsideItsOwnCell()
        {
            var sheep = PlaceNode(10, 10, _sheep);
            var hasMoved = false;

            for (var time = 0f; time < 30f; time += Step)
            {
                _roam.Tick(Step);
                hasMoved |= sheep.Offset != Vector2.zero;
                Assert.LessOrEqual(Mathf.Abs(sheep.Offset.x), 0.35f + 0.001f);
                Assert.GreaterOrEqual(sheep.Offset.y, 0f);
                Assert.LessOrEqual(sheep.Offset.y, 0.7f + 0.001f);
            }

            Assert.IsTrue(hasMoved);
            Assert.IsFalse(_map.IsWalkable(new Vector2Int(10, 10)), "Its cell stays taken.");
        }

        [Test]
        public void Sheep_Held_StandsStill()
        {
            var sheep = PlaceNode(10, 10, _sheep);
            _roam.Tick(Step);
            _roam.Tick(Step);
            var offset = sheep.Offset;

            sheep.Hold();
            _roam.Tick(Step);

            Assert.IsFalse(sheep.IsWalking);
            Assert.AreEqual(offset, sheep.Offset);
        }

        [Test]
        public void Tree_DoesNotRoam()
        {
            var tree = PlaceNode(10, 10);

            for (var time = 0f; time < 10f; time += Step)
                _roam.Tick(Step);

            Assert.AreEqual(Vector2.zero, tree.Offset);
        }

        // ---- Idle

        [Test]
        public void Idle_OnlyWithNothingToDo()
        {
            var node = PlaceNode(10, 10);
            var villager = SpawnVillager(4, 10);
            Assert.IsTrue(villager.IsIdle);

            _gathering.TryOrder(villager, node);
            Assert.IsFalse(villager.IsIdle, "On its way to work.");

            RunUntil(() => villager.HarvestingKind != null);
            Assert.IsFalse(villager.IsIdle, "Working.");

            _gathering.Cancel(villager.Id);
            Assert.IsTrue(villager.IsIdle);

            villager.SetAttackTarget(new GameEntityId(999));
            Assert.IsFalse(villager.IsIdle, "Fighting.");
        }

        [Test]
        public void Idle_NoDropOff_AfterTheJobEnds()
        {
            var node = PlaceNode(10, 10);
            var villager = SpawnVillager(4, 10);

            _gathering.TryOrder(villager, node);
            RunUntil(() => !_gathering.IsGathering(villager.Id));

            Assert.IsTrue(villager.IsIdle);
        }

        // ---- Shelter

        [Test]
        public void Shelter_EnemyFarAway_KeepsWorking()
        {
            PlaceCastle();
            var node = PlaceNode(25, 31);
            var villager = SpawnVillager(20, 31);
            SpawnEnemy(55, 55);

            _gathering.TryOrder(villager, node);
            Run(3f);

            Assert.IsTrue(_gathering.IsGathering(villager.Id));
            Assert.IsTrue(_units.TryGet(villager.Id, out _));
        }

        [Test]
        public void Shelter_EnemyClose_RunsToTheCastleAndGoesInside()
        {
            PlaceCastle();
            var node = PlaceNode(25, 31);
            var villager = SpawnVillager(20, 31);
            _gathering.TryOrder(villager, node);
            SpawnEnemy(18, 31);

            RunUntil(() => !_units.TryGet(villager.Id, out _));

            Assert.IsTrue(_units.Sheltered.Contains(villager));
            Assert.IsFalse(_gathering.IsGathering(villager.Id));
            Assert.IsFalse(villager.IsMoving);
        }

        [Test]
        public void Shelter_Inside_NothingCanSeeOrHitIt()
        {
            PlaceCastle();
            var population = new PopulationService(_sandbox, _units, _buildings);
            var villager = SpawnVillager(26, 31);
            SpawnEnemy(22, 31);
            using var selection = new SelectionService(_units, _buildings, _eventBus);
            selection.Select(villager.Id);

            RunUntil(() => !_units.TryGet(villager.Id, out _));

            Assert.IsFalse(_units.GetUnitAt(villager.Position).IsValid);
            Assert.IsFalse(_units.HasUnitAt(_map.WorldToCell(villager.Position)));
            Assert.IsFalse(_combat.TryGetTarget(villager.Id, villager.Position, out _));
            Assert.AreNotEqual(villager.Id, _combat.FindNearestHostile(villager.Position, Faction.Enemy, 100f));
            Assert.IsFalse(selection.IsSelected(villager.Id));
            Assert.AreEqual(_villager.PopulationCost, population.Used, "It still takes up room.");
        }

        [Test]
        public void Shelter_CarryingALoad_CountsItAtTheCastle()
        {
            PlaceCastle();
            var node = PlaceNode(25, 31);
            var villager = SpawnVillager(20, 31);
            _gathering.TryOrder(villager, node);
            RunUntil(() => node.Remaining < 100);
            Assert.AreEqual(0, _resources.Get(ResourceKind.Wood));

            SpawnEnemy(22, 26);
            RunUntil(() => !_units.TryGet(villager.Id, out _));

            Assert.AreEqual(10, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Shelter_EnemiesDead_ComesOutAndGoesBackToWork()
        {
            PlaceCastle();
            var node = PlaceNode(25, 31);
            var villager = SpawnVillager(20, 31);
            _gathering.TryOrder(villager, node);
            var enemy = SpawnEnemy(18, 31);
            RunUntil(() => !_units.TryGet(villager.Id, out _));

            _units.Remove(enemy.Id);
            RunUntil(() => _units.TryGet(villager.Id, out _));

            Assert.IsTrue(_gathering.IsGathering(villager.Id));
            Assert.IsEmpty(_units.Sheltered);
            Assert.IsFalse(villager.IsIdle);
        }

        [Test]
        public void Shelter_Builder_GoesBackToTheSite()
        {
            PlaceCastle();
            var house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            var serialized = new SerializedObject(house);
            serialized.FindProperty("buildTime").floatValue = 1000f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _buildings.TryPlace(house, new Vector2Int(24, 31), Faction.Player, out var site, asSite: true);
            var villager = SpawnVillager(20, 31);
            _construction.TryOrder(villager, site);
            var enemy = SpawnEnemy(18, 31);
            RunUntil(() => !_units.TryGet(villager.Id, out _));
            Assert.IsFalse(_construction.IsBuilding(villager.Id), "It left the site to hide.");

            _units.Remove(enemy.Id);
            RunUntil(() => _units.TryGet(villager.Id, out _));

            Assert.IsTrue(_construction.IsBuilding(villager.Id));
            Assert.IsFalse(villager.IsIdle);
        }

        [Test]
        public void Shelter_NoJobBefore_ComesOutIdle()
        {
            PlaceCastle();
            var villager = SpawnVillager(26, 31);
            var enemy = SpawnEnemy(22, 31);
            RunUntil(() => !_units.TryGet(villager.Id, out _));

            _units.Remove(enemy.Id);
            RunUntil(() => _units.TryGet(villager.Id, out _));

            Assert.IsTrue(villager.IsIdle);
        }

        [Test]
        public void Shelter_EnemiesDeadOnTheWay_TurnsBackToWork()
        {
            PlaceCastle();
            var node = PlaceNode(5, 31);
            var villager = SpawnVillager(8, 31);
            _gathering.TryOrder(villager, node);
            var enemy = SpawnEnemy(12, 31);
            RunUntil(() => !_gathering.IsGathering(villager.Id));

            _units.Remove(enemy.Id);
            RunUntil(() => _gathering.IsGathering(villager.Id));

            Assert.IsTrue(_units.TryGet(villager.Id, out _));
            Assert.IsEmpty(_units.Sheltered);
        }

        [Test]
        public void Shelter_PlayerOrder_BeatsTheFlight()
        {
            PlaceCastle();
            var commands = new UnitCommandService(_units, _movement, _combat, _search, _map, _gathering, _construction, _farming, _buildings, _nodes, _eventBus);
            var villager = SpawnVillager(8, 31);
            SpawnEnemy(12, 31);
            Run(0.5f);
            Assert.IsTrue(villager.IsMoving, "It started running.");

            commands.MoveTo(new[] { villager.Id }, _map.CellToWorldCenter(new Vector2Int(8, 25)));
            RunUntil(() => !villager.IsMoving);

            Assert.AreEqual(new Vector2Int(8, 25), _map.WorldToCell(villager.Position));
            Assert.IsTrue(_units.TryGet(villager.Id, out _));
        }

        [Test]
        public void Shelter_Soldier_StaysOut()
        {
            PlaceCastle();
            _units.TrySpawn(_knight, new Vector2Int(26, 31), Faction.Player, out var soldier);
            SpawnEnemy(22, 31);

            Run(3f);

            Assert.IsTrue(_units.TryGet(soldier.Id, out _));
            Assert.IsFalse(soldier.IsMoving);
        }

        [Test]
        public void Shelter_Sandbox_DoesNothing()
        {
            PlaceCastle();
            var sandbox = new SandboxService(new GameSessionSettings { Mode = GameMode.Sandbox }, new EventBus());
            using var shelter = CreateShelter(sandbox);
            var villager = SpawnVillager(26, 31);
            SpawnEnemy(22, 31);

            for (var time = 0f; time < 3f; time += Step)
            {
                _movement.Tick(Step);
                shelter.Tick(Step);
            }

            Assert.IsTrue(_units.TryGet(villager.Id, out _));
            Assert.IsFalse(villager.IsMoving);
        }

        [Test]
        public void BringOut_CellTaken_UsesTheNextOne()
        {
            var villager = SpawnVillager(10, 10);
            _units.Shelter(villager);
            SpawnEnemy(10, 10);

            _units.BringOut(villager, new Vector2Int(10, 10));

            Assert.AreNotEqual(new Vector2Int(10, 10), _map.WorldToCell(villager.Position));
            Assert.IsTrue(_units.TryGet(villager.Id, out _));
        }
    }
}
