using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>The selection changed. Read the new state from <see cref="SelectionService"/>.</summary>
    public readonly struct SelectionChangedEvent : IGameEvent
    {
    }
}
