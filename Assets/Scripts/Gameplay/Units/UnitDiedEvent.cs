using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>A unit reached zero health and was removed from the game.</summary>
    public readonly struct UnitDiedEvent : IGameEvent
    {
        public UnitDiedEvent(UnitModel unit)
        {
            Unit = unit;
        }

        public UnitModel Unit { get; }
    }
}
