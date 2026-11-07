using System;

namespace BarracksAndBolts.Core.Session
{
    /// <summary>
    /// Choices made in the new game screen. Lives in the project scope to carry them from the menu into gameplay.
    /// </summary>
    public sealed class GameSessionSettings
    {
        public GameMode Mode { get; set; } = GameMode.Sandbox;
        public TeamColor PlayerColor { get; set; } = TeamColor.Red;
        public Difficulty Difficulty { get; set; } = Difficulty.Normal;
        public int Seed { get; set; } = Environment.TickCount;
    }
}
