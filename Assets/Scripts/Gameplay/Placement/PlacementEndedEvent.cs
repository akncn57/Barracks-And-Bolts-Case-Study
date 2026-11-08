using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>Placement mode closed, either after placing a building or by cancelling.</summary>
    public readonly struct PlacementEndedEvent : IGameEvent
    {
    }
}
