using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Buildings.Production
{
    public enum ProductionBlock
    {
        None,

        /// <summary>This building doesn't make this unit.</summary>
        NotAvailable,

        /// <summary>The building is still a site.</summary>
        UnderConstruction,

        NotEnoughResources,

        NoHousing,

        /// <summary>The map has no free cell left.</summary>
        NoRoom
    }

    /// <summary>Makes units at a building. Production has no duration. In sandbox mode it has no price or limit either.</summary>
    public sealed class UnitProductionService
    {
        private readonly UnitService _units;
        private readonly ResourceService _resources;
        private readonly PopulationService _population;

        public UnitProductionService(UnitService units, ResourceService resources, PopulationService population)
        {
            _units = units;
            _resources = resources;
            _population = population;
        }

        /// <summary>What the building makes in this mode.</summary>
        public IReadOnlyList<UnitDefinition> GetProducibles(BuildingDefinition building)
        {
            return !_resources.IsUnlimited && building.NormalModeProducibles.Count > 0 ? building.NormalModeProducibles : building.Producibles;
        }

        /// <summary>Why the unit can't be made right now. None if it can.</summary>
        public ProductionBlock GetBlock(BuildingModel building, UnitDefinition unit)
        {
            if (!CanProduce(building.Definition, unit))
                return ProductionBlock.NotAvailable;

            if (!building.IsBuilt)
                return ProductionBlock.UnderConstruction;

            if (!_resources.CanAfford(unit.Cost))
                return ProductionBlock.NotEnoughResources;

            return _population.HasRoomFor(unit) ? ProductionBlock.None : ProductionBlock.NoHousing;
        }

        /// <returns>False if the unit can't be made; <see cref="GetBlock"/> tells why.</returns>
        public bool TryProduce(BuildingModel building, UnitDefinition unit, out UnitModel produced)
        {
            produced = null;
            if (GetBlock(building, unit) != ProductionBlock.None)
                return false;

            var spawnCell = building.Area.position + building.Definition.SpawnOffset;
            if (!_units.TrySpawn(unit, spawnCell, building.Faction, out produced))
                return false;

            _resources.TrySpend(unit.Cost);
            return true;
        }

        private bool CanProduce(BuildingDefinition building, UnitDefinition unit)
        {
            foreach (var producible in GetProducibles(building))
            {
                if (producible == unit)
                    return true;
            }

            return false;
        }
    }
}
