using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>A unit or building lost health.</summary>
    public readonly struct EntityDamagedEvent : IGameEvent
    {
        public EntityDamagedEvent(GameEntityId id)
        {
            Id = id;
        }

        public GameEntityId Id { get; }
    }

    /// <summary>A unit struck or fired. AimPoint is where the blow lands, in world space.</summary>
    public readonly struct UnitAttackedEvent : IGameEvent
    {
        public UnitAttackedEvent(UnitModel attacker, Vector2 aimPoint)
        {
            Attacker = attacker;
            AimPoint = aimPoint;
        }

        public UnitModel Attacker { get; }
        public Vector2 AimPoint { get; }
    }
}
