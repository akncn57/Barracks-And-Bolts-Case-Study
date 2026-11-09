using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    public readonly struct ResourceNodeRemovedEvent : IGameEvent
    {
        public ResourceNodeRemovedEvent(ResourceNodeModel node)
        {
            Node = node;
        }

        public ResourceNodeModel Node { get; }
    }
}
