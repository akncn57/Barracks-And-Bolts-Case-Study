using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.UI.Scroll;
using BarracksAndBolts.UI.Tooltips;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Production
{
    /// <summary>Lists every building in an endless scroll. Clicking one starts placing it.</summary>
    public sealed class ProductionMenuView : MonoBehaviour
    {
        [SerializeField] private RecyclingScrollView scrollView;

        private GameDatabase _database;
        private TeamService _teams;
        private PlacementService _placement;
        private SandboxService _sandbox;
        private TooltipView _tooltip;
        private IEventBus _eventBus;
        private TeamColorDefinition _colors;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<BuildingDefinition> _buildings = new();

        [Inject]
        private void Construct(GameDatabase database, TeamService teams, PlacementService placement, SandboxService sandbox, TooltipView tooltip, IEventBus eventBus)
        {
            _database = database;
            _teams = teams;
            _placement = placement;
            _sandbox = sandbox;
            _tooltip = tooltip;
            _eventBus = eventBus;
        }

        private void Start()
        {
            CollectBuildings();
            _eventBus.Subscribe<SandboxFactionChangedEvent>(_ => ShowBuildings()).AddTo(_subscriptions);
            ShowBuildings();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void CollectBuildings()
        {
            // A normal game starts with the one Castle it will ever have.
            foreach (var building in _database.Buildings)
            {
                if (_sandbox.IsEnabled || building != _database.NormalMode.Castle)
                    _buildings.Add(building);
            }
        }

        private void ShowBuildings()
        {
            // The menu shows the buildings in the color of the side being built for.
            _colors = _teams.GetColorSet(_sandbox.ActiveFaction);
            scrollView.Initialize<ProductionMenuCell>(_buildings.Count, OnCellCreated, BindCell);
        }

        private void OnCellCreated(ProductionMenuCell cell)
        {
            // Cells are destroyed together with this menu, so the handler never needs to be removed.
            cell.Clicked += OnBuildingClicked;
            cell.HoverStarted += OnCellHoverStarted;
            cell.HoverEnded += _ => _tooltip.Hide();
        }

        private void BindCell(ProductionMenuCell cell, int index)
        {
            var building = _buildings[index];
            cell.Bind(building, _colors.GetBuildingSprite(building));
        }

        private void OnCellHoverStarted(ProductionMenuCell cell)
        {
            _tooltip.Show(cell.Definition.DisplayName, TooltipText.ForBuilding(cell.Definition, showCost: !_sandbox.IsEnabled), cell.RectTransform);
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            _tooltip.Hide();
            _placement.Begin(building, _sandbox.ActiveFaction);
        }
    }
}
