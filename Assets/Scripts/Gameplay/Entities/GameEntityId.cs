using System;

namespace BarracksAndBolts.Gameplay.Entities
{
    /// <summary>
    /// Identifies a building, unit or resource. Systems keep ids instead of object references,
    /// so a destroyed target can be detected with a lookup instead of leaving a dangling reference.
    /// </summary>
    public readonly struct GameEntityId : IEquatable<GameEntityId>
    {
        public static readonly GameEntityId None = default;

        public GameEntityId(int value)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Entity ids start from 1; 0 is reserved for None.");

            Value = value;
        }

        public int Value { get; }
        public bool IsValid => Value > 0;

        public bool Equals(GameEntityId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is GameEntityId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => IsValid ? $"Entity#{Value}" : "Entity#None";

        public static bool operator ==(GameEntityId left, GameEntityId right) => left.Equals(right);
        public static bool operator !=(GameEntityId left, GameEntityId right) => !left.Equals(right);
    }
}
