using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.UI.Common;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>
    /// Shows the selected building: picture, name, HP and, for the player's production buildings, the units it can make.
    /// For selected units it shows the first one's picture and the health of the whole group.
    /// Hidden while nothing is selected. Redraws only when the selection changes.
    /// </summary>
    public sealed class InformationPanelView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible panel. Hidden while nothing is selected; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private ContentFitPanel panelFit;
        [SerializeField] private ScrollRect scrollRect;

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
        private UnitService _units;
        private SandboxService _sandbox;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(SelectionService selection, BuildingService buildings, TeamService teams,
            UnitProductionService production, UnitService units, SandboxService sandbox, IEventBus eventBus)
        {
            _selection = selection;
            _buildings = buildings;
            _teams = teams;
            _production = production;
            _units = units;
            _sandbox = sandbox;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _eventBus.Subscribe<SelectionChangedEvent>(_ => OnSelectionChanged()).AddTo(_subscriptions);
            _eventBus.Subscribe<SandboxFactionChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            _eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
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

        private void Refresh()
        {
            var primary = _selection.Primary;
            if (_buildings.TryGet(primary, out var building))
            {
                content.SetActive(true);
                ShowBuilding(building);
            }
            else if (_units.TryGet(primary, out var unit))
            {
                content.SetActive(true);
                ShowUnits(unit);
            }
            else
            {
                content.SetActive(false);
                return;
            }

            panelFit.Refit();
        }

        private void ShowUnits(UnitModel primary)
        {
            productionSection.SetActive(false);
            picture.sprite = primary.Definition.Portrait;

            var count = _selection.Selected.Count;
            nameLabel.text = count == 1 ? primary.Definition.DisplayName : $"{count} Units";

            // A group shows the health of all its units together.
            var currentHp = 0;
            var maxHp = 0;
            foreach (var id in _selection.Selected)
            {
                if (!_units.TryGet(id, out var unit))
                    continue;

                currentHp += unit.CurrentHp;
                maxHp += unit.Definition.MaxHp;
            }

            ShowHp(currentHp, maxHp);
        }

        private void ShowHp(int currentHp, int maxHp)
        {
            hpLabel.text = $"{currentHp} / {maxHp}";
            hpFill.fillAmount = (float)currentHp / maxHp;
        }

        private void ShowBuilding(BuildingModel building)
        {
            var definition = building.Definition;
            picture.sprite = _teams.GetColorSet(building.Faction).GetBuildingSprite(definition);
            nameLabel.text = definition.DisplayName;
            ShowHp(building.CurrentHp, definition.MaxHp);

            // Only the side being built for lists its units. Outside sandbox mode that is always the player.
            var showProduction = definition.CanProduce && building.Faction == _sandbox.ActiveFaction;
            productionSection.SetActive(showProduction);
            if (showProduction)
                ShowProducibles(definition.Producibles);
        }

        private void ShowProducibles(IReadOnlyList<UnitDefinition> units)
        {
            // Cells are kept and reused; a building never has more than a handful of units.
            while (_cells.Count < units.Count)
            {
                var cell = Instantiate(producibleCellPrefab, producibleContainer);

                // Cells are destroyed together with this panel, so the handler never needs to be removed.
                cell.Clicked += OnUnitClicked;
                _cells.Add(cell);
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var isUsed = i < units.Count;
                _cells[i].gameObject.SetActive(isUsed);
                if (isUsed)
                    _cells[i].Bind(units[i]);
            }
        }

        private void OnUnitClicked(UnitDefinition unit)
        {
            // The selection may have changed since the panel drew, so the building is looked up at click time.
            if (_buildings.TryGet(_selection.Primary, out var building))
                _production.TryProduce(building, unit, out _);
        }
    }
}
