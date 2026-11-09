using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Combat;
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
        private GameClock _clock;

        [Inject]
        private void Construct(IEventBus eventBus, GridMap gridMap, TeamService teams, GameClock clock)
        {
            _gridMap = gridMap;
            _teams = teams;
            _clock = clock;
            eventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingDestroyedEvent>(OnBuildingDestroyed).AddTo(_subscriptions);
            eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingFiredEvent>(OnBuildingFired).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnBuildingPlaced(BuildingPlacedEvent placedEvent)
        {
            var building = placedEvent.Building;
            var definition = building.Definition;
            var colorSet = _teams.GetColorSet(building.Faction);

            var view = Instantiate(buildingPrefab, transform);
            view.Bind(building, colorSet.GetBuildingSprite(definition), _gridMap.AreaToWorldBottomCenter(building.Area));
            if (definition.Defender != null)
                view.ShowDefender(colorSet.GetUnitSprites(definition.Defender), definition.DefenderPosition, _clock);

            _views.Add(building.Id, view);
        }

        private void OnBuildingDestroyed(BuildingDestroyedEvent destroyedEvent)
        {
            if (_views.Remove(destroyedEvent.Building.Id, out var view))
                Destroy(view.gameObject);
        }

        private void OnEntityDamaged(EntityDamagedEvent damagedEvent)
        {
            if (_views.TryGetValue(damagedEvent.Id, out var view))
                view.ShowDamage();
        }

        private void OnBuildingFired(BuildingFiredEvent firedEvent)
        {
            if (_views.TryGetValue(firedEvent.Building.Id, out var view))
                view.PlayDefenderShot(firedEvent.AimPoint);
        }
    }
}
