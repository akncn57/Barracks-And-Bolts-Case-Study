using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>Runtime state of one unit.</summary>
    public sealed class UnitModel
    {
        public UnitModel(GameEntityId id, UnitDefinition definition, Faction faction, Vector2 position)
        {
            Id = id;
            Definition = definition;
            Faction = faction;
            Position = position;
            CurrentHp = definition.MaxHp;
        }

        public GameEntityId Id { get; }
        public UnitDefinition Definition { get; }
        public Faction Faction { get; }

        /// <summary>World position of the unit's feet. A world position, not a cell, because units move smoothly.</summary>
        public Vector2 Position { get; private set; }

        public int CurrentHp { get; private set; }
    }
}
