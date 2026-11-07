using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Core.Events
{
    /// <summary>
    /// Typed publish/subscribe hub. Lets systems react to each other without direct references.
    /// </summary>
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> _channels = new();

        public void Publish<TEvent>(in TEvent gameEvent) where TEvent : struct, IGameEvent
        {
            if (_channels.TryGetValue(typeof(TEvent), out object channel))
                ((Channel<TEvent>)channel).Publish(gameEvent);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_channels.TryGetValue(typeof(TEvent), out object channel))
            {
                channel = new Channel<TEvent>();
                _channels.Add(typeof(TEvent), channel);
            }

            return ((Channel<TEvent>)channel).Add(handler);
        }

        private sealed class Channel<TEvent> where TEvent : struct, IGameEvent
        {
            private readonly List<Subscription> _subscriptions = new();
            private Subscription[] _snapshot = Array.Empty<Subscription>();
            private bool _isSnapshotDirty;

            public IDisposable Add(Action<TEvent> handler)
            {
                var subscription = new Subscription(this, handler);
                _subscriptions.Add(subscription);
                _isSnapshotDirty = true;
                return subscription;
            }

            public void Publish(in TEvent gameEvent)
            {
                // Iterate a snapshot so handlers can subscribe or unsubscribe while we are publishing.
                // The snapshot is rebuilt only when the list changed, so normal publishing doesn't allocate.
                if (_isSnapshotDirty)
                {
                    _snapshot = _subscriptions.ToArray();
                    _isSnapshotDirty = false;
                }

                foreach (Subscription subscription in _snapshot)
                {
                    // A handler earlier in this loop may have disposed this one.
                    if (!subscription.IsActive)
                        continue;

                    // One broken handler must not stop the others from receiving the event.
                    try
                    {
                        subscription.Handler(gameEvent);
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }

            private void Remove(Subscription subscription)
            {
                if (_subscriptions.Remove(subscription))
                    _isSnapshotDirty = true;
            }

            private sealed class Subscription : IDisposable
            {
                private readonly Channel<TEvent> _owner;

                public Subscription(Channel<TEvent> owner, Action<TEvent> handler)
                {
                    _owner = owner;
                    Handler = handler;
                }

                public Action<TEvent> Handler { get; }
                public bool IsActive { get; private set; } = true;

                public void Dispose()
                {
                    if (!IsActive)
                        return;

                    IsActive = false;
                    _owner.Remove(this);
                }
            }
        }
    }
}
