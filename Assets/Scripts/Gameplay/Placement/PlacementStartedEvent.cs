using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Teams;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>Placement mode started, or switched to another building while already active.</summary>
    public readonly struct PlacementStartedEvent : IGameEvent
    {
        public PlacementStartedEvent(BuildingDefinition definition, Faction faction)
        {
            Definition = definition;
            Faction = faction;
        }

        public BuildingDefinition Definition { get; }
        public Faction Faction { get; }
    }
}
