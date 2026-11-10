using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
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
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Territory;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class ConstructionTests
    {
        private const float Step = 0.1f;
        private const float HouseBuildTime = 10f;

        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private SandboxService _normalGame;
        private BuildingService _buildings;
        private UnitService _units;
        private ResourceNodeService _nodes;
        private UnitMovementSystem _movement;
        private ConstructionSystem _construction;
        private UnitCommandService _commands;
        private UnitDefinition _villager;
        private UnitDefinition _knight;
        private BuildingDefinition _house;
        private int _completedCount;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(24, 24, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            var search = new GridSearch(_map);
            _normalGame = new SandboxService(new GameSessionSettings { Mode = GameMode.Normal }, _eventBus);
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, search, ids, _eventBus);
            _nodes = new ResourceNodeService(_map, ids, _eventBus);
            _movement = new UnitMovementSystem(_units, _map, new AStarPathfinder(_map), _eventBus);
            _construction = new ConstructionSystem(_units, _movement, _buildings, _map, _eventBus);

            var gathering = new GatheringSystem(_units, _movement, _nodes, _buildings, TestDefinitions.CreateFreeResources(_eventBus), search, _map,
                Track(ScriptableObject.CreateInstance<NormalModeSettings>()), _eventBus);
            _commands = new UnitCommandService(_units, _movement, new CombatService(_units, _buildings, _map, _eventBus), search, _map,
                gathering, _construction, new FarmingSystem(_units, _movement, _buildings, TestDefinitions.CreateFreeResources(_eventBus), search, _map, _eventBus),
                _buildings, _nodes, _eventBus);

            _villager = Track(TestDefinitions.CreateUnit("villager"));
            SetValue(_villager, "canGather", true);
            _knight = Track(TestDefinitions.CreateUnit("knight"));
            _house = CreateSiteBuilding("house", HouseBuildTime);
            SetValue(_house, "populationCapacity", 5);

            _completedCount = 0;
            _eventBus.Subscribe<BuildingCompletedEvent>(_ => _completedCount++);
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

        private static void SetValue(Object asset, string field, bool value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object asset, string field, int value)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private BuildingDefinition CreateSiteBuilding(string id, float buildTime)
        {
            var definition = Track(TestDefinitions.CreateBuilding(id, new Vector2Int(2, 2)));
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("buildTime").floatValue = buildTime;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private BuildingModel PlaceSite(int x, int y, BuildingDefinition definition = null, Faction faction = Faction.Player)
        {
            _buildings.TryPlace(definition != null ? definition : _house, new Vector2Int(x, y), faction, out var site, asSite: true);
            return site;
        }

        private UnitModel Spawn(UnitDefinition unit, int x, int y, Faction faction = Faction.Player)
        {
            _units.TrySpawn(unit, new Vector2Int(x, y), faction, out var spawned);
            return spawned;
        }

        private ResourceNodeModel PlaceNode(int x, int y, ResourceKind kind)
        {
            var definition = Track(ScriptableObject.CreateInstance<ResourceNodeDefinition>());
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("kind").enumValueIndex = (int)kind;
            serialized.FindProperty("amount").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            _nodes.TryPlace(definition, new Vector2Int(x, y), out var node);
            return node;
        }

        private void Tick()
        {
            _movement.Tick(Step);
            _construction.Tick(Step);
        }

        private void Run(float seconds)
        {
            for (var time = 0f; time < seconds; time += Step)
                Tick();
        }

        private void RunUntil(Func<bool> condition, float maxSeconds = 60f)
        {
            for (var time = 0f; time < maxSeconds && !condition(); time += Step)
                Tick();

            Assert.IsTrue(condition(), "It never happened.");
        }

        private PlacementService CreatePlacement(ResourceService resources)
        {
            return new PlacementService(_map, _buildings, _units, new ResourceNodeService(_map, new GameEntityIdGenerator(), _eventBus), resources, TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
        }

        private BuildingModel PlaceWith(PlacementService placement, BuildingDefinition definition)
        {
            placement.Begin(definition, Faction.Player);
            placement.MoveTo(new Vector2(10f, 10f));
            Assert.IsTrue(placement.TryConfirm(keepPlacing: false));
            return _buildings.TryGet(_buildings.GetBuildingAt(new Vector2(10f, 10f)), out var building) ? building : null;
        }

        // ---- Placing

        [Test]
        public void Placement_NormalGame_StartsAsASiteWithLittleHp()
        {
            var site = PlaceWith(CreatePlacement(new ResourceService(_normalGame, _eventBus)), _house);

            Assert.IsFalse(site.IsBuilt);
            Assert.AreEqual(0f, site.BuildProgress);
            Assert.AreEqual(10, site.CurrentHp, "A tenth of 100.");
        }

        [Test]
        public void Placement_Sandbox_StandsAtOnce()
        {
            var site = PlaceWith(CreatePlacement(TestDefinitions.CreateFreeResources(_eventBus)), _house);

            Assert.IsTrue(site.IsBuilt);
            Assert.AreEqual(100, site.CurrentHp);
        }

        [Test]
        public void Placement_NoBuildTime_StandsAtOnce()
        {
            var tent = Track(TestDefinitions.CreateBuilding("tent", new Vector2Int(2, 2)));

            var building = PlaceWith(CreatePlacement(new ResourceService(_normalGame, _eventBus)), tent);

            Assert.IsTrue(building.IsBuilt);
        }

        [Test]
        public void Model_HpGrowsWithTheWork_AndDamageStays()
        {
            var site = PlaceSite(10, 10);
            site.TakeDamage(5);

            site.AddBuildProgress(0.5f);
            Assert.AreEqual(5 + 45, site.CurrentHp);

            site.AddBuildProgress(0.5f);
            Assert.IsTrue(site.IsBuilt);
            Assert.AreEqual(95, site.CurrentHp, "The lost 5 HP stay lost.");

            site.AddBuildProgress(0.5f);
            Assert.AreEqual(1f, site.BuildProgress, "Nothing more to do.");
        }

        [Test]
        public void Model_ManyUnevenFrames_EndAtFullHp()
        {
            var site = PlaceSite(10, 10);
            var random = new System.Random(5);

            // Frame times as they come in play: a little different each frame.
            while (!site.IsBuilt)
                site.AddBuildProgress((0.005f + (float)random.NextDouble() * 0.035f) / 15f);

            Assert.AreEqual(100, site.CurrentHp, "Float rounding must not cost the last HP.");
        }

        // ---- Building

        [Test]
        public void Villager_FinishesTheSite()
        {
            var site = PlaceSite(10, 10);
            var villager = Spawn(_villager, 4, 10);

            Assert.IsTrue(_construction.TryOrder(villager, site));
            RunUntil(() => site.IsBuilt);

            Assert.AreEqual(100, site.CurrentHp);
            Assert.AreEqual(1, _completedCount, "Finished once.");
            Run(Step * 2f);
            Assert.IsFalse(_construction.IsBuilding(villager.Id));
            Assert.IsFalse(villager.IsBuilding);
            Assert.IsTrue(villager.IsIdle, "Free for the next job.");
        }

        [Test]
        public void Villager_WorksRightAgainstTheSite()
        {
            var site = PlaceSite(10, 10);
            var villager = Spawn(_villager, 4, 10);

            _construction.TryOrder(villager, site);
            RunUntil(() => villager.IsBuilding);

            var cell = _map.WorldToCell(villager.Position);
            var around = new RectInt(site.Area.x - 1, site.Area.y - 1, site.Area.width + 2, site.Area.height + 2);
            Assert.IsTrue(around.Contains(cell) && !site.Area.Contains(cell), "Next to the site, not in it.");
            Assert.LessOrEqual(Mathf.Abs(villager.Position.x - site.Area.xMin), 0.45f, "Its feet are at the wall.");
            Assert.IsFalse(villager.IsFacingLeft, "It faces the site.");
        }

        [Test]
        public void Villager_MovesAroundTheSiteNowAndThen()
        {
            var site = PlaceSite(10, 10, CreateSiteBuilding("castle", 1000f));
            var villager = Spawn(_villager, 4, 10);
            _construction.TryOrder(villager, site);

            var spots = new HashSet<Vector2Int>();
            for (var time = 0f; time < 30f; time += Step)
            {
                Tick();
                if (villager.IsBuilding)
                    spots.Add(_map.WorldToCell(villager.Position));
            }

            Assert.GreaterOrEqual(spots.Count, 2, "It worked from more than one spot.");
            Assert.IsTrue(_construction.IsBuilding(villager.Id), "Still on the job.");
        }

        [Test]
        public void MoreVillagers_BuildFaster()
        {
            var alone = PlaceSite(4, 4, CreateSiteBuilding("slow", 1000f));
            var together = PlaceSite(4, 16, CreateSiteBuilding("fast", 1000f));
            _construction.TryOrder(Spawn(_villager, 1, 4), alone);
            _construction.TryOrder(Spawn(_villager, 1, 16), together);
            _construction.TryOrder(Spawn(_villager, 1, 17), together);

            Run(15f);

            Assert.Greater(together.BuildProgress, alone.BuildProgress * 1.5f);
        }

        [Test]
        public void TwoVillagers_TakeDifferentSpots()
        {
            var site = PlaceSite(10, 10);
            var first = Spawn(_villager, 4, 10);
            var second = Spawn(_villager, 4, 11);
            _construction.TryOrder(first, site);
            _construction.TryOrder(second, site);

            RunUntil(() => first.IsBuilding && second.IsBuilding);

            Assert.AreNotEqual(_map.WorldToCell(first.Position), _map.WorldToCell(second.Position));
        }

        [Test]
        public void Soldier_CantBuild()
        {
            var site = PlaceSite(10, 10);

            Assert.IsFalse(_construction.TryOrder(Spawn(_knight, 4, 10), site));
        }

        [Test]
        public void EnemySite_IsNotThePlayersWork()
        {
            var site = PlaceSite(10, 10, faction: Faction.Enemy);

            Assert.IsFalse(_construction.TryOrder(Spawn(_villager, 4, 10), site));
        }

        [Test]
        public void FinishedBuilding_NeedsNoWork()
        {
            _buildings.TryPlace(_house, new Vector2Int(10, 10), Faction.Player, out var house);

            Assert.IsFalse(_construction.TryOrder(Spawn(_villager, 4, 10), house));
        }

        [Test]
        public void SiteDestroyed_EndsTheWork()
        {
            var site = PlaceSite(10, 10);
            var villager = Spawn(_villager, 4, 10);
            _construction.TryOrder(villager, site);
            RunUntil(() => villager.IsBuilding);

            _buildings.Remove(site.Id);
            Run(Step * 2f);

            Assert.IsFalse(_construction.IsBuilding(villager.Id));
            Assert.IsFalse(villager.IsBuilding);
        }

        // ---- Orders

        [Test]
        public void RightClick_OnOwnSite_SendsVillagersToBuild_AndSoldiersWalk()
        {
            var site = PlaceSite(10, 10);
            var villager = Spawn(_villager, 4, 10);
            var knight = Spawn(_knight, 4, 12);

            Assert.IsTrue(_commands.CommandAt(new[] { villager.Id, knight.Id }, new Vector2(11f, 11f)));

            Assert.IsTrue(_construction.IsBuilding(villager.Id));
            Assert.IsFalse(_construction.IsBuilding(knight.Id));
            Assert.IsTrue(knight.IsMoving);
        }

        [Test]
        public void MoveOrder_TakesTheVillagerOffTheSite()
        {
            var site = PlaceSite(10, 10);
            var villager = Spawn(_villager, 4, 10);
            _construction.TryOrder(villager, site);
            RunUntil(() => villager.IsBuilding);

            _commands.MoveTo(new[] { villager.Id }, new Vector2(2.5f, 2.5f));
            Tick();

            Assert.IsFalse(_construction.IsBuilding(villager.Id));
            Assert.IsFalse(villager.IsBuilding);
        }

        // ---- A site gives nothing until it stands

        [Test]
        public void Site_HousesNobody_UntilFinished()
        {
            var population = new PopulationService(_normalGame, _units, _buildings);
            var site = PlaceSite(10, 10);
            Assert.AreEqual(0, population.Capacity);

            site.AddBuildProgress(1f);

            Assert.AreEqual(5, population.Capacity);
        }

        [Test]
        public void Site_MakesNoUnits_UntilFinished()
        {
            var barracks = Track(TestDefinitions.CreateProducer("barracks", new Vector2Int(2, 2), new Vector2Int(0, -1), _knight));
            var production = new UnitProductionService(_units, TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreePopulation(_units, _buildings, _eventBus));
            var site = PlaceSite(10, 10, barracks);

            Assert.AreEqual(ProductionBlock.UnderConstruction, production.GetBlock(site, _knight));
            Assert.IsFalse(production.TryProduce(site, _knight, out _));

            site.AddBuildProgress(1f);
            Assert.AreEqual(ProductionBlock.None, production.GetBlock(site, _knight));
        }

        [Test]
        public void Site_OpensNoTerritory_UntilFinished()
        {
            var territory = new TerritoryService(_map, _buildings, _normalGame, _eventBus);
            var outpost = Track(TestDefinitions.CreateTerritorySource("outpost", 4f));
            var site = PlaceSite(10, 10, outpost);
            Assert.IsFalse(territory.IsInside(new Vector2Int(11, 11)));

            site.AddBuildProgress(1f);
            _eventBus.Publish(new BuildingCompletedEvent(site));

            Assert.IsTrue(territory.IsInside(new Vector2Int(11, 11)));
            territory.Dispose();
        }

        [Test]
        public void Site_DoesntShoot_UntilFinished()
        {
            var tower = Track(TestDefinitions.CreateTower("tower", 6f, 3));
            var combat = new CombatService(_units, _buildings, _map, _eventBus);
            var defense = new BuildingDefenseSystem(_buildings, combat, _map, _eventBus);
            var site = PlaceSite(10, 10, tower);
            var enemy = Spawn(_knight, 13, 11, Faction.Enemy);

            defense.Tick(Step);
            Assert.AreEqual(enemy.Definition.MaxHp, enemy.CurrentHp);

            site.AddBuildProgress(1f);
            defense.Tick(Step);
            Assert.Less(enemy.CurrentHp, enemy.Definition.MaxHp);
        }

        [Test]
        public void Tooltip_ShowsBuildTimeOnlyInANormalGame()
        {
            StringAssert.Contains("Build time: 10s", TooltipText.ForBuilding(_house, isNormalMode: true));
            StringAssert.DoesNotContain("Build time", TooltipText.ForBuilding(_house));
        }

        // ---- Progress bar

        [Test]
        public void ProgressBar_FillsTheTrackInWholePixels()
        {
            // A 1.5 unit bar is 96 pixels; the track leaves 10 on each end, so 76 pixels.
            Assert.AreEqual(0f, BuildProgressBarView.GetFillWidth(1.5f, 0f));
            Assert.AreEqual(38f / 64f, BuildProgressBarView.GetFillWidth(1.5f, 0.5f), 0.0001f);
            Assert.AreEqual(76f / 64f, BuildProgressBarView.GetFillWidth(1.5f, 1f), 0.0001f);
            Assert.AreEqual(76f / 64f, BuildProgressBarView.GetFillWidth(1.5f, 2f), 0.0001f, "Never past the end.");
            Assert.AreEqual(37f / 64f, BuildProgressBarView.GetFillWidth(1.5f, 0.495f), 0.0001f, "Rounded down to a whole pixel.");
        }

        // ---- Cursor

        [Test]
        public void Cursor_TreesAndSheepTakeTheAxe_StoneAndGoldThePickaxe()
        {
            Assert.AreEqual(ToolCursor.Axe, ToolCursorPicker.Pick(PlaceNode(2, 2, ResourceKind.Wood), null));
            Assert.AreEqual(ToolCursor.Axe, ToolCursorPicker.Pick(PlaceNode(4, 2, ResourceKind.Food), null));
            Assert.AreEqual(ToolCursor.Pickaxe, ToolCursorPicker.Pick(PlaceNode(6, 2, ResourceKind.Stone), null));
            Assert.AreEqual(ToolCursor.Pickaxe, ToolCursorPicker.Pick(PlaceNode(8, 2, ResourceKind.Gold), null));
        }

        [Test]
        public void Cursor_OwnSiteTakesTheHammer_NothingElseDoes()
        {
            _buildings.TryPlace(_house, new Vector2Int(14, 14), Faction.Player, out var finished);

            Assert.AreEqual(ToolCursor.Hammer, ToolCursorPicker.Pick(null, PlaceSite(10, 10)));
            Assert.AreEqual(ToolCursor.None, ToolCursorPicker.Pick(null, finished), "A finished building.");
            Assert.AreEqual(ToolCursor.None, ToolCursorPicker.Pick(null, PlaceSite(4, 14, faction: Faction.Enemy)), "An enemy site.");
            Assert.AreEqual(ToolCursor.None, ToolCursorPicker.Pick(null, null), "Nothing under the mouse.");
        }
    }
}
