using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Buildings.Production;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>
    /// Shows the selected building: picture, name, HP and, for the player's production buildings, the units it can make.
    /// Hidden while nothing is selected. Redraws only when the selection changes.
    /// </summary>
    public sealed class InformationPanelView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible panel. Hidden while nothing is selected; this object stays active.")]
        [SerializeField] private GameObject content;

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
        private IEventBus _eventBus;

        [Inject]
        private void Construct(SelectionService selection, BuildingService buildings, TeamService teams,
            UnitProductionService production, IEventBus eventBus)
        {
            _selection = selection;
            _buildings = buildings;
            _teams = teams;
            _production = production;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _eventBus.Subscribe<SelectionChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void Refresh()
        {
            // Only buildings for now; units get their own branch when they exist.
            if (!_buildings.TryGet(_selection.Primary, out var building))
            {
                content.SetActive(false);
                return;
            }

            content.SetActive(true);
            ShowBuilding(building);
        }

        private void ShowBuilding(BuildingModel building)
        {
            var definition = building.Definition;
            picture.sprite = _teams.GetColorSet(building.Faction).GetBuildingSprite(definition);
            nameLabel.text = definition.DisplayName;
            hpLabel.text = $"{building.CurrentHp} / {definition.MaxHp}";
            hpFill.fillAmount = (float)building.CurrentHp / definition.MaxHp;

            // The enemy's production can't be controlled, so only the player's buildings list their units.
            var showProduction = definition.CanProduce && building.Faction == Faction.Player;
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
