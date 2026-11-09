using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
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

    /// <summary>A building fired. Muzzle is where the arrow leaves, AimPoint where it lands, both in world space.</summary>
    public readonly struct BuildingFiredEvent : IGameEvent
    {
        public BuildingFiredEvent(BuildingModel building, Vector2 muzzle, Vector2 aimPoint)
        {
            Building = building;
            Muzzle = muzzle;
            AimPoint = aimPoint;
        }

        public BuildingModel Building { get; }
        public Vector2 Muzzle { get; }
        public Vector2 AimPoint { get; }
    }
}
