using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.UI.Scroll;
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
        private TeamColorDefinition _playerColors;

        [Inject]
        private void Construct(GameDatabase database, TeamService teams, PlacementService placement)
        {
            _database = database;
            _teams = teams;
            _placement = placement;
        }

        private void Start()
        {
            _playerColors = _teams.GetColorSet(Faction.Player);
            scrollView.Initialize<ProductionMenuCell>(_database.Buildings.Count, OnCellCreated, BindCell);
        }

        private void OnCellCreated(ProductionMenuCell cell)
        {
            // Cells are destroyed together with this menu, so the handler never needs to be removed.
            cell.Clicked += OnBuildingClicked;
        }

        private void BindCell(ProductionMenuCell cell, int index)
        {
            var building = _database.Buildings[index];
            cell.Bind(building, _playerColors.GetBuildingSprite(building));
        }

        private void OnBuildingClicked(BuildingDefinition building)
        {
            _placement.Begin(building, Faction.Player);
        }
    }
}
