using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>A new unit entered the map.</summary>
    public readonly struct UnitSpawnedEvent : IGameEvent
    {
        public UnitSpawnedEvent(UnitModel unit)
        {
            Unit = unit;
        }

        public UnitModel Unit { get; }
    }
}
