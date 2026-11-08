using BarracksAndBolts.Core.Events;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Feedback
{
    public enum FloatingTextKind
    {
        Info,
        Warning
    }

    /// <summary>
    /// Asks for a short text that rises from a world position and fades. Any system can publish it;
    /// it doesn't need to know who draws it.
    /// </summary>
    public readonly struct FloatingTextRequestedEvent : IGameEvent
    {
        public FloatingTextRequestedEvent(string text, Vector2 position, FloatingTextKind kind = FloatingTextKind.Info)
        {
            Text = text;
            Position = position;
            Kind = kind;
        }

        public string Text { get; }
        public Vector2 Position { get; }
        public FloatingTextKind Kind { get; }
    }
}
