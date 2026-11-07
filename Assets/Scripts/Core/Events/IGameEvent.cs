namespace BarracksAndBolts.Core.Events
{
    /// <summary>
    /// Marker for messages sent through <see cref="IEventBus"/>.
    /// Events describe something that already happened, e.g. UnitDiedEvent.
    /// </summary>
    public interface IGameEvent
    {
    }
}
