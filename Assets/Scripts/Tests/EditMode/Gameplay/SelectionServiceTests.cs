using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
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
        private SelectionService _selection;
        private BuildingDefinition _house;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            _buildings = new BuildingService(_map, new GameEntityIdGenerator(), _eventBus);
            _selection = new SelectionService(_map, _eventBus);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
            _changedCount = 0;
            _eventBus.Subscribe<SelectionChangedEvent>(_ => _changedCount++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_house);
        }

        private BuildingModel Place(Vector2Int origin)
        {
            Assert.IsTrue(_buildings.TryPlace(_house, origin, Faction.Player, out var building));
            return building;
        }

        [Test]
        public void StartsEmpty()
        {
            Assert.IsFalse(_selection.HasSelection);
            Assert.AreEqual(GameEntityId.None, _selection.Primary);
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
    }
}
