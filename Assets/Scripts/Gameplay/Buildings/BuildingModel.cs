using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>Runtime state of one placed building.</summary>
    public sealed class BuildingModel
    {
        public BuildingModel(GameEntityId id, BuildingDefinition definition, Faction faction, RectInt area)
        {
            Id = id;
            Definition = definition;
            Faction = faction;
            Area = area;
            CurrentHp = definition.MaxHp;
        }

        public GameEntityId Id { get; }
        public BuildingDefinition Definition { get; }
        public Faction Faction { get; }

        /// <summary>Cells covered by the footprint.</summary>
        public RectInt Area { get; }

        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        }
    }
}
