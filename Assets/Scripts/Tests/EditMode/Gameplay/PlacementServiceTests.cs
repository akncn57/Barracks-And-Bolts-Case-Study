using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class PlacementServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private PlacementService _placement;
        private UnitDefinition _knight;
        private BuildingDefinition _house;
        private BuildingDefinition _barracks;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _placement = new PlacementService(_map, _buildings, _units, TestDefinitions.CreateFreeResources(_eventBus),
                TestDefinitions.CreateFreeTerritory(_map, _buildings, _eventBus), _eventBus);
            _knight = TestDefinitions.CreateUnit("knight");
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2), maxHp: 50);
            _barracks = TestDefinitions.CreateBuilding("barracks", new Vector2Int(3, 3));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_house);
            Object.DestroyImmediate(_barracks);
            Object.DestroyImmediate(_knight);
        }

        [Test]
        public void Begin_ActivatesWithoutAreaUntilPointerMoves()
        {
            _placement.Begin(_house, Faction.Player);

            Assert.IsTrue(_placement.IsActive);
            Assert.IsFalse(_placement.HasArea);
            Assert.IsFalse(_placement.IsAreaValid);
        }

        [Test]
        public void Begin_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _placement.Begin(null, Faction.Player));
        }

        [Test]
        public void MoveTo_CentersFootprintOnPointer()
        {
            _placement.Begin(_barracks, Faction.Player);

            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.IsTrue(_placement.HasArea);
            Assert.AreEqual(new RectInt(4, 4, 3, 3), _placement.Area);
        }

        [Test]
        public void MoveTo_WhileInactive_DoesNothing()
        {
            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.IsFalse(_placement.HasArea);
        }

        [Test]
        public void TryConfirm_FreeArea_PlacesBuildingAndEndsPlacement()
        {
            var endedCount = 0;
            _eventBus.Subscribe<PlacementEndedEvent>(_ => endedCount++);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            var confirmed = _placement.TryConfirm(keepPlacing: false);

            Assert.IsTrue(confirmed);
            Assert.AreEqual(1, _buildings.Count);
            Assert.IsFalse(_placement.IsActive);
            Assert.AreEqual(1, endedCount);
        }

        [Test]
        public void TryConfirm_KeepPlacing_StaysActiveWithSameBuilding()
        {
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            _placement.TryConfirm(keepPlacing: true);

            Assert.IsTrue(_placement.IsActive);
            Assert.AreSame(_house, _placement.Definition);
            Assert.IsFalse(_placement.IsAreaValid, "The cells under the pointer are now taken by the new house.");
        }

        [Test]
        public void TryConfirm_BlockedArea_FailsAndStaysActive()
        {
            _map.SetTerrainWalkable(new Vector2Int(3, 3), false);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            var confirmed = _placement.TryConfirm(keepPlacing: false);

            Assert.IsFalse(confirmed);
            Assert.AreEqual(0, _buildings.Count);
            Assert.IsTrue(_placement.IsActive);
        }

        [Test]
        public void AreaAcrossTwoLevels_IsInvalidAndHasBlockedCell()
        {
            foreach (var cell in new RectInt(3, 0, _map.Width - 3, _map.Height).allPositionsWithin)
                _map.SetLevel(cell, 1);

            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            Assert.IsFalse(_placement.IsAreaValid);
            var hasBlockedCell = false;
            foreach (var cell in _placement.Area.allPositionsWithin)
                hasBlockedCell |= !_placement.IsCellFree(cell);

            Assert.IsTrue(hasBlockedCell, "The preview must show why the footprint is refused.");
        }

        [Test]
        public void AreaOnHighGround_IsValid()
        {
            foreach (var cell in _map.Bounds.allPositionsWithin)
                _map.SetLevel(cell, 1);

            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            Assert.IsTrue(_placement.IsAreaValid);
            foreach (var cell in _placement.Area.allPositionsWithin)
                Assert.IsTrue(_placement.IsCellFree(cell));
        }

        [Test]
        public void AreaOnRamp_IsInvalidAndRampCellIsBlocked()
        {
            var ramp = new Vector2Int(3, 3);
            _map.SetRamp(ramp, true);

            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            Assert.IsFalse(_placement.IsAreaValid);
            Assert.IsFalse(_placement.IsCellFree(ramp));
        }

        [Test]
        public void TryConfirm_AreaHiddenOverUi_Fails()
        {
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));
            _placement.HideArea();

            Assert.IsFalse(_placement.TryConfirm(keepPlacing: false));
            Assert.AreEqual(0, _buildings.Count);
        }

        [Test]
        public void TryConfirm_UsesFactionGivenToBegin()
        {
            BuildingModel placed = null;
            _eventBus.Subscribe<BuildingPlacedEvent>(e => placed = e.Building);
            _placement.Begin(_house, Faction.Enemy);
            _placement.MoveTo(new Vector2(3f, 3f));

            _placement.TryConfirm(keepPlacing: false);

            Assert.AreEqual(Faction.Enemy, placed.Faction);
        }

        [Test]
        public void Begin_WhileActive_SwapsBuilding()
        {
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(3f, 3f));

            _placement.Begin(_barracks, Faction.Player);
            _placement.MoveTo(new Vector2(5.5f, 5.5f));

            Assert.AreSame(_barracks, _placement.Definition);
            Assert.AreEqual(new Vector2Int(3, 3), _placement.Area.size);
        }

        [Test]
        public void Cancel_EndsOnceAndPlacesNothing()
        {
            var endedCount = 0;
            _eventBus.Subscribe<PlacementEndedEvent>(_ => endedCount++);
            _placement.Begin(_house, Faction.Player);

            _placement.Cancel();
            _placement.Cancel();

            Assert.IsFalse(_placement.IsActive);
            Assert.AreEqual(1, endedCount);
            Assert.AreEqual(0, _buildings.Count);
        }

        [Test]
        public void UnitInsideFootprint_MakesAreaInvalidAndBlocksConfirm()
        {
            _units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out _);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(5f, 5f));

            Assert.IsFalse(_placement.IsAreaValid);
            Assert.IsFalse(_placement.TryConfirm(keepPlacing: false));
            Assert.IsTrue(_placement.IsActive);
            Assert.AreEqual(0, _buildings.Count);
        }

        [Test]
        public void UnitOutsideFootprint_DoesNotBlock()
        {
            _units.TrySpawn(_knight, new Vector2Int(8, 8), Faction.Player, out _);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(5f, 5f));

            Assert.IsTrue(_placement.IsAreaValid);
        }

        [Test]
        public void IsCellFree_FalseForUnitBuildingAndOutsideMap()
        {
            _units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out _);
            _map.TryOccupy(new RectInt(1, 1, 1, 1), new GameEntityId(99));

            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(5, 5)));
            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(1, 1)));
            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(-1, 0)));
            Assert.IsTrue(_placement.IsCellFree(new Vector2Int(7, 7)));
        }

        [Test]
        public void Block_TellsWhyTheAreaIsInvalid()
        {
            _placement.Begin(_house, Faction.Player);
            Assert.AreEqual(PlacementBlock.NoArea, _placement.Block);

            _placement.MoveTo(new Vector2(5f, 5f));
            Assert.AreEqual(PlacementBlock.None, _placement.Block);

            _placement.MoveTo(new Vector2(0f, 0f));
            Assert.AreEqual(PlacementBlock.OutsideMap, _placement.Block);

            _map.TryOccupy(new RectInt(5, 5, 1, 1), new GameEntityId(99));
            _placement.MoveTo(new Vector2(5f, 5f));
            Assert.AreEqual(PlacementBlock.Occupied, _placement.Block);

            _units.TrySpawn(_knight, new Vector2Int(8, 8), Faction.Player, out _);
            _placement.MoveTo(new Vector2(8f, 8f));
            Assert.AreEqual(PlacementBlock.UnitInTheWay, _placement.Block);
        }
    }
}
