using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Teams;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>Placement mode started, or switched to something else while already active.</summary>
    public readonly struct PlacementStartedEvent : IGameEvent
    {
        public PlacementStartedEvent(BuildingDefinition definition, Faction faction, UnitDefinition unit = null)
        {
            Definition = definition;
            Faction = faction;
            Unit = unit;
        }

        /// <summary>Null while a unit is placed.</summary>
        public BuildingDefinition Definition { get; }

        /// <summary>Null while a building is placed.</summary>
        public UnitDefinition Unit { get; }
        public Faction Faction { get; }
    }
}
