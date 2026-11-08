using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>A building reached zero health and was removed from the game.</summary>
    public readonly struct BuildingDestroyedEvent : IGameEvent
    {
        public BuildingDestroyedEvent(BuildingModel building)
        {
            Building = building;
        }

        public BuildingModel Building { get; }
    }
}
