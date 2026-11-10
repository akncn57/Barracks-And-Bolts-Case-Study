using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// Control groups 1 to 9: Ctrl and a number saves the selection, the number selects it again.
    /// Only the player's own units and buildings are kept. Ids are never used twice, so a dead unit's id simply
    /// stops being found; a sheltered villager is back in the group when it walks out.
    /// </summary>
    public sealed class ControlGroupService
    {
        public const int GroupCount = 9;

        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly GridMap _gridMap;
        private readonly List<GameEntityId>[] _groups = new List<GameEntityId>[GroupCount + 1];
        private readonly List<GameEntityId> _living = new();

        public ControlGroupService(UnitService units, BuildingService buildings, GridMap gridMap)
        {
            _units = units;
            _buildings = buildings;
            _gridMap = gridMap;
            for (var i = 0; i < _groups.Length; i++)
                _groups[i] = new List<GameEntityId>();
        }

        /// <param name="group">1 to 9.</param>
        /// <returns>How many of the ids went into the group.</returns>
        public int Assign(int group, IReadOnlyList<GameEntityId> ids)
        {
            var list = _groups[CheckGroup(group)];
            list.Clear();
            foreach (var id in ids)
            {
                if (IsOwnUnit(id) || IsOwnBuilding(id))
                    list.Add(id);
            }

            return list.Count;
        }

        /// <summary>The members of the group that are on the map now. The list is reused by the next call.</summary>
        public IReadOnlyList<GameEntityId> GetLiving(int group)
        {
            _living.Clear();
            foreach (var id in _groups[CheckGroup(group)])
            {
                if (IsOnMap(id))
                    _living.Add(id);
            }

            return _living;
        }

        /// <summary>The middle of the group's living members, for the camera.</summary>
        public bool TryGetCenter(int group, out Vector2 center)
        {
            var sum = Vector2.zero;
            var count = 0;
            foreach (var id in GetLiving(group))
            {
                if (_units.TryGet(id, out var unit))
                    sum += unit.Position;
                else if (_buildings.TryGet(id, out var building))
                    sum += _gridMap.Origin + building.Area.center;

                count++;
            }

            center = count > 0 ? sum / count : Vector2.zero;
            return count > 0;
        }

        private bool IsOnMap(GameEntityId id)
        {
            if (_units.TryGet(id, out var unit))
                return !unit.IsDead;

            return _buildings.TryGet(id, out var building) && !building.IsDead;
        }

        private bool IsOwnUnit(GameEntityId id)
        {
            return _units.TryGet(id, out var unit) && unit.Faction == Faction.Player;
        }

        private bool IsOwnBuilding(GameEntityId id)
        {
            return _buildings.TryGet(id, out var building) && building.Faction == Faction.Player;
        }

        private static int CheckGroup(int group)
        {
            if (group < 1 || group > GroupCount)
                throw new ArgumentOutOfRangeException(nameof(group), group, "Groups go from 1 to 9.");

            return group;
        }
    }
}
