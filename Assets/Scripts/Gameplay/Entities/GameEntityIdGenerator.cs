namespace BarracksAndBolts.Gameplay.Entities
{
    /// <summary>
    /// Hands out ids for the whole session. Buildings, units and resources share it, so their ids never collide.
    /// </summary>
    public sealed class GameEntityIdGenerator
    {
        private int _lastValue;

        public GameEntityId Next()
        {
            _lastValue++;
            return new GameEntityId(_lastValue);
        }
    }
}
