using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>The unit went inside a building and left the map.</summary>
    public readonly struct UnitShelteredEvent : IGameEvent
    {
        public UnitShelteredEvent(UnitModel unit)
        {
            Unit = unit;
        }

        public UnitModel Unit { get; }
    }

    /// <summary>The unit came out of a building and is on the map again.</summary>
    public readonly struct UnitLeftShelterEvent : IGameEvent
    {
        public UnitLeftShelterEvent(UnitModel unit)
        {
            Unit = unit;
        }

        public UnitModel Unit { get; }
    }
}
