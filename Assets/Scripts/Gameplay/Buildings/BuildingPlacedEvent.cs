using BarracksAndBolts.Core.Events;

namespace BarracksAndBolts.Gameplay.Buildings
{
    public readonly struct BuildingPlacedEvent : IGameEvent
    {
        public BuildingPlacedEvent(BuildingModel building)
        {
            Building = building;
        }

        public BuildingModel Building { get; }
    }
}
