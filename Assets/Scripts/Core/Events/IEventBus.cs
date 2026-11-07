using System;

namespace BarracksAndBolts.Core.Events
{
    public interface IEventBus
    {
        void Publish<TEvent>(in TEvent gameEvent) where TEvent : struct, IGameEvent;

        /// <returns>Dispose it to unsubscribe.</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;
    }
}
