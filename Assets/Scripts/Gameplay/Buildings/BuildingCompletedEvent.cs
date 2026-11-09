using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>A building site was finished. From now on it works like any other building.</summary>
    public readonly struct BuildingCompletedEvent : IGameEvent
    {
        public BuildingCompletedEvent(BuildingModel building)
        {
            Building = building;
        }

        public BuildingModel Building { get; }
    }
}
