using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Creates a view for every spawned unit, as a child of this object.
    /// Lives on the "Units" object of the scene, so new units never end up at the hierarchy root.
    /// </summary>
    public sealed class UnitViewSpawner : MonoBehaviour
    {
        [SerializeField] private UnitView unitPrefab;

        private readonly Dictionary<GameEntityId, UnitView> _views = new();
        private readonly CompositeDisposable _subscriptions = new();
        private TeamService _teams;
        private GameClock _clock;

        [Inject]
        private void Construct(IEventBus eventBus, TeamService teams, GameClock clock)
        {
            _teams = teams;
            _clock = clock;
            eventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnUnitSpawned(UnitSpawnedEvent spawnedEvent)
        {
            var unit = spawnedEvent.Unit;
            var frames = _teams.GetColorSet(unit.Faction).GetUnitIdleFrames(unit.Definition);

            var view = Instantiate(unitPrefab, transform);
            view.Bind(unit, frames, _clock);
            _views.Add(unit.Id, view);
        }
    }
}
