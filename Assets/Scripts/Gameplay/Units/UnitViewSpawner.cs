using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Creates a view for every spawned unit, as a child of this object, and turns their selection rings on and off.
    /// Lives on the "Units" object of the scene, so new units never end up at the hierarchy root.
    /// </summary>
    public sealed class UnitViewSpawner : MonoBehaviour
    {
        [SerializeField] private UnitView unitPrefab;

        private readonly Dictionary<GameEntityId, UnitView> _views = new();
        private readonly List<UnitView> _selectedViews = new();
        private readonly CompositeDisposable _subscriptions = new();
        private TeamService _teams;
        private GameClock _clock;
        private SelectionService _selection;

        [Inject]
        private void Construct(IEventBus eventBus, TeamService teams, GameClock clock, SelectionService selection)
        {
            _teams = teams;
            _clock = clock;
            _selection = selection;
            eventBus.Subscribe<UnitSpawnedEvent>(OnUnitSpawned).AddTo(_subscriptions);
            eventBus.Subscribe<UnitDiedEvent>(OnUnitDied).AddTo(_subscriptions);
            eventBus.Subscribe<UnitAttackedEvent>(OnUnitAttacked).AddTo(_subscriptions);
            eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged).AddTo(_subscriptions);
            eventBus.Subscribe<SelectionChangedEvent>(_ => RefreshSelectionMarkers()).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnUnitSpawned(UnitSpawnedEvent spawnedEvent)
        {
            var unit = spawnedEvent.Unit;
            var sprites = _teams.GetColorSet(unit.Faction).GetUnitSprites(unit.Definition);

            var view = Instantiate(unitPrefab, unit.Position, Quaternion.identity, transform);
            view.Bind(unit, sprites, _clock);
            _views.Add(unit.Id, view);
        }

        private void OnUnitDied(UnitDiedEvent diedEvent)
        {
            if (!_views.Remove(diedEvent.Unit.Id, out var view))
                return;

            _selectedViews.Remove(view);
            Destroy(view.gameObject);
        }

        private void OnUnitAttacked(UnitAttackedEvent attackedEvent)
        {
            if (_views.TryGetValue(attackedEvent.Attacker.Id, out var view))
                view.PlayAttack();
        }

        private void OnEntityDamaged(EntityDamagedEvent damagedEvent)
        {
            if (_views.TryGetValue(damagedEvent.Id, out var view))
                view.ShowDamage();
        }

        private void RefreshSelectionMarkers()
        {
            // Only the views that were on are touched, not every unit on the map.
            foreach (var view in _selectedViews)
                view.SetSelected(false);

            _selectedViews.Clear();
            foreach (var id in _selection.Selected)
            {
                if (!_views.TryGetValue(id, out var view))
                    continue;

                view.SetSelected(true);
                _selectedViews.Add(view);
            }
        }
    }
}
