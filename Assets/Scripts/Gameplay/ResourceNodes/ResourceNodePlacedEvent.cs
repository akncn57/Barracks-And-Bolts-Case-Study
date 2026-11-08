using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    public readonly struct ResourceNodePlacedEvent : IGameEvent
    {
        public ResourceNodePlacedEvent(ResourceNodeModel node)
        {
            Node = node;
        }

        public ResourceNodeModel Node { get; }
    }
}
