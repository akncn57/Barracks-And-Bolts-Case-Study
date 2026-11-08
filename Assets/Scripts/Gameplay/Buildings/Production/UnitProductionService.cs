using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Buildings.Production
{
    /// <summary>
    /// Makes units at a building. Production has no duration and no limit in sandbox mode;
    /// costs and population checks belong here when they arrive.
    /// </summary>
    public sealed class UnitProductionService
    {
        private readonly UnitService _units;

        public UnitProductionService(UnitService units)
        {
            _units = units;
        }

        /// <returns>False if the building can't make this unit or no free cell is left.</returns>
        public bool TryProduce(BuildingModel building, UnitDefinition unit, out UnitModel produced)
        {
            produced = null;
            if (!CanProduce(building.Definition, unit))
                return false;

            var spawnCell = building.Area.position + building.Definition.SpawnOffset;
            return _units.TrySpawn(unit, spawnCell, building.Faction, out produced);
        }

        private static bool CanProduce(BuildingDefinition building, UnitDefinition unit)
        {
            foreach (var producible in building.Producibles)
            {
                if (producible == unit)
                    return true;
            }

            return false;
        }
    }
}
