using System.Collections.Generic;
using System.Linq;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class GameLoopTests
    {
        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private GameEntityIdGenerator _ids;
        private GridSearch _search;
        private ResourceService _resources;
        private BuildingService _buildings;
        private UnitService _units;
        private CombatService _combat;
        private SandboxService _sandbox;
        private PopulationService _population;
        private CalendarService _calendar;
        private RaidSettings _raids;
        private NormalModeSettings _normal;
        private UnitDefinition _villager;
        private UnitDefinition _knight;
        private BuildingDefinition _castle;
        private BuildingDefinition _house;
        private readonly List<int> _days = new();
        private readonly List<GameNoticeEvent> _notices = new();

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(40, 40, Vector2.zero);
            _eventBus = new EventBus();
            _ids = new GameEntityIdGenerator();
            _search = new GridSearch(_map);
            _sandbox = CreateSandbox(GameMode.Normal);
            _resources = new ResourceService(_sandbox, _eventBus);
            _buildings = new BuildingService(_map, _ids, _eventBus);
            _units = new UnitService(_map, _search, _ids, _eventBus);
            _combat = new CombatService(_units, _buildings, _map, _eventBus);
            _population = new PopulationService(_sandbox, _units, _buildings);

            _villager = Track(TestDefinitions.CreateUnit("villager"));
            SetBool(_villager, "canGather", true);
            SetCost(_villager, ResourceKind.Food, 30);
            _knight = Track(TestDefinitions.CreateUnit("knight", damage: 10));
            _castle = Track(TestDefinitions.CreateBuilding("castle", new Vector2Int(3, 3), maxHp: 100));
            SetInt(_castle, "populationCapacity", 2);
            SetProducibles(_castle, "normalModeProducibles", _villager);
            _house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            SetInt(_house, "populationCapacity", 3);

            _normal = Track(ScriptableObject.CreateInstance<NormalModeSettings>());
            var normal = new SerializedObject(_normal);
            normal.FindProperty("castle").objectReferenceValue = _castle;
            normal.FindProperty("villager").objectReferenceValue = _villager;
            normal.FindProperty("sheepPerMorning").intValue = 2;
            normal.FindProperty("maxSheep").intValue = 3;
            normal.ApplyModifiedPropertiesWithoutUndo();

            _raids = CreateRaids();
            _calendar = new CalendarService(_raids, _sandbox, _eventBus);

            _days.Clear();
            _notices.Clear();
            _eventBus.Subscribe<DayStartedEvent>(e => _days.Add(e.Day));
            _eventBus.Subscribe<GameNoticeEvent>(e => _notices.Add(e));
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

        private static SandboxService CreateSandbox(GameMode mode)
        {
            return new SandboxService(new GameSessionSettings { Mode = mode }, new EventBus());
        }

        private static void SetBool(Object asset, string field, bool value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object asset, string field, int value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).intValue = value;
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

        private static void SetProducibles(Object asset, string field, UnitDefinition unit)
        {
            var serialized = new SerializedObject(asset);
            var list = serialized.FindProperty(field);
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = unit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Day 10 s, night 5 s. Day 2 has one raid of two waves, one knight each. It is also the last raid.</summary>
        private RaidSettings CreateRaids()
        {
            var raids = Track(ScriptableObject.CreateInstance<RaidSettings>());
            var serialized = new SerializedObject(raids);
            serialized.FindProperty("daySeconds").floatValue = 10f;
            serialized.FindProperty("nightSeconds").floatValue = 5f;
            serialized.FindProperty("earliestStart").floatValue = 0.2f;
            serialized.FindProperty("latestStart").floatValue = 0.2f;
            serialized.FindProperty("waveDelay").floatValue = 1f;
            serialized.FindProperty("minSpawnDistance").floatValue = 10f;
            serialized.FindProperty("aggroRange").floatValue = 5f;

            var list = serialized.FindProperty("raids");
            list.arraySize = 1;
            var raid = list.GetArrayElementAtIndex(0);
            raid.FindPropertyRelative("day").intValue = 2;
            var waves = raid.FindPropertyRelative("waves");
            waves.arraySize = 2;
            for (var i = 0; i < 2; i++)
            {
                var groups = waves.GetArrayElementAtIndex(i).FindPropertyRelative("groups");
                groups.arraySize = 1;
                groups.GetArrayElementAtIndex(0).FindPropertyRelative("unit").objectReferenceValue = _knight;
                groups.GetArrayElementAtIndex(0).FindPropertyRelative("count").intValue = 1;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return raids;
        }

        private RaidService CreateRaidService()
        {
            return new RaidService(_raids, _normal, _calendar, _units, _buildings, _search, _map, _sandbox, new GameSessionSettings { Seed = 3 }, _eventBus);
        }

        private BattleAiSystem CreateAi()
        {
            return new BattleAiSystem(_units, _buildings, _combat, _raids, _normal, _sandbox);
        }

        private GameOutcomeService CreateOutcome(GameClock clock)
        {
            return new GameOutcomeService(_sandbox, _normal, _resources, _population, clock, _eventBus);
        }

        private BuildingModel PlaceCastle()
        {
            _buildings.TryPlace(_castle, new Vector2Int(18, 18), Faction.Player, out var castle);
            return castle;
        }

        private void RunRaid(RaidService raids, float seconds)
        {
            const float step = 0.1f;
            for (var time = 0f; time < seconds; time += step)
            {
                _calendar.Tick(step);
                raids.Tick(step);
            }
        }

        private void KillEnemies()
        {
            foreach (var unit in _units.Units.Where(u => u.Faction == Faction.Enemy).ToList())
                _units.Remove(unit.Id);
        }

        // ---- Population

        [Test]
        public void Population_CountsPlayerUnitsAndHousing()
        {
            PlaceCastle();
            _buildings.TryPlace(_house, new Vector2Int(2, 2), Faction.Player, out _);
            _buildings.TryPlace(_house, new Vector2Int(6, 2), Faction.Enemy, out _);
            _units.TrySpawn(_villager, new Vector2Int(10, 10), Faction.Player, out _);
            _units.TrySpawn(_knight, new Vector2Int(12, 10), Faction.Enemy, out _);

            Assert.AreEqual(1, _population.Used);
            Assert.AreEqual(5, _population.Capacity);
        }

        [Test]
        public void Population_Full_RefusesNewUnit()
        {
            PlaceCastle();
            _units.TrySpawn(_villager, new Vector2Int(10, 10), Faction.Player, out _);
            _units.TrySpawn(_villager, new Vector2Int(12, 10), Faction.Player, out _);

            Assert.IsFalse(_population.HasRoomFor(_villager));
        }

        [Test]
        public void Population_Sandbox_HasNoLimit()
        {
            var population = new PopulationService(CreateSandbox(GameMode.Sandbox), _units, _buildings);

            Assert.IsTrue(population.HasRoomFor(_knight));
        }

        // ---- Production

        [Test]
        public void Castle_ProducesVillagerOnlyInNormalMode()
        {
            var normal = new UnitProductionService(_units, _resources, _population);
            var free = TestDefinitions.CreateFreeResources(_eventBus);
            var sandbox = new UnitProductionService(_units, free, new PopulationService(CreateSandbox(GameMode.Sandbox), _units, _buildings));

            CollectionAssert.AreEqual(new[] { _villager }, normal.GetProducibles(_castle));
            Assert.IsEmpty(sandbox.GetProducibles(_castle), "The case says only barracks produce.");
        }

        [Test]
        public void Production_GivesTheReasonItWasRefused()
        {
            var castle = PlaceCastle();
            var production = new UnitProductionService(_units, _resources, _population);

            Assert.AreEqual(ProductionBlock.NotEnoughResources, production.GetBlock(castle, _villager));

            _resources.Add(ResourceKind.Food, 200);
            Assert.AreEqual(ProductionBlock.None, production.GetBlock(castle, _villager));
            Assert.AreEqual(ProductionBlock.NotAvailable, production.GetBlock(castle, _knight));

            production.TryProduce(castle, _villager, out _);
            production.TryProduce(castle, _villager, out _);
            Assert.AreEqual(ProductionBlock.NoHousing, production.GetBlock(castle, _villager));
            Assert.AreEqual(140, _resources.Get(ResourceKind.Food));
        }

        // ---- Calendar

        [Test]
        public void Calendar_CountsDaysAndNights()
        {
            _calendar.Tick(0.1f);
            Assert.AreEqual(1, _calendar.Day);
            Assert.IsFalse(_calendar.IsNight);

            _calendar.Tick(10f);
            Assert.IsTrue(_calendar.IsNight);

            _calendar.Tick(5f);
            Assert.AreEqual(2, _calendar.Day);
            CollectionAssert.AreEqual(new[] { 1, 2 }, _days);
        }

        [Test]
        public void Calendar_Frozen_StandsStill()
        {
            _calendar.Tick(0.1f);
            _calendar.Freeze();
            _calendar.Tick(100f);

            Assert.AreEqual(1, _calendar.Day);

            _calendar.Resume();
            _calendar.Tick(15f);
            Assert.AreEqual(2, _calendar.Day);
        }

        [Test]
        public void Calendar_Sandbox_DoesNothing()
        {
            var calendar = new CalendarService(_raids, CreateSandbox(GameMode.Sandbox), _eventBus);

            calendar.Tick(1000f);

            Assert.IsFalse(calendar.IsEnabled);
            Assert.AreEqual(0, calendar.Day);
        }

        // ---- Raids

        [Test]
        public void Raid_WarnsInTheMorningThenStartsAndFreezesCalendar()
        {
            PlaceCastle();
            var raids = CreateRaidService();

            RunRaid(raids, 15.5f);
            Assert.IsTrue(_notices.Any(n => n.Text == "Today is a raid day!"));
            Assert.IsFalse(raids.IsRaidActive);

            RunRaid(raids, 3.5f);
            Assert.IsTrue(raids.IsRaidActive);
            Assert.IsTrue(_calendar.IsFrozen);
            Assert.AreEqual(1, _units.Units.Count(u => u.Faction == Faction.Enemy));
            Assert.IsTrue(_notices.Any(n => n.IsAlert));
        }

        [Test]
        public void Raid_SendsNextWaveOnlyAfterTheLastIsDead()
        {
            PlaceCastle();
            var raids = CreateRaidService();
            RunRaid(raids, 19f);

            RunRaid(raids, 3f);
            Assert.AreEqual(1, _units.Count, "Still the first wave.");

            KillEnemies();
            RunRaid(raids, 1.5f);
            Assert.AreEqual(1, _units.Count, "The second wave came.");
        }

        [Test]
        public void Raid_AllWavesDead_EndsAndResumesCalendar()
        {
            PlaceCastle();
            var raids = CreateRaidService();
            var isLast = false;
            _eventBus.Subscribe<RaidEndedEvent>(e => isLast = e.IsLastRaid);
            RunRaid(raids, 19f);
            KillEnemies();
            RunRaid(raids, 1.5f);
            KillEnemies();

            RunRaid(raids, 0.5f);

            Assert.IsFalse(raids.IsRaidActive);
            Assert.IsFalse(_calendar.IsFrozen);
            Assert.IsTrue(isLast);
        }

        [Test]
        public void Raid_SpawnsFarFromCastleOnTheCoast()
        {
            for (var x = 0; x < 40; x++)
            {
                _map.SetTerrainWalkable(new Vector2Int(x, 0), false);
                _map.SetTerrainWalkable(new Vector2Int(x, 39), false);
            }

            PlaceCastle();
            var raids = CreateRaidService();
            RunRaid(raids, 19f);

            var enemy = _units.Units.Single(u => u.Faction == Faction.Enemy);
            Assert.GreaterOrEqual(Vector2.Distance(enemy.Position, new Vector2(19.5f, 19.5f)), 8f);
        }

        [Test]
        public void Raid_Sandbox_NeverComes()
        {
            var sandbox = CreateSandbox(GameMode.Sandbox);
            var calendar = new CalendarService(_raids, sandbox, _eventBus);
            var raids = new RaidService(_raids, _normal, calendar, _units, _buildings, _search, _map, sandbox, new GameSessionSettings(), _eventBus);

            for (var i = 0; i < 400; i++)
            {
                calendar.Tick(0.1f);
                raids.Tick(0.1f);
            }

            Assert.AreEqual(0, _units.Count);
        }

        // ---- AI

        [Test]
        public void Ai_Enemy_MarchesOnCastle()
        {
            var castle = PlaceCastle();
            _units.TrySpawn(_knight, new Vector2Int(2, 2), Faction.Enemy, out var enemy);

            CreateAi().Tick(0.3f);

            Assert.AreEqual(castle.Id, enemy.AttackTarget);
        }

        [Test]
        public void Ai_Enemy_TurnsOnWhatComesNear()
        {
            PlaceCastle();
            _units.TrySpawn(_knight, new Vector2Int(2, 2), Faction.Enemy, out var enemy);
            _units.TrySpawn(_villager, new Vector2Int(4, 2), Faction.Player, out var villager);

            CreateAi().Tick(0.3f);

            Assert.AreEqual(villager.Id, enemy.AttackTarget);
        }

        [Test]
        public void Ai_Enemy_AttacksBuildingInItsWay()
        {
            PlaceCastle();
            _buildings.TryPlace(_house, new Vector2Int(4, 2), Faction.Player, out var house);
            _units.TrySpawn(_knight, new Vector2Int(2, 2), Faction.Enemy, out var enemy);

            CreateAi().Tick(0.3f);

            Assert.AreEqual(house.Id, enemy.AttackTarget);
        }

        [Test]
        public void Ai_Enemy_GivesUpOnTargetThatRanFar()
        {
            var castle = PlaceCastle();
            _units.TrySpawn(_knight, new Vector2Int(2, 2), Faction.Enemy, out var enemy);
            _units.TrySpawn(_villager, new Vector2Int(30, 2), Faction.Player, out var villager);
            enemy.SetAttackTarget(villager.Id);
            var ai = CreateAi();

            ai.Tick(0.3f);
            Assert.IsFalse(enemy.HasAttackTarget);

            ai.Tick(0.3f);
            Assert.AreEqual(castle.Id, enemy.AttackTarget);
        }

        [Test]
        public void Ai_IdleSoldier_AttacksEnemyNearby()
        {
            _units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out var soldier);
            _units.TrySpawn(_knight, new Vector2Int(8, 5), Faction.Enemy, out var enemy);

            CreateAi().Tick(0.3f);

            Assert.AreEqual(enemy.Id, soldier.AttackTarget);
        }

        [Test]
        public void Ai_IdleSoldier_TellsItSpottedAnEnemyForAMoment()
        {
            _units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out var soldier);
            _units.TrySpawn(_knight, new Vector2Int(8, 5), Faction.Enemy, out var enemy);
            Assert.IsFalse(soldier.HasJustSpottedEnemy);

            CreateAi().Tick(0.3f);
            Assert.IsTrue(soldier.HasJustSpottedEnemy);

            soldier.TickCombatTimers(2f);
            Assert.IsFalse(soldier.HasJustSpottedEnemy);
            Assert.AreEqual(enemy.Id, soldier.AttackTarget, "Only the icon ends, not the fight.");
        }

        [Test]
        public void Ai_Villager_LeavesFightingToSoldiers()
        {
            _units.TrySpawn(_villager, new Vector2Int(5, 5), Faction.Player, out var villager);
            _units.TrySpawn(_knight, new Vector2Int(8, 5), Faction.Enemy, out _);

            CreateAi().Tick(0.3f);

            Assert.IsFalse(villager.HasAttackTarget);
        }

        [Test]
        public void Ai_Sandbox_GivesNoOrders()
        {
            _units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out var soldier);
            _units.TrySpawn(_knight, new Vector2Int(8, 5), Faction.Enemy, out var enemy);
            var ai = new BattleAiSystem(_units, _buildings, _combat, _raids, _normal, CreateSandbox(GameMode.Sandbox));

            ai.Tick(1f);

            Assert.IsFalse(soldier.HasAttackTarget);
            Assert.IsFalse(enemy.HasAttackTarget);
        }

        // ---- Outcome

        [Test]
        public void Outcome_CastleFalls_IsDefeatAndStopsTheClock()
        {
            var clock = new GameClock();
            var ended = new List<GameEndedEvent>();
            _eventBus.Subscribe<GameEndedEvent>(e => ended.Add(e));
            var castle = PlaceCastle();
            var outcome = CreateOutcome(clock);

            _buildings.Remove(castle.Id);

            Assert.IsTrue(outcome.IsOver);
            Assert.IsTrue(clock.IsPaused);
            Assert.IsFalse(ended.Single().IsVictory);
        }

        [Test]
        public void Outcome_OtherBuildingFalls_GameGoesOn()
        {
            PlaceCastle();
            _buildings.TryPlace(_house, new Vector2Int(2, 2), Faction.Player, out var house);
            var outcome = CreateOutcome(new GameClock());

            _buildings.Remove(house.Id);

            Assert.IsFalse(outcome.IsOver);
        }

        [Test]
        public void Outcome_LastUnitDiesWithoutFood_IsDefeat()
        {
            PlaceCastle();
            _units.TrySpawn(_villager, new Vector2Int(5, 5), Faction.Player, out var villager);
            var outcome = CreateOutcome(new GameClock());

            _units.Remove(villager.Id);

            Assert.IsTrue(outcome.IsOver);
        }

        [Test]
        public void Outcome_LastUnitDiesWithFoodLeft_GameGoesOn()
        {
            PlaceCastle();
            _resources.Add(ResourceKind.Food, 30);
            _units.TrySpawn(_villager, new Vector2Int(5, 5), Faction.Player, out var villager);
            var outcome = CreateOutcome(new GameClock());

            _units.Remove(villager.Id);

            Assert.IsFalse(outcome.IsOver);
        }

        [Test]
        public void Outcome_LastRaidBeaten_IsVictory()
        {
            var ended = new List<GameEndedEvent>();
            _eventBus.Subscribe<GameEndedEvent>(e => ended.Add(e));
            var outcome = CreateOutcome(new GameClock());

            _eventBus.Publish(new RaidEndedEvent(isLastRaid: true));

            Assert.IsTrue(outcome.IsOver);
            Assert.IsTrue(ended.Single().IsVictory);
        }

        [Test]
        public void Outcome_Sandbox_NeverEnds()
        {
            var castle = PlaceCastle();
            var outcome = new GameOutcomeService(CreateSandbox(GameMode.Sandbox), _normal, _resources, _population, new GameClock(), _eventBus);

            _buildings.Remove(castle.Id);

            Assert.IsFalse(outcome.IsOver);
        }

        // ---- Sheep

        [Test]
        public void Sheep_ComeInTheMorningUpToTheLimit()
        {
            var sheep = Track(ScriptableObject.CreateInstance<ResourceNodeDefinition>());
            var serializedSheep = new SerializedObject(sheep);
            serializedSheep.FindProperty("kind").enumValueIndex = (int)ResourceKind.Food;
            serializedSheep.ApplyModifiedPropertiesWithoutUndo();

            var distribution = Track(ScriptableObject.CreateInstance<ResourceDistributionSettings>());
            var serialized = new SerializedObject(distribution);
            serialized.FindProperty("startClearRadius").floatValue = 0f;
            serialized.FindProperty("startDepositRadius").floatValue = 40f;
            var groups = serialized.FindProperty("groups");
            groups.arraySize = 1;
            var group = groups.GetArrayElementAtIndex(0);
            var definitions = group.FindPropertyRelative("definitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = sheep;
            group.FindPropertyRelative("depositCount").intValue = 0;
            group.FindPropertyRelative("minNodesPerDeposit").intValue = 1;
            group.FindPropertyRelative("maxNodesPerDeposit").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var nodes = new ResourceNodeService(_map, _ids, _eventBus);
            var distributor = new ResourceDistributor(_map, _search, nodes, distribution);
            distributor.Distribute(5);
            using var respawn = new ResourceRespawnService(distributor, nodes, _normal, _eventBus);

            _eventBus.Publish(new DayStartedEvent(1));
            Assert.AreEqual(0, nodes.Nodes.Count, "Not on the first morning.");

            _eventBus.Publish(new DayStartedEvent(2));
            Assert.AreEqual(2, nodes.Nodes.Count);

            _eventBus.Publish(new DayStartedEvent(3));
            Assert.AreEqual(3, nodes.Nodes.Count, "The limit is 3.");
        }
    }
}
