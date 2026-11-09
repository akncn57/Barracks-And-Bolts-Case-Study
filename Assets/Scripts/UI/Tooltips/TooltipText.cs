using System;
using System.Collections.Generic;
using System.Text;
using BarracksAndBolts.Gameplay.Data;

namespace BarracksAndBolts.UI.Tooltips
{
    /// <summary>The text shown in tooltips, built from design data.</summary>
    public static class TooltipText
    {
        /// <param name="showCost">False in sandbox mode, where everything is free.</param>
        public static string ForBuilding(BuildingDefinition building, bool showCost = false)
        {
            var text = new StringBuilder();
            text.Append("Size: ").Append(building.Size.x).Append('x').Append(building.Size.y).Append('\n');
            text.Append("HP: ").Append(building.MaxHp);

            if (building.CanProduce)
            {
                text.Append("\nProduces: ");
                for (var i = 0; i < building.Producibles.Count; i++)
                {
                    if (i > 0)
                        text.Append(", ");

                    text.Append(building.Producibles[i].DisplayName);
                }
            }

            if (showCost)
                AppendCost(text, building.Cost);

            return text.ToString();
        }

        /// <param name="showCost">False in sandbox mode, where everything is free.</param>
        public static string ForUnit(UnitDefinition unit, bool showCost = false)
        {
            // Invariant, so a Turkish system doesn't show 2,5 next to the rest of the English text.
            var text = new StringBuilder(FormattableString.Invariant($"HP: {unit.MaxHp}\nDamage: {unit.Damage}\nRange: {unit.AttackRange:0.#}\nSpeed: {unit.MoveSpeed:0.#}"));
            if (showCost)
                AppendCost(text, unit.Cost);

            return text.ToString();
        }

        private static void AppendCost(StringBuilder text, IReadOnlyList<ResourceCost> cost)
        {
            if (cost.Count == 0)
                return;

            text.Append("\nCost: ");
            for (var i = 0; i < cost.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");

                text.Append(cost[i].Amount).Append(' ').Append(cost[i].Kind);
            }
        }
    }
}
