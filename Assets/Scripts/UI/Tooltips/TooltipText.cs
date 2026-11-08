using System;
using System.Text;
using BarracksAndBolts.Gameplay.Data;

namespace BarracksAndBolts.UI.Tooltips
{
    /// <summary>The text shown in tooltips, built from design data. Costs join here when they exist.</summary>
    public static class TooltipText
    {
        public static string ForBuilding(BuildingDefinition building)
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

            return text.ToString();
        }

        public static string ForUnit(UnitDefinition unit)
        {
            // Invariant, so a Turkish system doesn't show 2,5 next to the rest of the English text.
            return FormattableString.Invariant($"HP: {unit.MaxHp}\nDamage: {unit.Damage}\nRange: {unit.AttackRange:0.#}\nSpeed: {unit.MoveSpeed:0.#}");
        }
    }
}
