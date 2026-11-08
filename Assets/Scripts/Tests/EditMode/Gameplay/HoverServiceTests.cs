using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class HoverServiceTests
    {
        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private HoverService _hover;
        private BuildingDefinition _house;
        private UnitDefinition _knight;
        private int _changed;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _hover = new HoverService(_units, _buildings, _eventBus);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2), visualHeight: 2.5f);
            _knight = TestDefinitions.CreateUnit("knight");
            _changed = 0;
            _eventBus.Subscribe<HoverChangedEvent>(_ => _changed++);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_house);
            Object.DestroyImmediate(_knight);
        }

        private BuildingModel Place(Vector2Int origin, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_buildings.TryPlace(_house, origin, faction, out var building));
            return building;
        }

        [Test]
        public void StartsWithNothing()
        {
            Assert.AreEqual(GameEntityId.None, _hover.Hovered);
            Assert.IsFalse(_hover.TryGetInfo(out _));
        }

        [Test]
        public void OverBuilding_GivesItsNameFactionAndVisibleArea()
        {
            var building = Place(new Vector2Int(2, 2), Faction.Enemy);

            _hover.UpdateAt(new Vector2(3f, 4.2f));

            Assert.AreEqual(building.Id, _hover.Hovered);
            Assert.IsTrue(_hover.TryGetInfo(out var info));
            Assert.AreEqual("house", info.Name);
            Assert.AreEqual(Faction.Enemy, info.Faction);
            Assert.AreEqual(new Rect(2f, 2f, 2f, 2.5f), info.Bounds);
        }

        [Test]
        public void OverUnit_GivesItsNameAndBody()
        {
            Assert.IsTrue(_units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out var unit));

            _hover.UpdateAt(unit.Position);

            Assert.IsTrue(_hover.TryGetInfo(out var info));
            Assert.AreEqual("knight", info.Name);
            Assert.AreEqual(unit.Body, info.Bounds);
        }

        [Test]
        public void UnitInFrontOfBuilding_WinsTheHover()
        {
            Place(new Vector2Int(5, 6));
            Assert.IsTrue(_units.TrySpawn(_knight, new Vector2Int(5, 5), Faction.Player, out var unit));

            _hover.UpdateAt(new Vector2(unit.Position.x, 6.2f));

            Assert.AreEqual(unit.Id, _hover.Hovered);
        }

        [Test]
        public void EmptyGround_ClearsTheHover()
        {
            Place(new Vector2Int(2, 2));
            _hover.UpdateAt(new Vector2(3f, 3f));

            _hover.UpdateAt(new Vector2(8f, 8f));

            Assert.AreEqual(GameEntityId.None, _hover.Hovered);
            Assert.IsFalse(_hover.TryGetInfo(out _));
        }

        [Test]
        public void Event_OnlyWhenTheTargetChanges()
        {
            Place(new Vector2Int(2, 2));

            _hover.UpdateAt(new Vector2(3f, 3f));
            _hover.UpdateAt(new Vector2(3.1f, 3.1f));
            _hover.Clear();
            _hover.Clear();

            Assert.AreEqual(2, _changed);
        }

        [Test]
        public void HoveredEntityRemoved_GivesNoInfo()
        {
            var building = Place(new Vector2Int(2, 2));
            _hover.UpdateAt(new Vector2(3f, 3f));

            _buildings.Remove(building.Id);

            Assert.IsFalse(_hover.TryGetInfo(out _));
        }
    }
}
