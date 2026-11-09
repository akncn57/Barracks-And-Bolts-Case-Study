using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Territory;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class TerritoryTests
    {
        // Footprint (10,10) to (11,11): the middle is the corner point (11,11).
        private static readonly Vector2Int CastleCell = new(10, 10);

        private GridMap _map;
        private EventBus _eventBus;
        private BuildingService _buildings;
        private UnitService _units;
        private TerritoryService _territory;
        private PlacementService _placement;
        private BuildingDefinition _castle;
        private BuildingDefinition _outpost;
        private BuildingDefinition _house;

        [SetUp]
        public void SetUp()
        {
            _map = new GridMap(40, 40, Vector2.zero);
            _eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _buildings = new BuildingService(_map, ids, _eventBus);
            _units = new UnitService(_map, new GridSearch(_map), ids, _eventBus);
            _territory = CreateTerritory(GameMode.Normal);
            _placement = new PlacementService(_map, _buildings, _units, TestDefinitions.CreateFreeResources(_eventBus), _territory, _eventBus);

            _castle = TestDefinitions.CreateTerritorySource("castle", 5f);
            _outpost = TestDefinitions.CreateTerritorySource("outpost", 3f);
            _house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2));
        }

        [TearDown]
        public void TearDown()
        {
            _territory.Dispose();
            Object.DestroyImmediate(_castle);
            Object.DestroyImmediate(_outpost);
            Object.DestroyImmediate(_house);
        }

        private TerritoryService CreateTerritory(GameMode mode)
        {
            var sandbox = new SandboxService(new GameSessionSettings { Mode = mode }, _eventBus);
            return new TerritoryService(_map, _buildings, sandbox, _eventBus);
        }

        private BuildingModel Place(BuildingDefinition definition, Vector2Int cell, Faction faction = Faction.Player)
        {
            Assert.IsTrue(_buildings.TryPlace(definition, cell, faction, out var building));
            return building;
        }

        [Test]
        public void NoSource_NothingIsInside()
        {
            Assert.IsFalse(_territory.IsInside(new Vector2Int(10, 10)));
        }

        [Test]
        public void Source_OpensACircleAroundItsMiddle()
        {
            Place(_castle, CastleCell);

            Assert.IsTrue(_territory.IsInside(new Vector2Int(15, 10)), "4.5 and 0.5 away.");
            Assert.IsFalse(_territory.IsInside(new Vector2Int(16, 10)), "5.5 away.");
            Assert.IsFalse(_territory.IsInside(new Vector2Int(15, 15)), "The corner of the square is outside the circle.");
        }

        [Test]
        public void OddFootprint_LeavesNoSpikeOnTheSides()
        {
            var castle = TestDefinitions.CreateTerritorySource("odd", 5f);
            var serialized = new UnityEditor.SerializedObject(castle);
            serialized.FindProperty("size").vector2IntValue = new Vector2Int(3, 3);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // The middle is (11.5, 11.5), so this cell's middle lies exactly on the radius.
            Place(castle, CastleCell);

            Assert.IsFalse(_territory.IsInside(new Vector2Int(16, 11)));
            Assert.IsTrue(_territory.IsInside(new Vector2Int(15, 11)));
            Object.DestroyImmediate(castle);
        }

        [Test]
        public void CircleAtTheEdge_StaysOnTheMap()
        {
            Place(_castle, Vector2Int.zero);

            Assert.IsTrue(_territory.IsInside(Vector2Int.zero));
            Assert.IsFalse(_territory.IsInside(new Vector2Int(-1, 0)));
        }

        [Test]
        public void Area_MustBeInsideAsAWhole()
        {
            Place(_castle, CastleCell);

            Assert.IsTrue(_territory.IsInside(new RectInt(13, 10, 2, 2)));
            Assert.IsFalse(_territory.IsInside(new RectInt(15, 10, 2, 2)));
        }

        [Test]
        public void SecondSource_GrowsTheTerritory()
        {
            Place(_castle, CastleCell);
            var far = new Vector2Int(17, 10);
            Assert.IsFalse(_territory.IsInside(far));

            Place(_outpost, new Vector2Int(15, 10));

            Assert.IsTrue(_territory.IsInside(far));
        }

        [Test]
        public void SourceLost_ShrinksTheTerritory_ButBuildingsStay()
        {
            Place(_castle, CastleCell);
            var outpost = Place(_outpost, new Vector2Int(15, 10));
            var house = Place(_house, new Vector2Int(17, 10));

            _buildings.Remove(outpost.Id);

            Assert.IsFalse(_territory.IsInside(new Vector2Int(17, 10)));
            Assert.IsTrue(_territory.IsInside(new Vector2Int(14, 10)), "What the Castle covers stays.");
            Assert.IsTrue(_buildings.TryGet(house.Id, out _));
        }

        [Test]
        public void EnemySource_OpensNothing()
        {
            Place(_castle, CastleCell, Faction.Enemy);

            Assert.IsFalse(_territory.IsInside(CastleCell));
        }

        [Test]
        public void Version_ChangesOnlyWhenASourceComesOrGoes()
        {
            var version = _territory.Version;

            var house = Place(_house, new Vector2Int(20, 20));
            _buildings.Remove(house.Id);
            Assert.AreEqual(version, _territory.Version);

            Place(_castle, CastleCell);
            Assert.Greater(_territory.Version, version);
        }

        [Test]
        public void CreatedAfterTheCastle_StillSeesIt()
        {
            Place(_castle, CastleCell);

            var late = CreateTerritory(GameMode.Normal);

            Assert.IsTrue(late.IsInside(CastleCell));
            late.Dispose();
        }

        [Test]
        public void Sandbox_EverythingIsInside()
        {
            var sandbox = CreateTerritory(GameMode.Sandbox);

            Assert.IsFalse(sandbox.IsEnabled);
            Assert.IsTrue(sandbox.IsInside(new Vector2Int(30, 30)));
            Assert.IsTrue(sandbox.IsInside(new RectInt(30, 30, 3, 3)));
            sandbox.Dispose();
        }

        [Test]
        public void Placement_OutsideTerritory_IsRefusedWithItsOwnReason()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(30f, 30f));

            Assert.AreEqual(PlacementBlock.OutsideTerritory, _placement.Block);
            Assert.IsFalse(_placement.TryConfirm(keepPlacing: false));
            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(30, 30)));
        }

        [Test]
        public void Placement_InsideTerritory_Works()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(14f, 11f));

            Assert.AreEqual(PlacementBlock.None, _placement.Block);
            Assert.IsTrue(_placement.TryConfirm(keepPlacing: false));
        }

        [Test]
        public void Placement_HalfOutside_MarksOnlyTheCellsOutside()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_house, Faction.Player);
            _placement.MoveTo(new Vector2(16f, 11f));

            Assert.AreEqual(PlacementBlock.OutsideTerritory, _placement.Block);
            Assert.IsTrue(_placement.IsCellFree(new Vector2Int(15, 10)));
            Assert.IsFalse(_placement.IsCellFree(new Vector2Int(16, 10)));
        }

        [Test]
        public void Preview_ShowsForASourceInsideTheTerritory()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_outpost, Faction.Player);
            Assert.IsFalse(_placement.ShowsTerritoryPreview, "The pointer isn't on the map yet.");

            _placement.MoveTo(new Vector2(14f, 11f));
            Assert.IsTrue(_placement.ShowsTerritoryPreview);

            _placement.HideArea();
            Assert.IsFalse(_placement.ShowsTerritoryPreview, "The pointer is on a panel.");
        }

        [Test]
        public void Preview_NotShownWhereTheSourceCantStand()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_outpost, Faction.Player);

            _placement.MoveTo(new Vector2(30f, 30f));

            Assert.IsFalse(_placement.ShowsTerritoryPreview);
        }

        [Test]
        public void Preview_NotShownForABuildingThatOpensNothing()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_house, Faction.Player);

            _placement.MoveTo(new Vector2(14f, 11f));

            Assert.IsFalse(_placement.ShowsTerritoryPreview);
        }

        [Test]
        public void Preview_EndsWithPlacement()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_outpost, Faction.Player);
            _placement.MoveTo(new Vector2(14f, 11f));

            _placement.Cancel();

            Assert.IsFalse(_placement.ShowsTerritoryPreview);
        }

        [Test]
        public void Preview_MatchesWhatTheBuildingThenOpens()
        {
            Place(_castle, CastleCell);
            _placement.Begin(_outpost, Faction.Player);
            _placement.MoveTo(new Vector2(14f, 11f));
            var center = _placement.Area.center;
            var radius = _placement.Definition.TerritoryRadius;

            Assert.IsTrue(_placement.TryConfirm(keepPlacing: false));

            foreach (var cell in _map.Bounds.allPositionsWithin)
            {
                if (TerritoryService.IsInCircle(cell, center, radius))
                    Assert.IsTrue(_territory.IsInside(cell), cell.ToString());
            }
        }

        [Test]
        public void Tooltip_ShowsTerritoryOnlyInANormalGame()
        {
            StringAssert.Contains("Territory: 3", TooltipText.ForBuilding(_outpost, isNormalMode: true));
            StringAssert.DoesNotContain("Territory", TooltipText.ForBuilding(_outpost));
            StringAssert.DoesNotContain("Territory", TooltipText.ForBuilding(_house, isNormalMode: true));
        }
    }
}
