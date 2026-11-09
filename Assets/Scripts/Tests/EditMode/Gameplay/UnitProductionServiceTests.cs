using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class UnitProductionServiceTests
    {
        private GridMap _map;
        private BuildingService _buildings;
        private UnitService _units;
        private UnitProductionService _production;
        private UnitDefinition _knight;
        private UnitDefinition _monk;
        private BuildingDefinition _barracks;
        private BuildingDefinition _house;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(10, 10, Vector2.zero);
            var eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, eventBus);
            _production = new UnitProductionService(_units, TestDefinitions.CreateFreeResources(eventBus), TestDefinitions.CreateFreePopulation(_units, _buildings, eventBus));
            _knight = TestDefinitions.CreateUnit("knight");
            _monk = TestDefinitions.CreateUnit("monk");
            _barracks = TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), new Vector2Int(1, -1), _knight);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_knight);
            Object.DestroyImmediate(_monk);
            Object.DestroyImmediate(_barracks);
            Object.DestroyImmediate(_house);
        }

        private BuildingModel Place(BuildingDefinition definition, Vector2Int origin, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_buildings.TryPlace(definition, origin, faction, out var building));
            return building;
        }

        [Test]
        public void TryProduce_SpawnsOnTheBuildingsSpawnPoint()
        {
            var barracks = Place(_barracks, new Vector2Int(4, 4));

            Assert.IsTrue(_production.TryProduce(barracks, _knight, out var unit));

            Assert.AreEqual(new Vector2Int(5, 3), _map.WorldToCell(unit.Position));
            Assert.AreSame(_knight, unit.Definition);
        }

        [Test]
        public void TryProduce_UnitGetsTheBuildingsFaction()
        {
            var barracks = Place(_barracks, new Vector2Int(4, 4), Faction.Enemy);

            _production.TryProduce(barracks, _knight, out var unit);

            Assert.AreEqual(Faction.Enemy, unit.Faction);
        }

        [Test]
        public void TryProduce_NoLimit_SpawnPointTaken_UsesNearestFreeCell()
        {
            var barracks = Place(_barracks, new Vector2Int(4, 4));

            _production.TryProduce(barracks, _knight, out var first);
            Assert.IsTrue(_production.TryProduce(barracks, _knight, out var second));

            Assert.AreNotEqual(_map.WorldToCell(first.Position), _map.WorldToCell(second.Position));
            Assert.AreEqual(2, _units.Count);
        }

        [Test]
        public void TryProduce_SpawnPointOutsideMap_StaysOnTheMap()
        {
            var barracks = Place(_barracks, new Vector2Int(4, 0));

            Assert.IsTrue(_production.TryProduce(barracks, _knight, out var unit));

            Assert.IsTrue(_map.IsInBounds(_map.WorldToCell(unit.Position)));
            Assert.IsTrue(_map.IsWalkable(_map.WorldToCell(unit.Position)));
        }

        [Test]
        public void TryProduce_UnitNotInBuildingsList_Fails()
        {
            var barracks = Place(_barracks, new Vector2Int(4, 4));

            Assert.IsFalse(_production.TryProduce(barracks, _monk, out var unit));

            Assert.IsNull(unit);
            Assert.AreEqual(0, _units.Count);
        }

        [Test]
        public void TryProduce_BuildingProducesNothing_Fails()
        {
            var house = Place(_house, new Vector2Int(4, 4));

            Assert.IsFalse(_production.TryProduce(house, _knight, out _));
        }
    }
}
