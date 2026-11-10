using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Construction;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Gathering;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class FarmHungerRallyTests
    {
        private const float Step = 0.1f;

        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private SandboxService _normalGame;
        private ResourceService _resources;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitMovementSystem _movement;
        private CombatService _combat;
        private FarmingSystem _farming;
        private UnitCommandService _commands;
        private UnitDefinition _villager;
        private UnitDefinition _knight;
        private BuildingDefinition _castle;
        private BuildingDefinition _farm;
        private NormalModeSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(24, 24, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var search = new GridSearch(_map);
            _normalGame = new SandboxService(new GameSessionSettings { Mode = GameMode.Normal }, _eventBus);
            _resources = new ResourceService(_normalGame, _eventBus);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, search, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _combat = new CombatService(_units, _buildings, _map, _eventBus);
            _farming = new FarmingSystem(_units, _movement, _buildings, _resources, search, _map, _eventBus);
            var nodes = new ResourceNodeService(_map, ids, _eventBus);
            _settings = Track(ScriptableObject.CreateInstance<NormalModeSettings>());
            var gathering = new GatheringSystem(_units, _movement, nodes, _buildings, _resources, search, _map, _settings, _eventBus);
            _commands = new UnitCommandService(_units, _movement, _combat, search, _map, gathering,
                new ConstructionSystem(_units, _movement, _buildings, _map, _eventBus), _farming, _buildings, nodes, _eventBus);

            _villager = Track(TestDefinitions.CreateUnit("villager"));
            Set(_villager, "canGather", true);
            _knight = Track(TestDefinitions.CreateUnit("knight"));
            _castle = Track(TestDefinitions.CreateBuilding("castle", new Vector2Int(3, 3)));
            Set(_castle, "isDropOff", true);
            _farm = Track(TestDefinitions.CreateBuilding("farm", new Vector2Int(3, 3)));
            Set(_farm, "farmYield", 5);
            Set(_farm, "farmHarvestDuration", 2f);
            Set(_farm, "farmWorkers", 1);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private T Track<T>(T asset) where T : Object
        {
            _created.Add(asset);
            return asset;
        }

        private static void Set(Object asset, string field, bool value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object asset, string field, int value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Set(Object asset, string field, float value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private BuildingModel Place(BuildingDefinition definition, int x, int y, bool asSite = false, Faction faction = Faction.Player)
        {
            _buildings.TryPlace(definition, new Vector2Int(x, y), faction, out var building, asSite);
            return building;
        }

        private UnitModel Spawn(UnitDefinition unit, int x, int y, Faction faction = Faction.Player)
        {
            _units.TrySpawn(unit, new Vector2Int(x, y), faction, out var spawned);
            return spawned;
        }

        private void RunUntil(Func<bool> condition, float maxSeconds = 60f)
        {
            for (var time = 0f; time < maxSeconds && !condition(); time += Step)
            {
                _movement.Tick(Step);
                _farming.Tick(Step);
            }

            Assert.IsTrue(condition(), "It never happened.");
        }

        // ---- Farm

        [Test]
        public void Farm_VillagerHarvestsAndCarriesFoodToTheDropOff_AndComesBack()
        {
            Place(_castle, 2, 10);
            var farm = Place(_farm, 12, 10);
            var villager = Spawn(_villager, 8, 11);

            Assert.IsTrue(_farming.TryOrder(villager, farm));
            RunUntil(() => _resources.Get(ResourceKind.Food) == 5);

            Assert.IsTrue(_farming.IsFarming(villager.Id), "Still on the job.");
            RunUntil(() => _resources.Get(ResourceKind.Food) == 10);
        }

        [Test]
        public void Farm_VillagerCarriesFood_UntilItReachesTheDropOff()
        {
            Place(_castle, 2, 10);
            var farm = Place(_farm, 12, 10);
            var villager = Spawn(_villager, 8, 11);
            _farming.TryOrder(villager, farm);

            RunUntil(() => villager.CarriedKind.HasValue);
            Assert.AreEqual(ResourceKind.Food, villager.CarriedKind);
            Assert.AreEqual(0, _resources.Get(ResourceKind.Food));

            RunUntil(() => !villager.CarriedKind.HasValue);
            Assert.AreEqual(5, _resources.Get(ResourceKind.Food));

            _farming.Cancel(villager.Id);
            Assert.IsNull(villager.CarriedKind);
        }

        [Test]
        public void Farm_VillagerWorksInsideTheFence_AndLeavesItForTheDropOff()
        {
            Place(_castle, 2, 10);
            var farm = Place(_farm, 12, 10);
            var villager = Spawn(_villager, 8, 11);
            _farming.TryOrder(villager, farm);

            RunUntil(() => villager.HarvestingKind.HasValue);
            Assert.IsTrue(farm.Area.Contains(_map.WorldToCell(villager.Position)), "Working among the crops.");
            Assert.IsTrue(villager.IsInField, "Drawn over the field.");

            RunUntil(() => !farm.Area.Contains(_map.WorldToCell(villager.Position)));
            Assert.IsFalse(villager.IsInField, "Past the fence again.");
        }

        [Test]
        public void Farm_EnemyAtTheFence_ReachesTheVillagerInside()
        {
            var farm = Place(_farm, 12, 10);
            var villager = Spawn(_villager, 11, 11);
            _farming.TryOrder(villager, farm);
            RunUntil(() => villager.HarvestingKind.HasValue);

            _combat.TryGetTarget(villager.Id, new Vector2(2.5f, 20.5f), out var fromAfar);
            Assert.AreEqual(new Vector2Int(11, 11), fromAfar.ChaseCell, "Chases to the cell outside the nearest fence.");

            _combat.TryGetTarget(villager.Id, _map.CellToWorldCenter(fromAfar.ChaseCell), out var atTheFence);
            Assert.LessOrEqual(atTheFence.Distance, 1f, "A knight at the fence can hit it.");
        }

        [Test]
        public void Farm_TakesOnlyItsWorkers()
        {
            var farm = Place(_farm, 12, 10);

            Assert.IsTrue(_farming.TryOrder(Spawn(_villager, 8, 11), farm));
            Assert.IsFalse(_farming.TryOrder(Spawn(_villager, 8, 13), farm), "One villager at a time.");
            Assert.IsFalse(_farming.TryOrder(Spawn(_knight, 8, 15), farm), "Soldiers don't farm.");
        }

        [Test]
        public void Farm_WhileASite_IsBuiltFirst()
        {
            var site = Place(_farm, 12, 10, asSite: true);
            var villager = Spawn(_villager, 8, 11);

            Assert.IsFalse(_farming.TryOrder(villager, site));
            Assert.IsTrue(_commands.CommandAt(new[] { villager.Id }, new Vector2(13.5f, 11.5f)));
            Assert.IsFalse(_farming.IsFarming(villager.Id), "A right click on the site sends it to build.");
        }

        [Test]
        public void Farm_RightClick_SendsAVillagerToWork()
        {
            var farm = Place(_farm, 12, 10);
            var villager = Spawn(_villager, 8, 11);

            _commands.CommandAt(new[] { villager.Id }, new Vector2(13.5f, 11.5f));

            Assert.IsTrue(_farming.IsFarming(villager.Id));
            Assert.AreEqual(ToolCursor.Axe, ToolCursorPicker.Pick(null, farm), "Its cursor is the axe.");
        }

        [Test]
        public void Farm_Tooltip_ShowsWhatItGives()
        {
            StringAssert.Contains("Food: 5 every 2s, 1 worker(s)", TooltipText.ForBuilding(_farm));
        }

        // ---- Hunger

        private HungerSystem CreateHunger(Difficulty difficulty, SandboxService mode = null)
        {
            mode ??= _normalGame;
            return new HungerSystem(new PopulationService(mode, _units, _buildings), _resources, _units, _combat, _settings,
                new GameSessionSettings { Difficulty = difficulty }, mode, _eventBus);
        }

        [Test]
        public void Hunger_EachUnitEatsInTheMorning_ByDifficulty()
        {
            Spawn(_villager, 5, 5);
            Spawn(_villager, 6, 5);
            _resources.Add(ResourceKind.Food, 20);
            var normal = CreateHunger(Difficulty.Normal);

            _eventBus.Publish(new DayStartedEvent(1));
            Assert.AreEqual(20, _resources.Get(ResourceKind.Food), "Nobody eats on the first morning.");

            _eventBus.Publish(new DayStartedEvent(2));
            Assert.AreEqual(16, _resources.Get(ResourceKind.Food), "Normal: 2 each.");
            normal.Dispose();

            var easy = CreateHunger(Difficulty.Easy);
            _eventBus.Publish(new DayStartedEvent(3));
            Assert.AreEqual(14, _resources.Get(ResourceKind.Food), "Easy: 1 each.");
            easy.Dispose();
        }

        [Test]
        public void Hunger_NotEnoughFood_UnitsLoseHealth_UntilFoodComes()
        {
            var villager = Spawn(_villager, 5, 5);
            _resources.Add(ResourceKind.Food, 1);
            var hunger = CreateHunger(Difficulty.Normal);
            var changes = 0;
            _eventBus.Subscribe<HungerChangedEvent>(_ => changes++);

            _eventBus.Publish(new DayStartedEvent(2));
            Assert.AreEqual(0, _resources.Get(ResourceKind.Food), "What there was is eaten.");
            Assert.IsTrue(hunger.IsStarving);
            Assert.AreEqual(1, hunger.FoodOwed);

            hunger.Tick(10f);
            Assert.AreEqual(9, villager.CurrentHp, "One HP every 10 seconds on Normal.");
            Assert.IsFalse(villager.HasAttackTarget, "Hunger has no attacker to turn on.");

            _resources.Add(ResourceKind.Food, 5);
            hunger.Tick(10f);
            Assert.IsFalse(hunger.IsStarving);
            Assert.AreEqual(4, _resources.Get(ResourceKind.Food), "The debt is paid first.");
            Assert.AreEqual(9, villager.CurrentHp, "No more loss once fed.");
            Assert.AreEqual(2, changes);
            hunger.Dispose();
        }

        [Test]
        public void Hunger_Sandbox_HasNone()
        {
            var sandbox = new SandboxService(new GameSessionSettings { Mode = GameMode.Sandbox }, _eventBus);
            Spawn(_villager, 5, 5);
            var hunger = CreateHunger(Difficulty.Hard, sandbox);

            _eventBus.Publish(new DayStartedEvent(2));

            Assert.IsFalse(hunger.IsStarving);
            hunger.Dispose();
        }

        // ---- Rally point

        private RallyPointService CreateRally(out UnitProductionService production)
        {
            production = new UnitProductionService(_units, TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreePopulation(_units, _buildings, _eventBus));
            return new RallyPointService(_buildings, production, _commands);
        }

        [Test]
        public void RallyPoint_NewUnitsWalkThere()
        {
            var rally = CreateRally(out var production);
            var barracks = Place(Track(TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), new Vector2Int(1, -1), _knight)), 5, 5);

            Assert.IsTrue(rally.TrySet(barracks.Id, new Vector2(15.5f, 15.5f)));
            production.TryProduce(barracks, _knight, out var knight);

            Assert.IsTrue(knight.IsMoving);
            Assert.AreEqual(new Vector2Int(15, 15), _map.WorldToCell(knight.Destination));
            rally.Dispose();
        }

        [Test]
        public void RallyPoint_OnlyOnThePlayersFinishedProducers()
        {
            var rally = CreateRally(out _);
            var producer = Track(TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), new Vector2Int(1, -1), _knight));
            var house = Place(Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2))), 1, 1);
            var enemy = Place(producer, 5, 5, faction: Faction.Enemy);
            var site = Place(producer, 12, 12, asSite: true);

            Assert.IsFalse(rally.TrySet(house.Id, Vector2.one), "Makes nothing.");
            Assert.IsFalse(rally.TrySet(enemy.Id, Vector2.one), "Not the player's.");
            Assert.IsFalse(rally.TrySet(site.Id, Vector2.one), "Not finished.");
            rally.Dispose();
        }

        [Test]
        public void RallyPoint_WithoutOne_UnitsStayAtTheDoor()
        {
            var rally = CreateRally(out var production);
            var barracks = Place(Track(TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), new Vector2Int(1, -1), _knight)), 5, 5);

            production.TryProduce(barracks, _knight, out var knight);

            Assert.IsFalse(knight.IsMoving);
            rally.Dispose();
        }
    }
}
