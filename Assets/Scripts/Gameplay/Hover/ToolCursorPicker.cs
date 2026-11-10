using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Teams;

namespace BarracksAndBolts.Gameplay.Hover
{
    /// <summary>Which tool the cursor shows: the one a villager would use on the hovered thing.</summary>
    public static class ToolCursorPicker
    {
        /// <param name="node">The hovered resource, or null.</param>
        /// <param name="building">The hovered building, or null.</param>
        public static ToolCursor Pick(ResourceNodeModel node, BuildingModel building)
        {
            if (node != null)
            {
                // Trees and sheep take the axe, stone and gold the pickaxe; the same split as the villager's work frames.
                var kind = node.Definition.Kind;
                return kind == ResourceKind.Stone || kind == ResourceKind.Gold ? ToolCursor.Pickaxe : ToolCursor.Axe;
            }

            if (building == null || building.Faction != Faction.Player || building.IsDead)
                return ToolCursor.None;

            if (!building.IsBuilt)
                return ToolCursor.Hammer;

            // A finished farm is worked like sheep and trees.
            if (building.Definition.FarmYield > 0)
                return ToolCursor.Axe;

            return ToolCursor.None;
        }
    }
}
