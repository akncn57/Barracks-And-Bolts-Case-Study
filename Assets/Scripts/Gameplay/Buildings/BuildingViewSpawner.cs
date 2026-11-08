using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// Creates a view for every placed building, as a child of this object.
    /// Lives on the "Buildings" object of the scene, so new buildings never end up at the hierarchy root.
    /// </summary>
    public sealed class BuildingViewSpawner : MonoBehaviour
    {
        [SerializeField] private BuildingView buildingPrefab;

        private readonly Dictionary<GameEntityId, BuildingView> _views = new();
        private readonly CompositeDisposable _subscriptions = new();
        private GridMap _gridMap;
        private TeamService _teams;

        [Inject]
        private void Construct(IEventBus eventBus, GridMap gridMap, TeamService teams)
        {
            _gridMap = gridMap;
            _teams = teams;
            eventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnBuildingPlaced(BuildingPlacedEvent placedEvent)
        {
            var building = placedEvent.Building;
            var sprite = _teams.GetColorSet(building.Faction).GetBuildingSprite(building.Definition);

            var view = Instantiate(buildingPrefab, transform);
            view.Bind(building, sprite, _gridMap.AreaToWorldBottomCenter(building.Area));
            _views.Add(building.Id, view);
        }
    }
}
