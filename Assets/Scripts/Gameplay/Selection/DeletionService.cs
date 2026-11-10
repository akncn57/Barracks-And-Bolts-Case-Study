using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// Takes selected things off the map at once, with no death or refund. Sandbox mode deletes anything, also a resource
    /// under the mouse. A normal game deletes only the player's own units and buildings, and never the Castle.
    /// </summary>
    public sealed class DeletionService
    {
        private readonly SelectionService _selection;
        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly ResourceNodeService _nodes;
        private readonly HoverService _hover;
        private readonly SandboxService _sandbox;
        private readonly NormalModeSettings _normalMode;

        // Deleting changes the selection, so the ids are copied first.
        private readonly List<GameEntityId> _toDelete = new();

        public DeletionService(SelectionService selection, UnitService units, BuildingService buildings, ResourceNodeService nodes,
            HoverService hover, SandboxService sandbox, NormalModeSettings normalMode)
        {
            _selection = selection;
            _units = units;
            _buildings = buildings;
            _nodes = nodes;
            _hover = hover;
            _sandbox = sandbox;
            _normalMode = normalMode;
        }

        public bool CanDelete(GameEntityId id)
        {
            if (_units.TryGet(id, out var unit))
                return _sandbox.IsEnabled || unit.Faction == Faction.Player;

            if (_buildings.TryGet(id, out var building))
                return _sandbox.IsEnabled || (building.Faction == Faction.Player && building.Definition != _normalMode.Castle);

            return false;
        }

        /// <summary>Deletes what is selected and allowed. In sandbox mode, with nothing selected, the resource under the mouse.</summary>
        /// <returns>How many things were deleted. 0 with a selection means none of it may be deleted.</returns>
        public int DeleteSelectedOrHovered()
        {
            if (_selection.Selected.Count == 0)
                return TryDeleteHoveredNode() ? 1 : 0;

            _toDelete.Clear();
            _toDelete.AddRange(_selection.Selected);

            var deleted = 0;
            foreach (var id in _toDelete)
            {
                if (!CanDelete(id))
                    continue;

                // Removing publishes the same events as a death, so the selection, panels and targets clean up as usual.
                if (_units.Remove(id) || _buildings.Remove(id))
                    deleted++;
            }

            return deleted;
        }

        private bool TryDeleteHoveredNode()
        {
            var node = _hover.HoveredNode;
            if (!_sandbox.IsEnabled || node == null)
                return false;

            // Emptied first, so villagers working on it move on as if it ran out.
            node.Take(node.Remaining);
            _nodes.Remove(node);
            return true;
        }
    }
}
