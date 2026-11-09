using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Sandbox;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>What the player owns. In sandbox mode everything is free, so prices never stop anything.</summary>
    public sealed class ResourceService
    {
        private readonly int[] _amounts = new int[Enum.GetValues(typeof(ResourceKind)).Length];
        private readonly IEventBus _eventBus;

        public ResourceService(SandboxService sandbox, IEventBus eventBus)
        {
            IsUnlimited = sandbox.IsEnabled;
            _eventBus = eventBus;
        }

        public bool IsUnlimited { get; }

        public int Get(ResourceKind kind)
        {
            return _amounts[(int)kind];
        }

        public void Add(ResourceKind kind, int amount)
        {
            if (amount <= 0)
                return;

            _amounts[(int)kind] += amount;
            _eventBus.Publish(new ResourcesChangedEvent());
        }

        public bool CanAfford(IReadOnlyList<ResourceCost> cost)
        {
            if (IsUnlimited)
                return true;

            foreach (var part in cost)
            {
                if (Get(part.Kind) < part.Amount)
                    return false;
            }

            return true;
        }

        /// <returns>False if the player can't afford it; nothing is taken then.</returns>
        public bool TrySpend(IReadOnlyList<ResourceCost> cost)
        {
            if (!CanAfford(cost))
                return false;

            if (IsUnlimited || cost.Count == 0)
                return true;

            foreach (var part in cost)
                _amounts[(int)part.Kind] -= part.Amount;

            _eventBus.Publish(new ResourcesChangedEvent());
            return true;
        }
    }

    /// <summary>An amount changed. Read the new ones from <see cref="ResourceService"/>.</summary>
    public readonly struct ResourcesChangedEvent : IGameEvent
    {
    }
}
