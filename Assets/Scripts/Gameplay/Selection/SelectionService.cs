using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// What the player has selected. Holds ids, not objects, so a destroyed entity never stays referenced.
    /// Kept as a list so box selection of units can be added without changing the readers.
    /// </summary>
    public sealed class SelectionService
    {
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;
        private readonly List<GameEntityId> _selected = new();

        public SelectionService(GridMap gridMap, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _eventBus = eventBus;
        }

        public IReadOnlyList<GameEntityId> Selected => _selected;
        public bool HasSelection => _selected.Count > 0;

        /// <summary>The entity the information panel shows. None when nothing is selected.</summary>
        public GameEntityId Primary => HasSelection ? _selected[0] : GameEntityId.None;

        public bool IsSelected(GameEntityId id) => _selected.Contains(id);

        /// <summary>Selects whatever stands on the clicked cell, or clears the selection on an empty cell.</summary>
        public void SelectAt(Vector2 worldPosition)
        {
            Select(_gridMap.GetOccupant(_gridMap.WorldToCell(worldPosition)));
        }

        /// <summary>Replaces the selection with this entity. An invalid id clears it.</summary>
        public void Select(GameEntityId id)
        {
            if (!id.IsValid)
            {
                Clear();
                return;
            }

            // Clicking the selected entity again changes nothing, so listeners don't redraw for no reason.
            if (_selected.Count == 1 && _selected[0] == id)
                return;

            _selected.Clear();
            _selected.Add(id);
            _eventBus.Publish(new SelectionChangedEvent());
        }

        public void Clear()
        {
            if (_selected.Count == 0)
                return;

            _selected.Clear();
            _eventBus.Publish(new SelectionChangedEvent());
        }
    }
}
