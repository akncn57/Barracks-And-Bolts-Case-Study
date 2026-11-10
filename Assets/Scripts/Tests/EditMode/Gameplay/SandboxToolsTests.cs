using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.TimeControl;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    /// <summary>Time control, deleting, the battle AI switch and placing resources.</summary>
    public class SandboxToolsTests
    {
        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private GameEntityIdGenerator _ids;
        private BuildingService _buildings;
        private UnitService _units;
        private ResourceNodeService _nodes;
        private SelectionService _selection;
        private UnitDefinition _knight;
        private BuildingDefinition _house;
        private BuildingDefinition _castle;
        private NormalModeSettings _normalMode;
        private ResourceNodeDefinition _tree;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(16, 16, Vector2.zero);
            _eventBus = new EventBus();
            _ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, _ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), _ids, _eventBus);
            _nodes = new ResourceNodeService(_map, _ids, _eventBus);
            _selection = new SelectionService(_units, _buildings, _eventBus);

            _knight = Track(TestDefinitions.CreateUnit("knight", damage: 3));
            _house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));
            _castle = Track(TestDefinitions.CreateBuilding("castle", new Vector2Int(3, 3)));
            _normalMode = Track(ScriptableObject.CreateInstance<NormalModeSettings>());
            var settings = new SerializedObject(_normalMode);
            settings.FindProperty("castle").objectReferenceValue = _castle;
            settings.ApplyModifiedPropertiesWithoutUndo();

            _tree = Track(ScriptableObject.CreateInstance<ResourceNodeDefinition>());
            var tree = new SerializedObject(_tree);
            tree.FindProperty("displayName").stringValue = "Tree";
            tree.FindProperty("amount").intValue = 50;
            tree.ApplyModifiedPropertiesWithoutUndo();
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

        private SandboxService CreateMode(GameMode mode)
        {
            return new SandboxService(new GameSessionSettings { Mode = mode }, _eventBus);
        }

        private DeletionService CreateDeletion(SandboxService mode)
        {
            return new DeletionService(_selection, _units, _buildings, _nodes, new HoverService(_units, _buildings, _nodes, _eventBus), mode, _normalMode);
        }

        private UnitModel Spawn(int x, int y, Faction faction)
        {
            _units.TrySpawn(_knight, new Vector2Int(x, y), faction, out var unit);
            return unit;
        }

        // ---- Time control

        [Test]
        public void TimeControl_PausesAndResumes()
        {
            var clock = new GameClock();
            var time = new TimeControlService(clock, _eventBus);
            var changes = 0;
            _eventBus.Subscribe<GameSpeedChangedEvent>(_ => changes++);

            time.TogglePause();
            Assert.IsTrue(clock.IsPaused);
            Assert.AreEqual(0f, clock.Advance(0.05f));

            time.TogglePause();
            Assert.IsFalse(clock.IsPaused);
            Assert.AreEqual(2, changes);
        }

        [Test]
        public void TimeControl_SpeedSteps_StayInRange_AndEndAPause()
        {
            var clock = new GameClock();
            var time = new TimeControlService(clock, _eventBus);

            time.SpeedUp();
            Assert.AreEqual(2f, time.Speed);
            time.SpeedUp();
            time.SpeedUp();
            Assert.AreEqual(3f, time.Speed, "No step above 3x.");

            time.SetPaused(true);
            time.SpeedDown();
            Assert.AreEqual(2f, time.Speed);
            Assert.IsFalse(time.IsPaused, "Picking a speed resumes.");

            time.SpeedDown();
            time.SpeedDown();
            Assert.AreEqual(1f, time.Speed, "No step below 1x.");
        }

        [Test]
        public void TimeControl_AfterTheGameEnds_CantRestartTheClock()
        {
            var clock = new GameClock();
            var time = new TimeControlService(clock, _eventBus);
            clock.SetPaused(true);
            _eventBus.Publish(new GameEndedEvent(false, "Lost"));

            time.TogglePause();
            time.SetSpeed(2f);

            Assert.IsTrue(clock.IsPaused);
            time.Dispose();
        }

        // ---- Deleting

        [Test]
        public void Delete_Sandbox_RemovesAnySelectedUnitOrBuilding()
        {
            var deletion = CreateDeletion(CreateMode(GameMode.Sandbox));
            var enemy = Spawn(3, 3, Faction.Enemy);
            _buildings.TryPlace(_house, new Vector2Int(8, 8), Faction.Enemy, out var house);

            _selection.Select(enemy.Id);
            Assert.AreEqual(1, deletion.DeleteSelectedOrHovered());
            _selection.Select(house.Id);
            Assert.AreEqual(1, deletion.DeleteSelectedOrHovered());

            Assert.AreEqual(0, _units.Count);
            Assert.AreEqual(0, _buildings.Count);
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(8, 8)), "The cells are free again.");
            Assert.AreEqual(0, _selection.Selected.Count, "The selection lets go of it.");
        }

        [Test]
        public void Delete_NormalGame_OnlyOwnThings_AndNeverTheCastle()
        {
            var deletion = CreateDeletion(CreateMode(GameMode.Normal));
            var own = Spawn(2, 2, Faction.Player);
            var enemy = Spawn(4, 2, Faction.Enemy);
            _buildings.TryPlace(_castle, new Vector2Int(8, 8), Faction.Player, out var castle);
            _buildings.TryPlace(_house, new Vector2Int(2, 8), Faction.Player, out var house);

            Assert.IsTrue(deletion.CanDelete(own.Id));
            Assert.IsTrue(deletion.CanDelete(house.Id));
            Assert.IsFalse(deletion.CanDelete(enemy.Id));
            Assert.IsFalse(deletion.CanDelete(castle.Id));

            _selection.Select(castle.Id);
            Assert.AreEqual(0, deletion.DeleteSelectedOrHovered());
            Assert.IsTrue(_buildings.TryGet(castle.Id, out _));
        }

        [Test]
        public void Delete_SandboxWithNothingSelected_RemovesTheResourceUnderTheMouse()
        {
            var mode = CreateMode(GameMode.Sandbox);
            var hover = new HoverService(_units, _buildings, _nodes, _eventBus);
            var deletion = new DeletionService(_selection, _units, _buildings, _nodes, hover, mode, _normalMode);
            _nodes.TryPlace(_tree, new Vector2Int(5, 5), out var node);
            hover.UpdateAt(new Vector2(5.5f, 5.5f));

            Assert.AreEqual(1, deletion.DeleteSelectedOrHovered());

            Assert.AreEqual(0, _nodes.Nodes.Count);
            Assert.IsTrue(node.IsDepleted, "Emptied, so a villager on it moves on.");
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(5, 5)));
        }

        // ---- Battle AI switch

        [Test]
        public void BattleAi_Sandbox_RunsOnlyWhileSwitchedOn()
        {
            var mode = CreateMode(GameMode.Sandbox);
            var raids = Track(ScriptableObject.CreateInstance<RaidSettings>());
            var ai = new BattleAiSystem(_units, _buildings, new CombatService(_units, _buildings, _map, _eventBus), raids, _normalMode, mode);
            var enemy = Spawn(5, 5, Faction.Enemy);
            var player = Spawn(7, 5, Faction.Player);

            ai.Tick(1f);
            Assert.IsFalse(enemy.HasAttackTarget, "Off by default.");

            mode.ToggleBattleAi();
            ai.Tick(1f);

            Assert.AreEqual(player.Id, enemy.AttackTarget, "With no Castle, the enemy turns on what is near.");
            Assert.AreEqual(enemy.Id, player.AttackTarget, "The idle soldier answers.");
        }

        [Test]
        public void Switches_DoNothingInANormalGame()
        {
            var mode = CreateMode(GameMode.Normal);

            mode.ToggleBattleAi();
            mode.ToggleDebugView();

            Assert.IsFalse(mode.IsBattleAiOn);
            Assert.IsFalse(mode.IsDebugViewOn);
        }

        // ---- Placing resources

        [Test]
        public void Resource_IsPlacedOnFreeGround_AndBlocksThePath()
        {
            var placement = new PlacementService(_map, _buildings, _units, _nodes, TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            placement.BeginNode(_tree);
            placement.MoveTo(new Vector2(6.5f, 6.5f));

            Assert.AreEqual(PlacementBlock.None, placement.Block);
            Assert.IsTrue(placement.TryConfirm(keepPlacing: true));

            Assert.AreEqual(1, _nodes.Nodes.Count);
            Assert.IsFalse(_map.IsWalkable(new Vector2Int(6, 6)), "A* goes around it now.");
            Assert.AreEqual(PlacementBlock.Occupied, placement.Block, "The same cell is taken.");
        }

        [Test]
        public void Resource_NotOnAUnit()
        {
            var placement = new PlacementService(_map, _buildings, _units, _nodes, TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            Spawn(6, 6, Faction.Player);
            placement.BeginNode(_tree);

            placement.MoveTo(new Vector2(6.5f, 6.5f));

            Assert.AreEqual(PlacementBlock.UnitInTheWay, placement.Block);
        }

        [Test]
        public void Tooltip_Resource_ShowsKindAndAmount()
        {
            var text = TooltipText.ForResource(_tree);

            StringAssert.Contains("Kind: Wood", text);
            StringAssert.Contains("Amount: 50", text);
        }

        // ---- Debug view

        [Test]
        public void RemainingPath_IsWhatIsLeftToWalk()
        {
            var unit = Spawn(1, 1, Faction.Player);
            unit.SetPath(new[] { new Vector2(2.5f, 1.5f), new Vector2(3.5f, 1.5f) });
            var path = new List<Vector2>();

            unit.MoveAlongPath(1f);
            unit.CopyRemainingPath(path);

            CollectionAssert.AreEqual(new[] { new Vector2(3.5f, 1.5f) }, path);
        }
    }
}
