using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Tests.EditMode.Gameplay;
using BarracksAndBolts.UI.Common;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.UI
{
    public class EscapeInputHandlerTests
    {
        private sealed class FakePanel : ICloseablePanel
        {
            public bool IsOpen { get; set; }

            public void Close()
            {
                IsOpen = false;
            }
        }

        private GameInputActions _inputActions;
        private UnitDefinition _knight;
        private UnitService _units;
        private SelectionService _selection;
        private FakePanel _panel;
        private EscapeInputHandler _handler;

        [SetUp]
        public void SetUp()
        {
            var map = new GridMap(10, 10, Vector2.zero);
            var eventBus = new EventBus();
            var ids = new GameEntityIdGenerator();
            _inputActions = new GameInputActions();
            _knight = TestDefinitions.CreateUnit("knight");
            _units = new UnitService(map, new GridSearch(map), ids, eventBus);
            _selection = new SelectionService(_units, new BuildingService(map, ids, eventBus), eventBus);
            _panel = new FakePanel { IsOpen = true };
            _handler = new EscapeInputHandler(_selection, _panel, _inputActions);
        }

        [TearDown]
        public void TearDown()
        {
            _inputActions.Dispose();
            Object.DestroyImmediate(_knight);
        }

        private void SelectAUnit()
        {
            Assert.IsTrue(_units.TrySpawn(_knight, new Vector2Int(2, 2), Faction.Player, out var unit));
            _selection.Select(unit.Id);
        }

        [Test]
        public void WithSelection_ClearsItAndKeepsThePanelOpen()
        {
            SelectAUnit();

            _handler.CloseTopLayer();

            Assert.IsFalse(_selection.HasSelection);
            Assert.IsTrue(_panel.IsOpen);
        }

        [Test]
        public void SecondPress_ClosesThePanel()
        {
            SelectAUnit();

            _handler.CloseTopLayer();
            _handler.CloseTopLayer();

            Assert.IsFalse(_panel.IsOpen);
        }

        [Test]
        public void WithoutSelection_ClosesThePanel()
        {
            _handler.CloseTopLayer();

            Assert.IsFalse(_panel.IsOpen);
        }

        [Test]
        public void WhileDrawingABox_ChangesNothing()
        {
            SelectAUnit();
            _selection.UpdateBox(Vector2.zero, Vector2.one);

            _handler.CloseTopLayer();

            Assert.IsTrue(_selection.HasSelection);
            Assert.IsTrue(_selection.HasBox);
            Assert.IsTrue(_panel.IsOpen);
        }

        [Test]
        public void WithNothingOpen_DoesNothing()
        {
            _panel.IsOpen = false;

            Assert.DoesNotThrow(() => _handler.CloseTopLayer());
            Assert.IsFalse(_selection.HasSelection);
        }
    }
}
