using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>How many units the player has and how many the buildings can house. Sandbox mode has no limit.</summary>
    public sealed class PopulationService
    {
        private readonly UnitService _units;
        private readonly BuildingService _buildings;

        public PopulationService(SandboxService sandbox, UnitService units, BuildingService buildings)
        {
            IsUnlimited = sandbox.IsEnabled;
            _units = units;
            _buildings = buildings;
        }

        public bool IsUnlimited { get; }

        public int Used
        {
            get
            {
                var used = 0;
                foreach (var unit in _units.Units)
                {
                    if (unit.Faction == Faction.Player)
                        used += unit.Definition.PopulationCost;
                }

                foreach (var unit in _units.Sheltered)
                    used += unit.Definition.PopulationCost;

                return used;
            }
        }

        public int Capacity
        {
            get
            {
                var capacity = 0;
                foreach (var building in _buildings.Buildings)
                {
                    // A house still being built shelters nobody.
                    if (building.Faction == Faction.Player && building.IsBuilt)
                        capacity += building.Definition.PopulationCapacity;
                }

                return capacity;
            }
        }

        public bool HasRoomFor(UnitDefinition unit)
        {
            return IsUnlimited || Used + unit.PopulationCost <= Capacity;
        }
    }
}
