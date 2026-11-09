using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.UI.Common;
using BarracksAndBolts.UI.Tooltips;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>
    /// Shows the selected building: picture, name, HP and, for the player's production buildings, the units it can make.
    /// Hidden unless a building is selected; units have their own small card. Redraws only when the selection changes.
    /// </summary>
    public sealed class InformationPanelView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible panel. Hidden unless a building is selected; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private ContentFitPanel panelFit;
        [SerializeField] private ScrollRect scrollRect;
        [Tooltip("Closing the panel clears the selection, because the panel only shows what is selected.")]
        [SerializeField] private Button closeButton;

        [Header("Building")]
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text hpLabel;
        [Tooltip("Image with Image Type set to Filled.")]
        [SerializeField] private Image hpFill;

        [Header("Production")]
        [Tooltip("Title and list of producible units. Hidden for buildings that produce nothing.")]
        [SerializeField] private GameObject productionSection;
        [SerializeField] private Transform producibleContainer;
        [SerializeField] private ProducibleUnitCell producibleCellPrefab;

        private readonly List<ProducibleUnitCell> _cells = new();
        private readonly CompositeDisposable _subscriptions = new();
        private SelectionService _selection;
        private BuildingService _buildings;
        private TeamService _teams;
        private UnitProductionService _production;
        private SandboxService _sandbox;
        private TooltipView _tooltip;
        private IEventBus _eventBus;

        // The site on show and the percent last written, so the label is only rewritten when the number changes.
        private BuildingModel _shownSite;
        private int _shownPercent = -1;

        [Inject]
        private void Construct(SelectionService selection, BuildingService buildings, TeamService teams,
            UnitProductionService production, SandboxService sandbox, TooltipView tooltip, IEventBus eventBus)
        {
            _selection = selection;
            _buildings = buildings;
            _teams = teams;
            _production = production;
            _sandbox = sandbox;
            _tooltip = tooltip;
            _eventBus = eventBus;
        }

        private void Start()
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            _eventBus.Subscribe<SelectionChangedEvent>(_ => OnSelectionChanged()).AddTo(_subscriptions);
            _eventBus.Subscribe<SandboxFactionChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            _eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged).AddTo(_subscriptions);
            _eventBus.Subscribe<BuildingCompletedEvent>(OnBuildingCompleted).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            closeButton.onClick.RemoveListener(OnCloseClicked);
            _subscriptions.Dispose();
        }

        private void OnCloseClicked()
        {
            _selection.Clear();
        }

        private void OnSelectionChanged()
        {
            Refresh();

            // Something new is shown, so it starts from the top. A health update keeps the scroll position.
            scrollRect.verticalNormalizedPosition = 1f;
        }

        private void OnEntityDamaged(EntityDamagedEvent damagedEvent)
        {
            if (_selection.IsSelected(damagedEvent.Id))
                Refresh();
        }

        private void OnBuildingCompleted(BuildingCompletedEvent completedEvent)
        {
            if (_selection.IsSelected(completedEvent.Building.Id))
                Refresh();
        }

        // Work on a site moves every frame, with no event for it; only the progress line is kept up to date here.
        private void LateUpdate()
        {
            if (_shownSite == null || _shownSite.IsBuilt || GetPercent(_shownSite) == _shownPercent)
                return;

            ShowSiteProgress(_shownSite);
        }

        private void Refresh()
        {
            _shownSite = null;
            var hasBuilding = _buildings.TryGet(_selection.Primary, out var building);
            content.SetActive(hasBuilding);
            if (!hasBuilding)
                return;

            ShowBuilding(building);
            panelFit.Refit();
        }

        private void ShowBuilding(BuildingModel building)
        {
            var definition = building.Definition;
            picture.sprite = _teams.GetColorSet(building.Faction).GetBuildingSprite(definition);
            nameLabel.text = definition.DisplayName;
            ShowHp(building);
            if (!building.IsBuilt)
                ShowSiteProgress(building);

            // Only the side being built for lists its units. Outside sandbox mode that is always the player.
            // A site makes nothing until it is finished.
            var producibles = _production.GetProducibles(definition);
            var showProduction = producibles.Count > 0 && building.Faction == _sandbox.ActiveFaction && building.IsBuilt;
            productionSection.SetActive(showProduction);
            if (showProduction)
                ShowProducibles(producibles, _teams.GetColorSet(building.Faction));
        }

        private void ShowHp(BuildingModel building)
        {
            hpLabel.text = $"{building.CurrentHp} / {building.Definition.MaxHp}";
            hpFill.fillAmount = (float)building.CurrentHp / building.Definition.MaxHp;
        }

        private void ShowSiteProgress(BuildingModel site)
        {
            _shownSite = site;
            _shownPercent = GetPercent(site);
            nameLabel.text = $"{site.Definition.DisplayName}\n<size=70%>Under construction {_shownPercent}%</size>";

            // HP grows with the work.
            ShowHp(site);
        }

        private static int GetPercent(BuildingModel site)
        {
            return Mathf.FloorToInt(site.BuildProgress * 100f);
        }

        private void ShowProducibles(IReadOnlyList<UnitDefinition> units, TeamColorDefinition colors)
        {
            // Cells are kept and reused; a building never has more than a handful of units.
            while (_cells.Count < units.Count)
            {
                var cell = Instantiate(producibleCellPrefab, producibleContainer);

                // Cells are destroyed together with this panel, so the handler never needs to be removed.
                cell.Clicked += OnUnitClicked;
                cell.HoverStarted += OnCellHoverStarted;
                cell.HoverEnded += _ => _tooltip.Hide();
                _cells.Add(cell);
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var isUsed = i < units.Count;
                _cells[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    _cells[i].Bind(units[i], colors.GetUnitPortrait(units[i]));
            }
        }

        private void OnCellHoverStarted(ProducibleUnitCell cell)
        {
            _tooltip.Show(cell.Definition.DisplayName, TooltipText.ForUnit(cell.Definition, showCost: !_sandbox.IsEnabled), (RectTransform)cell.transform);
        }

        private void OnUnitClicked(UnitDefinition unit)
        {
            // The selection may have changed since the panel drew, so the building is looked up at click time.
            if (!_buildings.TryGet(_selection.Primary, out var building))
                return;

            var block = _production.GetBlock(building, unit);
            if (block == ProductionBlock.None)
                _production.TryProduce(building, unit, out _);
            else
                _eventBus.Publish(new FloatingTextRequestedEvent(GetBlockMessage(block), _buildings.GetVisibleArea(building).center, FloatingTextKind.Warning));
        }

        private static string GetBlockMessage(ProductionBlock block)
        {
            switch (block)
            {
                case ProductionBlock.NotEnoughResources:
                    return "Not enough resources";
                case ProductionBlock.NoHousing:
                    return "Build more houses";
                case ProductionBlock.UnderConstruction:
                    return "Still being built";
                default:
                    return "Can't make this here";
            }
        }
    }
}
