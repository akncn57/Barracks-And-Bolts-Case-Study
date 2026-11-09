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
                target = new CombatTarget(
                    unit.Faction,
                    Vector2.Distance(attackerPosition, unit.Position),
                    unit.Position + UnitAimOffset,
                    _gridMap.WorldToCell(unit.Position));
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
                else if (!unit.HasAttackTarget && !unit.IsMoving)
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
