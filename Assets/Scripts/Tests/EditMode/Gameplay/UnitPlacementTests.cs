using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    /// <summary>The sandbox places units straight onto the map, with the same mode that places buildings.</summary>
    public class UnitPlacementTests
    {
        private readonly List<Object> _created = new();
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private PlacementService _placement;
        private UnitDefinition _knight;
        private BuildingDefinition _house;
        private int _startedCount;
        private int _endedCount;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(12, 12, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _placement = new PlacementService(_map, _buildings, _units, new ResourceNodeService(_map, new GameEntityIdGenerator(), _eventBus), TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);

            _knight = Track(TestDefinitions.CreateUnit("knight"));
            _house = Track(TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2)));

            _startedCount = 0;
            _endedCount = 0;
            _eventBus.Subscribe<PlacementStartedEvent>(_ => _startedCount++);
            _eventBus.Subscribe<PlacementEndedEvent>(_ => _endedCount++);
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

        [Test]
        public void BeginUnit_StartsPlacementMode()
        {
            _placement.BeginUnit(_knight, Faction.Enemy);

            Assert.IsTrue(_placement.IsActive);
            Assert.IsTrue(_placement.IsPlacingUnit);
            Assert.IsNull(_placement.Definition);
            Assert.AreEqual(Faction.Enemy, _placement.Faction);
            Assert.AreEqual(1, _startedCount, "The input maps switch like for a building.");
        }

        [Test]
        public void Footprint_IsTheCellUnderThePointer()
        {
            _placement.BeginUnit(_knight, Faction.Player);

            _placement.MoveTo(new Vector2(5.7f, 3.2f));

            Assert.AreEqual(new RectInt(5, 3, 1, 1), _placement.Area);
        }

        [Test]
        public void Confirm_PutsTheUnitOnTheCell_ForTheChosenSide()
        {
            _placement.BeginUnit(_knight, Faction.Enemy);
            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.IsTrue(_placement.TryConfirm(keepPlacing: false));

            var unit = _units.TryGet(_units.GetUnitAt(new Vector2(5.5f, 5.6f)), out var placed) ? placed : null;
            Assert.IsNotNull(unit);
            Assert.AreEqual(_knight, unit.Definition);
            Assert.AreEqual(Faction.Enemy, unit.Faction);
            Assert.AreEqual(new Vector2Int(5, 5), _map.WorldToCell(unit.Position));
            Assert.IsFalse(_placement.IsActive);
            Assert.AreEqual(1, _endedCount);
        }

        [Test]
        public void ConfirmAndContinue_KeepsPlacing_AndASecondUnitStepsAside()
        {
            _placement.BeginUnit(_knight, Faction.Player);
            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.IsTrue(_placement.TryConfirm(keepPlacing: true));
            Assert.IsTrue(_placement.TryConfirm(keepPlacing: true));

            Assert.IsTrue(_placement.IsActive);
            Assert.AreEqual(2, _units.Count);
            Assert.AreEqual(0, _endedCount);
        }

        [Test]
        public void BlockedCell_IsRefused()
        {
            _buildings.TryPlace(_house, new Vector2Int(5, 5), Faction.Player, out _);
            _map.SetTerrainWalkable(new Vector2Int(1, 1), false);
            _placement.BeginUnit(_knight, Faction.Player);

            _placement.MoveTo(new Vector2(5.5f, 5.5f));
            Assert.AreEqual(PlacementBlock.Occupied, _placement.Block, "A building.");
            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(5, 5)));
            Assert.IsFalse(_placement.TryConfirm(keepPlacing: false));

            _placement.MoveTo(new Vector2(1.5f, 1.5f));
            Assert.AreEqual(PlacementBlock.Occupied, _placement.Block, "Water.");

            Assert.AreEqual(0, _units.Count);
            Assert.IsTrue(_placement.IsActive, "The player can try another cell.");
        }

        [Test]
        public void OutsideTheMap_IsRefused()
        {
            _placement.BeginUnit(_knight, Faction.Player);

            _placement.MoveTo(new Vector2(-3f, 4f));

            Assert.AreEqual(PlacementBlock.OutsideMap, _placement.Block);
        }

        [Test]
        public void SwitchingBetweenBuildingAndUnit_KeepsOnlyTheLast()
        {
            _placement.Begin(_house, Faction.Player);
            _placement.BeginUnit(_knight, Faction.Player);
            Assert.IsNull(_placement.Definition);
            Assert.AreEqual(_knight, _placement.Unit);

            _placement.Begin(_house, Faction.Player);
            Assert.IsNull(_placement.Unit);
            Assert.AreEqual(_house, _placement.Definition);
            Assert.AreEqual(0, _endedCount, "Swapping needs no cancel.");
        }

        [Test]
        public void Unit_ShowsNoTerritoryPreview()
        {
            _placement.BeginUnit(_knight, Faction.Player);
            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.IsFalse(_placement.ShowsTerritoryPreview);
        }

        [Test]
        public void Cancel_EndsUnitPlacement()
        {
            _placement.BeginUnit(_knight, Faction.Player);

            _placement.Cancel();

            Assert.IsFalse(_placement.IsActive);
            Assert.IsNull(_placement.Unit);
            Assert.AreEqual(1, _endedCount);
        }
    }
}
