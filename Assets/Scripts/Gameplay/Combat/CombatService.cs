using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// The rules of a fight: what counts as a target, how far away it is, and what damage does.
    /// A blow lands a moment after the swing starts and an arrow when it arrives, so hits can be scheduled.
    /// Dead entities are removed in one go at the end of the tick, so nothing is deleted while a system loops over it.
    /// </summary>
    public sealed class CombatService
    {
        // A unit's aim point is its chest, not its feet.
        private static readonly Vector2 UnitAimOffset = new(0f, 0.5f);

        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;
        private readonly List<GameEntityId> _pendingDeaths = new();
        private readonly List<ScheduledHit> _scheduledHits = new();

        private struct ScheduledHit
        {
            public GameEntityId Target;
            public GameEntityId Attacker;
            public int Damage;

            // Until the blow is struck or the arrow leaves, the hit dies with the attacker.
            public float ReleaseLeft;
            public float TimeLeft;
        }

        public CombatService(UnitService units, BuildingService buildings, GridMap gridMap, IEventBus eventBus)
        {
            _units = units;
            _buildings = buildings;
            _gridMap = gridMap;
            _eventBus = eventBus;
        }

        /// <returns>False if the entity doesn't exist, or is already dead and waiting to be removed.</returns>
        public bool TryGetTarget(GameEntityId id, Vector2 attackerPosition, out CombatTarget target)
        {
            if (_units.TryGet(id, out var unit) && !unit.IsDead)
            {
                var distance = Vector2.Distance(attackerPosition, unit.Position);
                var chaseCell = _gridMap.WorldToCell(unit.Position);
                if (unit.IsInField && _buildings.TryGet(_buildings.GetBuildingAt(unit.Position), out var field))
                    ReachOverFence(field.Area, unit.Position, ref distance, ref chaseCell);

                target = new CombatTarget(unit.Faction, distance, unit.Position + UnitAimOffset, chaseCell);
                return true;
            }

            if (_buildings.TryGet(id, out var building) && !building.IsDead)
            {
                // Measured to the nearest edge, so a big building can be hit from any side.
                var area = building.Area;
                var min = _gridMap.Origin + area.min;
                var max = _gridMap.Origin + area.max;
                var closest = new Vector2(
                    Mathf.Clamp(attackerPosition.x, min.x, max.x),
                    Mathf.Clamp(attackerPosition.y, min.y, max.y));

                var attackerCell = _gridMap.WorldToCell(attackerPosition);
                var nearestCell = new Vector2Int(
                    Mathf.Clamp(attackerCell.x, area.xMin, area.xMax - 1),
                    Mathf.Clamp(attackerCell.y, area.yMin, area.yMax - 1));

                target = new CombatTarget(building.Faction, Vector2.Distance(attackerPosition, closest), closest, nearestCell);
                return true;
            }

            target = default;
            return false;
        }

        /// <summary>
        /// A villager working inside a farm is hit over the fence: the way in from the nearest fence doesn't count,
        /// and the attacker walks to the cell outside that fence.
        /// </summary>
        private void ReachOverFence(RectInt field, Vector2 position, ref float distance, ref Vector2Int chaseCell)
        {
            var min = _gridMap.Origin + field.min;
            var max = _gridMap.Origin + field.max;
            var toLeft = position.x - min.x;
            var toRight = max.x - position.x;
            var toBottom = position.y - min.y;
            var toTop = max.y - position.y;
            var depth = Mathf.Min(Mathf.Min(toLeft, toRight), Mathf.Min(toBottom, toTop));
            distance = Mathf.Max(0f, distance - depth);

            if (depth == toBottom)
                chaseCell.y = field.yMin - 1;
            else if (depth == toLeft)
                chaseCell.x = field.xMin - 1;
            else if (depth == toRight)
                chaseCell.x = field.xMax;
            else
                chaseCell.y = field.yMax;
        }

        /// <summary>The enemy of the faction at the point: a unit first, then a building. None if there is no enemy there.</summary>
        public GameEntityId GetHostileAt(Vector2 worldPosition, Faction faction)
        {
            var unitId = _units.GetUnitAt(worldPosition);
            if (_units.TryGet(unitId, out var unit) && unit.Faction != faction)
                return unitId;

            var buildingId = _buildings.GetBuildingAt(worldPosition);
            return _buildings.TryGet(buildingId, out var building) && building.Faction != faction ? buildingId : GameEntityId.None;
        }

        /// <summary>The closest enemy of the faction within the range, unit or building. None if nothing is that close.</summary>
        public GameEntityId FindNearestHostile(Vector2 from, Faction faction, float range)
        {
            var nearest = FindNearestHostileUnit(from, faction, range, out var nearestDistance);
            foreach (var building in _buildings.Buildings)
            {
                if (building.Faction != faction && TryGetTarget(building.Id, from, out var target) && target.Distance <= nearestDistance)
                {
                    nearestDistance = target.Distance;
                    nearest = building.Id;
                }
            }

            return nearest;
        }

        /// <summary>The closest enemy unit of the faction within the range. None if nothing is that close.</summary>
        public GameEntityId FindNearestHostileUnit(Vector2 from, Faction faction, float range)
        {
            return FindNearestHostileUnit(from, faction, range, out _);
        }

        // Without a unit in range the distance stays at the range, so a building can still be closer.
        private GameEntityId FindNearestHostileUnit(Vector2 from, Faction faction, float range, out float nearestDistance)
        {
            var nearest = GameEntityId.None;
            nearestDistance = range;
            foreach (var unit in _units.Units)
            {
                if (unit.Faction == faction || unit.IsDead)
                    continue;

                var distance = Vector2.Distance(from, unit.Position);
                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = unit.Id;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Lowers the target's health. A unit that has nothing else to do turns on its attacker.
        /// Reaching zero queues the removal; hitting something already dead does nothing.
        /// </summary>
        public void ApplyDamage(GameEntityId targetId, int amount, GameEntityId attackerId)
        {
            if (_units.TryGet(targetId, out var unit))
            {
                if (unit.IsDead)
                    return;

                unit.TakeDamage(amount);
                if (unit.IsDead)
                    _pendingDeaths.Add(targetId);
                else if (attackerId.IsValid && !unit.HasAttackTarget && !unit.IsMoving)
                    unit.SpotEnemy(attackerId);
            }
            else if (_buildings.TryGet(targetId, out var building))
            {
                if (building.IsDead)
                    return;

                building.TakeDamage(amount);
                if (building.IsDead)
                    _pendingDeaths.Add(targetId);
            }
            else
            {
                return;
            }

            _eventBus.Publish(new EntityDamagedEvent(targetId));
        }

        /// <summary>Damage that lands later: after the swing, or when an arrow arrives. With no delay it lands at once.</summary>
        /// <param name="releaseDelay">Time until the blow is struck or the arrow leaves. An attacker gone by then cancels it.</param>
        /// <param name="totalDelay">Time until the damage lands; the flight comes after the release.</param>
        public void ScheduleHit(GameEntityId targetId, int damage, GameEntityId attackerId, float releaseDelay, float totalDelay)
        {
            if (totalDelay <= 0f)
            {
                ApplyDamage(targetId, damage, attackerId);
                return;
            }

            _scheduledHits.Add(new ScheduledHit
            {
                Target = targetId,
                Attacker = attackerId,
                Damage = damage,
                ReleaseLeft = releaseDelay,
                TimeLeft = totalDelay
            });
        }

        /// <summary>Moves scheduled hits on and lands the ones that are due.</summary>
        public void TickScheduledHits(float deltaTime)
        {
            // Backwards, so landed hits can be removed on the way.
            for (var i = _scheduledHits.Count - 1; i >= 0; i--)
            {
                var hit = _scheduledHits[i];
                if (hit.ReleaseLeft > 0f)
                {
                    hit.ReleaseLeft -= deltaTime;
                    if (!IsAttackerAlive(hit.Attacker))
                    {
                        _scheduledHits.RemoveAt(i);
                        continue;
                    }
                }

                hit.TimeLeft -= deltaTime;
                if (hit.TimeLeft > 0f)
                {
                    _scheduledHits[i] = hit;
                    continue;
                }

                _scheduledHits.RemoveAt(i);
                ApplyDamage(hit.Target, hit.Damage, hit.Attacker);
            }
        }

        // Buildings shoot too; a tower that falls still lets its last arrow land.
        private bool IsAttackerAlive(GameEntityId attackerId)
        {
            if (_units.TryGet(attackerId, out var unit))
                return !unit.IsDead;

            return _buildings.TryGet(attackerId, out var building) && !building.IsDead;
        }

        /// <summary>Removes everything that died since the last call.</summary>
        public void RemoveDead()
        {
            foreach (var id in _pendingDeaths)
            {
                if (!_units.Remove(id))
                    _buildings.Remove(id);
            }

            _pendingDeaths.Clear();
        }
    }

    public readonly struct CombatTarget
    {
        public CombatTarget(Faction faction, float distance, Vector2 aimPoint, Vector2Int chaseCell)
        {
            Faction = faction;
            Distance = distance;
            AimPoint = aimPoint;
            ChaseCell = chaseCell;
        }

        public Faction Faction { get; }

        /// <summary>From the attacker's feet to the target: its feet, or the nearest edge of a building.</summary>
        public float Distance { get; }

        public Vector2 AimPoint { get; }

        /// <summary>The cell to walk towards when out of range.</summary>
        public Vector2Int ChaseCell { get; }
    }
}
