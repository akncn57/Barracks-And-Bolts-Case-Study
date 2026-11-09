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
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Production
{
    /// <summary>
    /// Lists every building in an endless scroll. Clicking one starts placing it. In sandbox mode a second tab lists
    /// the units, which are placed straight onto the map; a normal game makes units in buildings only.
    /// </summary>
    public sealed class ProductionMenuView : MonoBehaviour
    {
        private enum Tab
        {
            Buildings,
            Units
        }

        [SerializeField] private RecyclingScrollView scrollView;

        [Header("Tabs (sandbox only)")]
        [SerializeField] private Button buildingsTab;
        [SerializeField] private Button unitsTab;
        [Tooltip("The list area. Without tabs it grows up into their space.")]
        [SerializeField] private RectTransform viewport;
        [Tooltip("Height the tabs take above the list, gap included.")]
        [SerializeField, Min(0f)] private float tabsSpace = 51f;
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor = new(0.6f, 0.6f, 0.6f, 1f);

        private GameDatabase _database;
        private TeamService _teams;
        private PlacementService _placement;
        private SandboxService _sandbox;
        private TooltipView _tooltip;
        private IEventBus _eventBus;
        private TeamColorDefinition _colors;
        private Tab _tab;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly List<BuildingDefinition> _buildings = new();
        private readonly List<UnitDefinition> _units = new();

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
            CollectItems();
            SetUpTabs();
            _eventBus.Subscribe<SandboxFactionChangedEvent>(_ => ShowList()).AddTo(_subscriptions);
            ShowList();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            if (buildingsTab != null)
                buildingsTab.onClick.RemoveListener(OnBuildingsTabClicked);

            if (unitsTab != null)
                unitsTab.onClick.RemoveListener(OnUnitsTabClicked);
        }

        private void CollectItems()
        {
            foreach (var building in _database.Buildings)
            {
                // A normal game starts with the one Castle it will ever have.
                var isShown = _sandbox.IsEnabled ? !building.IsNormalModeOnly : building != _database.NormalMode.Castle;
                if (isShown)
                    _buildings.Add(building);
            }

            if (!_sandbox.IsEnabled)
                return;

            foreach (var unit in _database.Units)
                _units.Add(unit);
        }

        private void SetUpTabs()
        {
            if (buildingsTab == null || unitsTab == null)
                return;

            if (!_sandbox.IsEnabled)
            {
                // Buildings only, so the list takes the tabs' place.
                buildingsTab.gameObject.SetActive(false);
                unitsTab.gameObject.SetActive(false);
                if (viewport != null)
                    viewport.offsetMax += new Vector2(0f, tabsSpace);

                return;
            }

            buildingsTab.onClick.AddListener(OnBuildingsTabClicked);
            unitsTab.onClick.AddListener(OnUnitsTabClicked);
            UpdateTabLook();
        }

        private void OnBuildingsTabClicked()
        {
            SelectTab(Tab.Buildings);
        }

        private void OnUnitsTabClicked()
        {
            SelectTab(Tab.Units);
        }

        private void SelectTab(Tab tab)
        {
            if (_tab == tab)
                return;

            _tab = tab;
            _tooltip.Hide();
            UpdateTabLook();
            ShowList();
        }

        private void UpdateTabLook()
        {
            SetTabColor(buildingsTab, _tab == Tab.Buildings);
            SetTabColor(unitsTab, _tab == Tab.Units);
        }

        private void SetTabColor(Button tab, bool isActive)
        {
            if (tab.targetGraphic != null)
                tab.targetGraphic.color = isActive ? activeTabColor : inactiveTabColor;
        }

        private void ShowList()
        {
            // The menu shows everything in the color of the side being built for.
            _colors = _teams.GetColorSet(_sandbox.ActiveFaction);
            var count = _tab == Tab.Units ? _units.Count : _buildings.Count;
            scrollView.Initialize<ProductionMenuCell>(count, OnCellCreated, BindCell);
        }

        private void OnCellCreated(ProductionMenuCell cell)
        {
            // Cells are destroyed together with this menu, so the handler never needs to be removed.
            cell.Clicked += OnCellClicked;
            cell.HoverStarted += OnCellHoverStarted;
            cell.HoverEnded += _ => _tooltip.Hide();
        }

        private void BindCell(ProductionMenuCell cell, int index)
        {
            if (_tab == Tab.Units)
            {
                var unit = _units[index];
                cell.Bind(index, unit.DisplayName, _colors.GetUnitPortrait(unit));
                return;
            }

            var building = _buildings[index];
            cell.Bind(index, building.DisplayName, _colors.GetBuildingSprite(building));
        }

        private void OnCellHoverStarted(ProductionMenuCell cell)
        {
            if (_tab == Tab.Units)
            {
                var unit = _units[cell.Index];
                _tooltip.Show(unit.DisplayName, TooltipText.ForUnit(unit), cell.RectTransform);
                return;
            }

            var building = _buildings[cell.Index];
            _tooltip.Show(building.DisplayName, TooltipText.ForBuilding(building, isNormalMode: !_sandbox.IsEnabled), cell.RectTransform);
        }

        private void OnCellClicked(ProductionMenuCell cell)
        {
            _tooltip.Hide();
            if (_tab == Tab.Units)
                _placement.BeginUnit(_units[cell.Index], _sandbox.ActiveFaction);
            else
                _placement.Begin(_buildings[cell.Index], _sandbox.ActiveFaction);
        }
    }
}
