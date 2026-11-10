using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>The player's villagers with nothing to do (the "Zz" ones), one after another for the idle villager key.</summary>
    public sealed class IdleVillagerFinder
    {
        private readonly UnitService _units;
        private GameEntityId _last = GameEntityId.None;

        public IdleVillagerFinder(UnitService units)
        {
            _units = units;
        }

        public int CountIdle()
        {
            var count = 0;
            foreach (var unit in _units.Units)
            {
                if (IsIdleVillager(unit))
                    count++;
            }

            return count;
        }

        /// <summary>The idle villager after the one given last time, by id; after the last one it starts again.</summary>
        /// <returns>Null when no villager is idle.</returns>
        public UnitModel FindNext()
        {
            UnitModel first = null;
            UnitModel next = null;
            foreach (var unit in _units.Units)
            {
                if (!IsIdleVillager(unit))
                    continue;

                if (first == null || unit.Id.Value < first.Id.Value)
                    first = unit;

                if (unit.Id.Value > _last.Value && (next == null || unit.Id.Value < next.Id.Value))
                    next = unit;
            }

            var found = next ?? first;
            if (found != null)
                _last = found.Id;

            return found;
        }

        private static bool IsIdleVillager(UnitModel unit)
        {
            return unit.Faction == Faction.Player && unit.Definition.CanGather && !unit.IsDead && unit.IsIdle;
        }
    }
}
