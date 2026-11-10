using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class NormalModeTests
    {
        private const float Step = 0.1f;

        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private ResourceService _resources;
        private BuildingService _buildings;
        private UnitService _units;
        private ResourceNodeService _nodes;
        private UnitMovementSystem _movement;
        private GatheringSystem _gathering;
        private NormalModeSettings _settings;
        private UnitDefinition _villager;
        private UnitDefinition _knight;
        private BuildingDefinition _castle;
        private ResourceNodeDefinition _tree;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(16, 16, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var search = new GridSearch(_map);
            _resources = new ResourceService(CreateSandbox(GameMode.Normal), _eventBus);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, search, ids, _eventBus);
            _nodes = new ResourceNodeService(_map, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);

            _villager = Track(TestDefinitions.CreateUnit("villager"));
            SetBool(_villager, "canGather", true);
            _knight = Track(TestDefinitions.CreateUnit("knight"));
            _castle = Track(TestDefinitions.CreateBuilding("castle", new Vector2Int(2, 2)));
            SetBool(_castle, "isDropOff", true);
            _tree = CreateNode("tree", ResourceKind.Wood, 25);

            _settings = Track(ScriptableObject.CreateInstance<NormalModeSettings>());
            var serialized = new SerializedObject(_settings);
            serialized.FindProperty("castle").objectReferenceValue = _castle;
            serialized.FindProperty("villager").objectReferenceValue = _villager;
            serialized.FindProperty("startingVillagers").intValue = 2;
            serialized.FindProperty("carryCapacity").intValue = 10;
            serialized.FindProperty("harvestDuration").floatValue = 1f;
            serialized.FindProperty("nextNodeRadius").floatValue = 6f;
            var start = serialized.FindProperty("startingResources");
            start.arraySize = 1;
            start.GetArrayElementAtIndex(0).FindPropertyRelative("kind").enumValueIndex = (int)ResourceKind.Wood;
            start.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = 40;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _gathering = new GatheringSystem(_units, _movement, _nodes, _buildings, _resources, search, _map, _settings, _eventBus);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private static SandboxService CreateSandbox(GameMode mode)
        {
            return new SandboxService(new GameSessionSettings { Mode = mode }, new EventBus());
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

        private static void SetCost(Object asset, ResourceKind kind, int amount)
        {
            var serialized = new SerializedObject(asset);
            var cost = serialized.FindProperty("cost");
            cost.arraySize = 1;
            cost.GetArrayElementAtIndex(0).FindPropertyRelative("kind").enumValueIndex = (int)kind;
            cost.GetArrayElementAtIndex(0).FindPropertyRelative("amount").intValue = amount;
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

        private UnitModel SpawnVillager(Vector2Int cell)
        {
            _units.TrySpawn(_villager, cell, Faction.Player, out var unit);
            return unit;
        }

        private ResourceNodeModel PlaceNode(Vector2Int cell, ResourceNodeDefinition definition = null)
        {
            _nodes.TryPlace(definition != null ? definition : _tree, cell, out var node);
            return node;
        }

        private void RunUntilCarrying(UnitModel villager)
        {
            for (var time = 0f; time < 5f && !villager.CarriedKind.HasValue; time += Step)
                Run(Step);

            Assert.IsTrue(villager.CarriedKind.HasValue, "It never picked up a load.");
        }

        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds; time += Step)
            {
                _movement.Tick(Step);
                _gathering.Tick(Step);
            }
        }

        // ---- ResourceService

        [Test]
        public void Resources_Normal_SpendsOnlyWhatItHas()
        {
            _resources.Add(ResourceKind.Wood, 50);
            var cost = new[] { new ResourceCost(ResourceKind.Wood, 30) };

            Assert.IsTrue(_resources.TrySpend(cost));
            Assert.AreEqual(20, _resources.Get(ResourceKind.Wood));
            Assert.IsFalse(_resources.TrySpend(cost));
            Assert.AreEqual(20, _resources.Get(ResourceKind.Wood), "A refused price takes nothing.");
        }

        [Test]
        public void Resources_PriceOfTwoKinds_TakesNothingIfOneIsShort()
        {
            _resources.Add(ResourceKind.Wood, 50);
            var cost = new[] { new ResourceCost(ResourceKind.Wood, 30), new ResourceCost(ResourceKind.Stone, 1) };

            Assert.IsFalse(_resources.TrySpend(cost));
            Assert.AreEqual(50, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Resources_Sandbox_EverythingIsFree()
        {
            var resources = new ResourceService(CreateSandbox(GameMode.Sandbox), _eventBus);
            var cost = new[] { new ResourceCost(ResourceKind.Gold, 9999) };

            Assert.IsTrue(resources.CanAfford(cost));
            Assert.IsTrue(resources.TrySpend(cost));
            Assert.AreEqual(0, resources.Get(ResourceKind.Gold));
        }

        // ---- Prices

        [Test]
        public void Placement_TooExpensive_IsRefusedWithItsOwnReason()
        {
            var house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            SetCost(house, ResourceKind.Wood, 30);
            var placement = new PlacementService(_map, _buildings, _units, new ResourceNodeService(_map, new GameEntityIdGenerator(), _eventBus), _resources, TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            placement.Begin(house, Faction.Player);
            placement.MoveTo(new Vector2(5f, 5f));

            Assert.AreEqual(PlacementBlock.NotEnoughResources, placement.Block);
            Assert.IsFalse(placement.TryConfirm(keepPlacing: false));
            Assert.AreEqual(0, _buildings.Count);
        }

        [Test]
        public void Placement_Affordable_PlacesAndCharges()
        {
            var house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            SetCost(house, ResourceKind.Wood, 30);
            _resources.Add(ResourceKind.Wood, 50);
            var placement = new PlacementService(_map, _buildings, _units, new ResourceNodeService(_map, new GameEntityIdGenerator(), _eventBus), _resources, TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            placement.Begin(house, Faction.Player);
            placement.MoveTo(new Vector2(5f, 5f));

            Assert.IsTrue(placement.TryConfirm(keepPlacing: false));
            Assert.AreEqual(20, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Placement_OnBlockedCells_DoesNotCharge()
        {
            var house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            SetCost(house, ResourceKind.Wood, 30);
            _resources.Add(ResourceKind.Wood, 50);
            _map.SetTerrainWalkable(new Vector2Int(5, 5), false);
            var placement = new PlacementService(_map, _buildings, _units, new ResourceNodeService(_map, new GameEntityIdGenerator(), _eventBus), _resources, TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            placement.Begin(house, Faction.Player);
            placement.MoveTo(new Vector2(5f, 5f));

            Assert.IsFalse(placement.TryConfirm(keepPlacing: false));
            Assert.AreEqual(50, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Production_ChargesAndRefusesWhenShort()
        {
            SetCost(_knight, ResourceKind.Food, 20);
            var barracks = Track(TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), new Vector2Int(1, -1), _knight));
            _buildings.TryPlace(barracks, new Vector2Int(5, 5), Faction.Player, out var building);
            var production = new UnitProductionService(_units, _resources, TestDefinitions.CreateFreePopulation(_units, _buildings, _eventBus));
            _resources.Add(ResourceKind.Food, 30);

            Assert.IsTrue(production.TryProduce(building, _knight, out _));
            Assert.AreEqual(10, _resources.Get(ResourceKind.Food));
            Assert.AreEqual(ProductionBlock.NotEnoughResources, production.GetBlock(building, _knight));
            Assert.IsFalse(production.TryProduce(building, _knight, out _));
            Assert.AreEqual(1, _units.Count);
        }

        // ---- Start

        [Test]
        public void Starter_Normal_PlacesCastleVillagersAndResources()
        {
            new NormalModeStarter(CreateSandbox(GameMode.Normal), _settings, _resources, _buildings, _units, _map, new GameSessionSettings()).Start();

            Assert.AreEqual(1, _buildings.Count);
            Assert.AreEqual(2, _units.Count);
            Assert.AreEqual(40, _resources.Get(ResourceKind.Wood));
        }

        [TestCase(Difficulty.Easy, 60)]
        [TestCase(Difficulty.Normal, 40)]
        [TestCase(Difficulty.Hard, 30)]
        public void Starter_Difficulty_ScalesTheStartingResources(Difficulty difficulty, int wood)
        {
            new NormalModeStarter(CreateSandbox(GameMode.Normal), _settings, _resources, _buildings, _units, _map,
                new GameSessionSettings { Difficulty = difficulty }).Start();

            Assert.AreEqual(wood, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Starter_Sandbox_LeavesMapEmpty()
        {
            new NormalModeStarter(CreateSandbox(GameMode.Sandbox), _settings, _resources, _buildings, _units, _map, new GameSessionSettings()).Start();

            Assert.AreEqual(0, _buildings.Count);
            Assert.AreEqual(0, _units.Count);
        }

        // ---- Gathering

        [Test]
        public void Gather_FullLoop_BringsResourcesToDropOff()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var node = PlaceNode(new Vector2Int(8, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            Assert.IsTrue(_gathering.TryOrder(villager, node));
            Run(12f);

            Assert.GreaterOrEqual(_resources.Get(ResourceKind.Wood), 10);
            Assert.Less(node.Remaining, 25);
        }

        [Test]
        public void Gather_Soldier_IsRefused()
        {
            var node = PlaceNode(new Vector2Int(8, 2));
            _units.TrySpawn(_knight, new Vector2Int(5, 2), Faction.Player, out var knight);

            Assert.IsFalse(_gathering.TryOrder(knight, node));
        }

        [Test]
        public void Gather_NodeRunsOut_IsRemovedAndCellFreed()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var small = CreateNode("small", ResourceKind.Wood, 5);
            var node = PlaceNode(new Vector2Int(6, 2), small);
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            Run(6f);

            Assert.IsEmpty(_nodes.Nodes);
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(6, 2)));
            Assert.AreEqual(5, _resources.Get(ResourceKind.Wood), "Only what was left in the node.");
        }

        [Test]
        public void Gather_NodeRunsOut_MovesToNearbySameKind()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var small = CreateNode("small", ResourceKind.Wood, 5);
            var first = PlaceNode(new Vector2Int(6, 2), small);
            var second = PlaceNode(new Vector2Int(9, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, first);
            Run(14f);

            Assert.Less(second.Remaining, 25);
            Assert.IsTrue(_gathering.IsGathering(villager.Id));
        }

        [Test]
        public void Gather_NoOtherNodeNearby_Stops()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var small = CreateNode("small", ResourceKind.Wood, 5);
            var node = PlaceNode(new Vector2Int(6, 2), small);
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            Run(8f);

            Assert.IsFalse(_gathering.IsGathering(villager.Id));
            Assert.IsNull(villager.HarvestingKind);
        }

        [Test]
        public void Gather_NoDropOff_StopsAfterHarvest()
        {
            var node = PlaceNode(new Vector2Int(6, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            Run(3f);

            Assert.IsFalse(_gathering.IsGathering(villager.Id));
            Assert.AreEqual(0, _resources.Get(ResourceKind.Wood));
            Assert.IsNull(villager.CarriedKind, "The load is lost, so no icon either.");
        }

        [Test]
        public void Gather_AfterHarvest_CarriesTheKindUntilDelivered()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var node = PlaceNode(new Vector2Int(10, 2));
            var villager = SpawnVillager(new Vector2Int(9, 2));
            _gathering.TryOrder(villager, node);

            Assert.IsNull(villager.CarriedKind, "Nothing in hand on the way to the node.");
            RunUntilCarrying(villager);
            Assert.AreEqual(ResourceKind.Wood, villager.CarriedKind);
            Assert.AreEqual(0, _resources.Get(ResourceKind.Wood));

            for (var time = 0f; time < 12f && villager.CarriedKind.HasValue; time += Step)
                Run(Step);

            Assert.IsNull(villager.CarriedKind);
            Assert.AreEqual(10, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Gather_CancelWhileCarrying_DropsTheLoad()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var node = PlaceNode(new Vector2Int(10, 2));
            var villager = SpawnVillager(new Vector2Int(9, 2));
            _gathering.TryOrder(villager, node);
            RunUntilCarrying(villager);

            _gathering.Cancel(villager.Id);

            Assert.IsNull(villager.CarriedKind);
        }

        [Test]
        public void Gather_SuspendWhileCarrying_KeepsTheLoadUntilItIsDelivered()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var node = PlaceNode(new Vector2Int(10, 2));
            var villager = SpawnVillager(new Vector2Int(9, 2));
            _gathering.TryOrder(villager, node);
            RunUntilCarrying(villager);

            _gathering.Suspend(villager.Id);
            Assert.AreEqual(ResourceKind.Wood, villager.CarriedKind, "Still in hand while it runs.");

            _gathering.DeliverLoad(villager.Id);
            Assert.IsNull(villager.CarriedKind);
            Assert.AreEqual(10, _resources.Get(ResourceKind.Wood));
        }

        [Test]
        public void Gather_CancelWhileSuspended_DropsTheLoad()
        {
            _buildings.TryPlace(_castle, new Vector2Int(2, 2), Faction.Player, out _);
            var node = PlaceNode(new Vector2Int(10, 2));
            var villager = SpawnVillager(new Vector2Int(9, 2));
            _gathering.TryOrder(villager, node);
            RunUntilCarrying(villager);
            _gathering.Suspend(villager.Id);

            _gathering.Cancel(villager.Id);

            Assert.IsNull(villager.CarriedKind);
        }

        [Test]
        public void Gather_DeadUnit_OrderEnds()
        {
            var node = PlaceNode(new Vector2Int(8, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            _units.Remove(villager.Id);
            Run(0.2f);

            Assert.IsFalse(_gathering.IsGathering(villager.Id));
        }

        [Test]
        public void Gather_AttackOrder_EndsGathering()
        {
            var node = PlaceNode(new Vector2Int(8, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            villager.SetAttackTarget(new GameEntityId(999));
            Run(0.2f);

            Assert.IsFalse(_gathering.IsGathering(villager.Id));
        }

        [Test]
        public void Gather_Cancel_StopsTheWorkAnimation()
        {
            var node = PlaceNode(new Vector2Int(6, 2));
            var villager = SpawnVillager(new Vector2Int(5, 2));

            _gathering.TryOrder(villager, node);
            for (var time = 0f; time < 0.9f && villager.HarvestingKind == null; time += Step)
                Run(Step);

            Assert.AreEqual(ResourceKind.Wood, villager.HarvestingKind);

            _gathering.Cancel(villager.Id);

            Assert.IsNull(villager.HarvestingKind);
            Assert.IsFalse(_gathering.IsGathering(villager.Id));
        }

        [Test]
        public void Gather_NodeOnHighGround_StandsOnTheSameLevel()
        {
            foreach (var cell in new RectInt(6, 0, 10, 16).allPositionsWithin)
                _map.SetLevel(cell, 1);

            _map.SetRamp(new Vector2Int(5, 8), true);
            var node = PlaceNode(new Vector2Int(6, 2));
            var villager = SpawnVillager(new Vector2Int(4, 2));

            _gathering.TryOrder(villager, node);

            // The way up goes around through the ramp; stops as soon as the work starts.
            for (var time = 0f; time < 12f && villager.HarvestingKind == null; time += Step)
                Run(Step);

            Assert.AreEqual(ResourceKind.Wood, villager.HarvestingKind);
            Assert.AreEqual(1, _map.GetLevel(_map.WorldToCell(villager.Position)));
        }

        [Test]
        public void FindCellBeside_PicksNearestFreeCellOnSameLevel()
        {
            var search = new GridSearch(_map);

            Assert.IsTrue(search.TryFindCellBeside(new RectInt(5, 5, 1, 1), new Vector2(0f, 5.5f), out var cell));
            Assert.AreEqual(new Vector2Int(4, 5), cell);
        }

        [Test]
        public void FindCellBeside_Surrounded_ReturnsFalse()
        {
            var map = TestMaps.FromRows("###", "#.#", "###");

            Assert.IsFalse(new GridSearch(map).TryFindCellBeside(new RectInt(1, 1, 1, 1), Vector2.zero, out _));
        }
    }
}
