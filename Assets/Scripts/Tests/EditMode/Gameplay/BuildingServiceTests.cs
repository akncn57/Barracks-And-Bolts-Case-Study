using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class BuildingServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _service;
        private BuildingDefinition _barracks;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            _service = new BuildingService(_map, new GameEntityIdGenerator(), _eventBus);
            _barracks = TestDefinitions.CreateBuilding("barracks", new Vector2Int(3, 3), maxHp: 100);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_barracks);
        }

        [Test]
        public void TryPlace_FreeArea_OccupiesFootprintAndCreatesModel()
        {
            var placed = _service.TryPlace(_barracks, new Vector2Int(2, 2), Faction.Player, out var building);

            Assert.IsTrue(placed);
            Assert.AreEqual(new RectInt(2, 2, 3, 3), building.Area);
            Assert.AreEqual(100, building.CurrentHp);
            Assert.AreEqual(Faction.Player, building.Faction);
            Assert.AreEqual(building.Id, _map.GetOccupant(new Vector2Int(4, 4)));
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(5, 2)));
        }

        [Test]
        public void TryPlace_Overlapping_FailsAndKeepsFirstBuilding()
        {
            _service.TryPlace(_barracks, new Vector2Int(2, 2), Faction.Player, out var first);

            var placed = _service.TryPlace(_barracks, new Vector2Int(4, 4), Faction.Player, out var second);

            Assert.IsFalse(placed);
            Assert.IsNull(second);
            Assert.AreEqual(1, _service.Count);
            Assert.AreEqual(first.Id, _map.GetOccupant(new Vector2Int(4, 4)));
            Assert.IsTrue(_map.IsWalkable(new Vector2Int(5, 5)));
        }

        [Test]
        public void TryPlace_OutsideMap_Fails()
        {
            var placed = _service.TryPlace(_barracks, new Vector2Int(8, 8), Faction.Player, out _);

            Assert.IsFalse(placed);
            Assert.AreEqual(0, _service.Count);
        }

        [Test]
        public void TryPlace_GivesEveryBuildingItsOwnId()
        {
            _service.TryPlace(_barracks, new Vector2Int(0, 0), Faction.Player, out var first);
            _service.TryPlace(_barracks, new Vector2Int(5, 5), Faction.Enemy, out var second);

            Assert.AreNotEqual(first.Id, second.Id);
            Assert.IsTrue(_service.TryGet(second.Id, out var found));
            Assert.AreSame(second, found);
        }

        [Test]
        public void TryPlace_PublishesPlacedAndGridChangedEvents()
        {
            var placedEvents = new List<BuildingPlacedEvent>();
            var changedAreas = new List<RectInt>();
            _eventBus.Subscribe<BuildingPlacedEvent>(placedEvents.Add);
            _eventBus.Subscribe<GridChangedEvent>(e => changedAreas.Add(e.Area));

            _service.TryPlace(_barracks, new Vector2Int(1, 1), Faction.Player, out var building);

            Assert.AreEqual(1, placedEvents.Count);
            Assert.AreSame(building, placedEvents[0].Building);
            CollectionAssert.AreEqual(new[] { new RectInt(1, 1, 3, 3) }, changedAreas);
        }

        [Test]
        public void TryPlace_Failed_PublishesNothing()
        {
            _service.TryPlace(_barracks, new Vector2Int(0, 0), Faction.Player, out _);
            var eventCount = 0;
            _eventBus.Subscribe<BuildingPlacedEvent>(_ => eventCount++);
            _eventBus.Subscribe<GridChangedEvent>(_ => eventCount++);

            _service.TryPlace(_barracks, new Vector2Int(1, 1), Faction.Player, out _);

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void GetBuildingAt_FootprintAndRoof_FindTheBuilding()
        {
            var tall = TestDefinitions.CreateBuilding("tall", new Vector2Int(2, 2), visualHeight: 3f);
            Assert.IsTrue(_service.TryPlace(tall, new Vector2Int(2, 2), Faction.Player, out var building));

            Assert.AreEqual(building.Id, _service.GetBuildingAt(new Vector2(2.5f, 2.5f)));
            Assert.AreEqual(building.Id, _service.GetBuildingAt(new Vector2(3.5f, 4.5f)), "The roof above the footprint counts.");
            Assert.AreEqual(GameEntityId.None, _service.GetBuildingAt(new Vector2(3.5f, 5.5f)), "Above the roof.");
            Assert.AreEqual(GameEntityId.None, _service.GetBuildingAt(new Vector2(4.5f, 2.5f)), "Beside it.");
            Object.DestroyImmediate(tall);
        }

        [Test]
        public void GetBuildingAt_RoofInFrontOfAnotherBuilding_ReturnsTheFrontOne()
        {
            var tall = TestDefinitions.CreateBuilding("tall", new Vector2Int(2, 2), visualHeight: 3f);
            Assert.IsTrue(_service.TryPlace(tall, new Vector2Int(2, 4), Faction.Player, out var behind));
            Assert.IsTrue(_service.TryPlace(tall, new Vector2Int(2, 2), Faction.Player, out var front));

            Assert.AreEqual(front.Id, _service.GetBuildingAt(new Vector2(3f, 4.5f)), "The lower building is drawn in front.");
            Assert.AreEqual(behind.Id, _service.GetBuildingAt(new Vector2(3f, 5.5f)));
            Object.DestroyImmediate(tall);
        }

        [Test]
        public void VisualHeight_NeverLessThanTheFootprint()
        {
            var flat = TestDefinitions.CreateBuilding("flat", new Vector2Int(2, 3), visualHeight: 1f);

            Assert.AreEqual(3f, flat.VisualHeight);
            Object.DestroyImmediate(flat);
        }

        [Test]
        public void TryGet_UnknownId_ReturnsFalse()
        {
            Assert.IsFalse(_service.TryGet(new GameEntityId(99), out _));
        }
    }
}
