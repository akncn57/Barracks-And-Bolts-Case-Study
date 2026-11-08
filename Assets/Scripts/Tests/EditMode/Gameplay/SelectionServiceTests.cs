using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
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
    public class SelectionServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private SelectionService _selection;
        private BuildingDefinition _house;
        private UnitDefinition _knight;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _selection = new SelectionService(_units, _buildings, _eventBus);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
            _knight = TestDefinitions.CreateUnit("knight");
            _changedCount = 0;
            _eventBus.Subscribe<SelectionChangedEvent>(_ => _changedCount++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_house);
            Object.DestroyImmediate(_knight);
        }

        private BuildingModel Place(Vector2Int origin)
        {
            Assert.IsTrue(_buildings.TryPlace(_house, origin, Faction.Player, out var building));
            return building;
        }

        private UnitModel Spawn(Vector2Int cell, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_units.TrySpawn(_knight, cell, faction, out var unit));
            return unit;
        }

        [Test]
        public void StartsEmpty()
        {
            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(GameEntityId.None, _selection.Primary);
            Assert.IsFalse(_selection.HasBox);
        }

        [Test]
        public void SelectAt_AnyCellOfBuilding_SelectsIt()
        {
            var building = Place(new Vector2Int(2, 2));

            // Top-right cell of the 2x2 footprint, not the origin.
            _selection.SelectAt(new Vector2(3.5f, 3.5f));

            Assert.AreEqual(building.Id, _selection.Primary);
            Assert.IsTrue(_selection.IsSelected(building.Id));
            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void SelectAt_OnTheRoofAboveTheFootprint_SelectsTheBuilding()
        {
            var tall = TestDefinitions.CreateBuilding("tall", new Vector2Int(2, 2), visualHeight: 3f);
            Assert.IsTrue(_buildings.TryPlace(tall, new Vector2Int(2, 2), Faction.Player, out var building));

            _selection.SelectAt(new Vector2(3f, 4.5f));

            Assert.AreEqual(building.Id, _selection.Primary);
            Object.DestroyImmediate(tall);
        }

        [Test]
        public void SelectAt_EmptyCell_ClearsSelection()
        {
            Place(new Vector2Int(2, 2));
            _selection.SelectAt(new Vector2(2.5f, 2.5f));

            _selection.SelectAt(new Vector2(7.5f, 7.5f));

            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(2, _changedCount);
        }

        [Test]
        public void SelectAt_OutsideMap_ClearsSelection()
        {
            Place(new Vector2Int(2, 2));
            _selection.SelectAt(new Vector2(2.5f, 2.5f));

            _selection.SelectAt(new Vector2(-3f, 20f));

            Assert.IsFalse(_selection.HasSelection);
        }

        [Test]
        public void SelectAt_OnUnit_SelectsTheUnit()
        {
            var unit = Spawn(new Vector2Int(5, 5));

            _selection.SelectAt(unit.Position + Vector2.up * 0.5f);

            Assert.AreEqual(unit.Id, _selection.Primary);
        }

        [Test]
        public void SelectAt_UnitInFrontOfBuilding_PrefersTheUnit()
        {
            Place(new Vector2Int(5, 6));
            var unit = Spawn(new Vector2Int(5, 5));

            // The unit's head reaches into the building's bottom cell.
            _selection.SelectAt(new Vector2(unit.Position.x, 6.2f));

            Assert.AreEqual(unit.Id, _selection.Primary);
        }

        [Test]
        public void SelectAt_EnemyUnit_CanBeSelected()
        {
            var enemy = Spawn(new Vector2Int(5, 5), Faction.Enemy);

            _selection.SelectAt(enemy.Position);

            Assert.AreEqual(enemy.Id, _selection.Primary);
        }

        [Test]
        public void Select_SameEntityTwice_PublishesOnce()
        {
            var building = Place(new Vector2Int(2, 2));

            _selection.Select(building.Id);
            _selection.Select(building.Id);

            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void Select_OtherEntity_ReplacesSelection()
        {
            var first = Place(new Vector2Int(0, 0));
            var second = Place(new Vector2Int(5, 5));

            _selection.Select(first.Id);
            _selection.Select(second.Id);

            Assert.AreEqual(1, _selection.Selected.Count);
            Assert.AreEqual(second.Id, _selection.Primary);
            Assert.IsFalse(_selection.IsSelected(first.Id));
            Assert.AreEqual(2, _changedCount);
        }

        [Test]
        public void Select_InvalidId_Clears()
        {
            _selection.Select(Place(new Vector2Int(2, 2)).Id);

            _selection.Select(GameEntityId.None);

            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(2, _changedCount);
        }

        [Test]
        public void Clear_WhenEmpty_DoesNotPublish()
        {
            _selection.Clear();
            _selection.SelectAt(new Vector2(5.5f, 5.5f));

            Assert.AreEqual(0, _changedCount);
        }

        [Test]
        public void UpdateBox_AnyCornerOrder_GivesTheSameRect()
        {
            _selection.UpdateBox(new Vector2(4f, 1f), new Vector2(2f, 3f));

            Assert.IsTrue(_selection.HasBox);
            Assert.AreEqual(Rect.MinMaxRect(2f, 1f, 4f, 3f), _selection.Box);
        }

        [Test]
        public void UpdateBox_DoesNotChangeSelectionUntilConfirmed()
        {
            Spawn(new Vector2Int(3, 3));

            _selection.UpdateBox(Vector2.zero, new Vector2(6f, 6f));

            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(0, _changedCount);
        }

        [Test]
        public void ConfirmBox_SelectsPlayerUnitsInside()
        {
            var inside = Spawn(new Vector2Int(2, 2));
            var alsoInside = Spawn(new Vector2Int(3, 3));
            var outside = Spawn(new Vector2Int(8, 8));

            _selection.UpdateBox(Vector2.zero, new Vector2(5f, 5f));
            _selection.ConfirmBox();

            Assert.IsFalse(_selection.HasBox);
            Assert.AreEqual(2, _selection.Selected.Count);
            Assert.IsTrue(_selection.IsSelected(inside.Id));
            Assert.IsTrue(_selection.IsSelected(alsoInside.Id));
            Assert.IsFalse(_selection.IsSelected(outside.Id));
            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void ConfirmBox_SkipsEnemyUnitsAndBuildings()
        {
            var enemy = Spawn(new Vector2Int(2, 2), Faction.Enemy);
            var building = Place(new Vector2Int(3, 3));

            _selection.UpdateBox(Vector2.zero, new Vector2(6f, 6f));
            _selection.ConfirmBox();

            Assert.IsFalse(_selection.IsSelected(enemy.Id));
            Assert.IsFalse(_selection.IsSelected(building.Id));
            Assert.IsFalse(_selection.HasSelection);
        }

        [Test]
        public void ConfirmBox_Empty_ClearsSelection()
        {
            _selection.Select(Spawn(new Vector2Int(8, 8)).Id);

            _selection.UpdateBox(Vector2.zero, new Vector2(2f, 2f));
            _selection.ConfirmBox();

            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(2, _changedCount);
        }

        [Test]
        public void ConfirmBox_SameUnitsAgain_DoesNotPublish()
        {
            Spawn(new Vector2Int(2, 2));
            _selection.UpdateBox(Vector2.zero, new Vector2(5f, 5f));
            _selection.ConfirmBox();

            _selection.UpdateBox(Vector2.zero, new Vector2(5f, 5f));
            _selection.ConfirmBox();

            Assert.AreEqual(1, _changedCount);
        }

        [Test]
        public void ConfirmBox_WithoutBox_DoesNothing()
        {
            var unit = Spawn(new Vector2Int(2, 2));
            _selection.Select(unit.Id);

            _selection.ConfirmBox();

            Assert.AreEqual(unit.Id, _selection.Primary);
        }

        [Test]
        public void CancelBox_HidesBoxAndKeepsSelection()
        {
            var unit = Spawn(new Vector2Int(8, 8));
            _selection.Select(unit.Id);

            _selection.UpdateBox(Vector2.zero, new Vector2(2f, 2f));
            _selection.CancelBox();

            Assert.IsFalse(_selection.HasBox);
            Assert.AreEqual(unit.Id, _selection.Primary);
        }
    }
}
