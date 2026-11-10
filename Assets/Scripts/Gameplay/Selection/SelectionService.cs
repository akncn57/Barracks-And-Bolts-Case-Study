using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// What the player has selected, and the selection box while it is being dragged.
    /// Holds ids, not objects, so a destroyed entity never stays referenced.
    /// </summary>
    public sealed class SelectionService : IDisposable
    {
        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly IEventBus _eventBus;
        private readonly List<GameEntityId> _selected = new();
        private readonly List<GameEntityId> _candidates = new();
        private readonly CompositeDisposable _subscriptions = new();

        public SelectionService(UnitService units, BuildingService buildings, IEventBus eventBus)
        {
            _units = units;
            _buildings = buildings;
            _eventBus = eventBus;
            eventBus.Subscribe<UnitDiedEvent>(e => Deselect(e.Unit.Id)).AddTo(_subscriptions);
            eventBus.Subscribe<UnitShelteredEvent>(e => Deselect(e.Unit.Id)).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingDestroyedEvent>(e => Deselect(e.Building.Id)).AddTo(_subscriptions);
        }

        public IReadOnlyList<GameEntityId> Selected => _selected;
        public bool HasSelection => _selected.Count > 0;

        /// <summary>The entity the information panel shows. None when nothing is selected.</summary>
        public GameEntityId Primary => HasSelection ? _selected[0] : GameEntityId.None;

        public bool HasBox { get; private set; }

        /// <summary>The selection box in world space. Only meaningful while <see cref="HasBox"/> is true.</summary>
        public Rect Box { get; private set; }

        public bool IsSelected(GameEntityId id) => _selected.Contains(id);

        /// <summary>Selects the unit or building under the click, or clears the selection if there is none.</summary>
        public void SelectAt(Vector2 worldPosition)
        {
            // Units stand in front of buildings, so they are checked first.
            var unit = _units.GetUnitAt(worldPosition);
            Select(unit.IsValid ? unit : _buildings.GetBuildingAt(worldPosition));
        }

        /// <summary>Replaces the selection with this entity. An invalid id clears it.</summary>
        public void Select(GameEntityId id)
        {
            _candidates.Clear();
            if (id.IsValid)
                _candidates.Add(id);

            ApplyCandidates();
        }

        /// <summary>Replaces the selection with these entities. An empty list clears it.</summary>
        public void Select(IReadOnlyList<GameEntityId> ids)
        {
            _candidates.Clear();
            foreach (var id in ids)
            {
                if (id.IsValid)
                    _candidates.Add(id);
            }

            ApplyCandidates();
        }

        public void Clear()
        {
            _candidates.Clear();
            ApplyCandidates();
        }

        /// <summary>Shows the box between the two world corners, in any order.</summary>
        public void UpdateBox(Vector2 cornerA, Vector2 cornerB)
        {
            Box = Rect.MinMaxRect(
                Mathf.Min(cornerA.x, cornerB.x), Mathf.Min(cornerA.y, cornerB.y),
                Mathf.Max(cornerA.x, cornerB.x), Mathf.Max(cornerA.y, cornerB.y));
            HasBox = true;
        }

        /// <summary>Selects the player's units inside the box and hides it. An empty box clears the selection.</summary>
        public void ConfirmBox()
        {
            if (!HasBox)
                return;

            HasBox = false;
            _units.CollectUnitsIn(Box, Faction.Player, _candidates);
            ApplyCandidates();
        }

        public void CancelBox()
        {
            HasBox = false;
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void Deselect(GameEntityId id)
        {
            if (_selected.Remove(id))
                _eventBus.Publish(new SelectionChangedEvent());
        }

        private void ApplyCandidates()
        {
            // The same selection again changes nothing, so listeners don't redraw for no reason.
            if (IsSameAsSelected(_candidates))
                return;

            _selected.Clear();
            _selected.AddRange(_candidates);
            _eventBus.Publish(new SelectionChangedEvent());
        }

        private bool IsSameAsSelected(List<GameEntityId> ids)
        {
            if (ids.Count != _selected.Count)
                return false;

            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] != _selected[i])
                    return false;
            }

            return true;
        }
    }
}
