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

    /// <summary>A unit started an attack. AimPoint is where the blow lands, in world space.</summary>
    public readonly struct UnitAttackedEvent : IGameEvent
    {
        public UnitAttackedEvent(UnitModel attacker, Vector2 aimPoint, float releaseDelay = 0f, float flightTime = 0f)
        {
            Attacker = attacker;
            AimPoint = aimPoint;
            ReleaseDelay = releaseDelay;
            FlightTime = flightTime;
        }

        public UnitModel Attacker { get; }
        public Vector2 AimPoint { get; }

        /// <summary>Game seconds until the blow is struck or the arrow leaves.</summary>
        public float ReleaseDelay { get; }

        /// <summary>Game seconds the arrow flies. 0 for blows.</summary>
        public float FlightTime { get; }
    }

    /// <summary>A building fired. Muzzle is where the arrow leaves, AimPoint where it lands, both in world space.</summary>
    public readonly struct BuildingFiredEvent : IGameEvent
    {
        public BuildingFiredEvent(BuildingModel building, Vector2 muzzle, Vector2 aimPoint, float releaseDelay = 0f, float flightTime = 0f)
        {
            Building = building;
            Muzzle = muzzle;
            AimPoint = aimPoint;
            ReleaseDelay = releaseDelay;
            FlightTime = flightTime;
        }

        public BuildingModel Building { get; }
        public Vector2 Muzzle { get; }
        public Vector2 AimPoint { get; }

        /// <summary>Game seconds until the defender lets the arrow go.</summary>
        public float ReleaseDelay { get; }

        /// <summary>Game seconds the arrow flies.</summary>
        public float FlightTime { get; }
    }
}
