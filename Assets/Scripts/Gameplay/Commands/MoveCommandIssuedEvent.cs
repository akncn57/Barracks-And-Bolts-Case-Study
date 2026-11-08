using BarracksAndBolts.Core.Events;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>Units were ordered to move. Position is the clicked world point, for the feedback marker.</summary>
    public readonly struct MoveCommandIssuedEvent : IGameEvent
    {
        public MoveCommandIssuedEvent(Vector2 position)
        {
            Position = position;
        }

        public Vector2 Position { get; }
    }

    /// <summary>Units were ordered to attack. Position is the clicked world point, for the feedback marker.</summary>
    public readonly struct AttackCommandIssuedEvent : IGameEvent
    {
        public AttackCommandIssuedEvent(Vector2 position)
        {
            Position = position;
        }

        public Vector2 Position { get; }
    }
}
