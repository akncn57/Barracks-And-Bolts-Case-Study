using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>Runtime state of one unit.</summary>
    public sealed class UnitModel
    {
        // The clickable body around the feet, in world units. Roughly the visible character.
        private const float BodyWidth = 0.9f;
        private const float BodyHeight = 1.25f;
        private const float BodyBelowFeet = 0.1f;

        // Below this sideways distance the unit keeps its facing, so walking straight up doesn't make it flicker.
        private const float MinFacingChange = 0.001f;

        // How long a unit keeps telling that it noticed an enemy.
        private const float EnemySpottedDuration = 1.5f;

        private readonly List<Vector2> _waypoints = new();
        private int _nextWaypoint;
        private float _enemySpottedLeft;

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

        /// <summary>The area that counts as the unit for clicks and box selection.</summary>
        public Rect Body => new(Position.x - BodyWidth * 0.5f, Position.y - BodyBelowFeet, BodyWidth, BodyHeight);

        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        /// <summary>The entity this unit is ordered to attack. None when it has no target.</summary>
        public GameEntityId AttackTarget { get; private set; }
        public bool HasAttackTarget => AttackTarget.IsValid;
        public float AttackCooldownLeft { get; private set; }

        /// <summary>The unit picked its target by itself a moment ago. Any change of target ends this.</summary>
        public bool HasJustSpottedEnemy => _enemySpottedLeft > 0f;

        /// <summary>The cell this unit last set out for while chasing its target.</summary>
        public Vector2Int? ChasedCell { get; private set; }
        public float RepathDelayLeft { get; private set; }

        /// <summary>What the unit is working on right now. Null when it isn't working.</summary>
        public ResourceKind? HarvestingKind { get; private set; }

        /// <summary>The unit has a job to go back to, even if it isn't working this very moment.</summary>
        public bool IsOnJob { get; private set; }

        /// <summary>Nothing to do: no job, no target and not going anywhere.</summary>
        public bool IsIdle => !IsOnJob && !IsMoving && !HasAttackTarget;

        public bool IsMoving => _nextWaypoint < _waypoints.Count;
        public bool IsFacingLeft { get; private set; }

        /// <summary>Where the unit ends up: the end of its path, or where it stands.</summary>
        public Vector2 Destination => IsMoving ? _waypoints[_waypoints.Count - 1] : Position;

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        }

        public void SetAttackTarget(GameEntityId target)
        {
            AttackTarget = target;
            ChasedCell = null;
            RepathDelayLeft = 0f;
            _enemySpottedLeft = 0f;
        }

        /// <summary>Attacks a target the unit found by itself, not one the player ordered.</summary>
        public void SpotEnemy(GameEntityId target)
        {
            SetAttackTarget(target);
            _enemySpottedLeft = EnemySpottedDuration;
        }

        public void ClearAttackTarget()
        {
            SetAttackTarget(GameEntityId.None);
        }

        public void SetHarvesting(ResourceKind? kind)
        {
            HarvestingKind = kind;
        }

        public void SetOnJob(bool isOnJob)
        {
            IsOnJob = isOnJob;
        }

        /// <summary>Puts the unit down at the point, with no walking.</summary>
        public void PlaceAt(Vector2 position)
        {
            Stop();
            Position = position;
        }

        public void TickCombatTimers(float deltaTime)
        {
            AttackCooldownLeft = Mathf.Max(AttackCooldownLeft - deltaTime, 0f);
            RepathDelayLeft = Mathf.Max(RepathDelayLeft - deltaTime, 0f);
            _enemySpottedLeft = Mathf.Max(_enemySpottedLeft - deltaTime, 0f);
        }

        public void StartAttackCooldown()
        {
            AttackCooldownLeft = Definition.AttackCooldown;
        }

        public void MarkChase(Vector2Int cell, float repathDelay)
        {
            ChasedCell = cell;
            RepathDelayLeft = repathDelay;
        }

        public void FaceTowards(Vector2 point)
        {
            var sideways = point.x - Position.x;
            if (Mathf.Abs(sideways) > MinFacingChange)
                IsFacingLeft = sideways < 0f;
        }

        /// <summary>Replaces the path. The list is copied, so the caller can reuse it.</summary>
        public void SetPath(IReadOnlyList<Vector2> waypoints)
        {
            _waypoints.Clear();
            _nextWaypoint = 0;
            for (var i = 0; i < waypoints.Count; i++)
                _waypoints.Add(waypoints[i]);
        }

        public void Stop()
        {
            _waypoints.Clear();
            _nextWaypoint = 0;
        }

        /// <summary>Walks the given distance along the path. Leftover distance carries over to the next waypoint.</summary>
        public void MoveAlongPath(float distance)
        {
            while (IsMoving && distance > 0f)
            {
                var toWaypoint = _waypoints[_nextWaypoint] - Position;
                var remaining = toWaypoint.magnitude;

                if (Mathf.Abs(toWaypoint.x) > MinFacingChange)
                    IsFacingLeft = toWaypoint.x < 0f;

                if (remaining > distance)
                {
                    Position += toWaypoint / remaining * distance;
                    return;
                }

                Position = _waypoints[_nextWaypoint];
                distance -= remaining;
                _nextWaypoint++;
            }

            if (!IsMoving)
                Stop();
        }

        /// <summary>True if a waypoint still ahead lies inside the area of cells.</summary>
        public bool IsPathThrough(RectInt area, Func<Vector2, Vector2Int> worldToCell)
        {
            for (var i = _nextWaypoint; i < _waypoints.Count; i++)
            {
                if (area.Contains(worldToCell(_waypoints[i])))
                    return true;
            }

            return false;
        }
    }
}
