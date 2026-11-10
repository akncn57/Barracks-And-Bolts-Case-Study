using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Sandbox
{
    /// <summary>
    /// Sandbox load test: two armies of mixed soldiers face each other with the battle AI on,
    /// so pathfinding, combat and drawing all run with many units at once.
    /// </summary>
    public sealed class SandboxBattleSpawner
    {
        // Cells between the two army centers. About 50 units fill a 7x7 block, so this leaves open ground between them.
        private const int ArmyGap = 14;

        private readonly GameDatabase _database;
        private readonly UnitService _units;
        private readonly GridMap _gridMap;
        private readonly SandboxService _sandbox;
        private readonly List<UnitDefinition> _soldiers = new();

        public SandboxBattleSpawner(GameDatabase database, UnitService units, GridMap gridMap, SandboxService sandbox)
        {
            _database = database;
            _units = units;
            _gridMap = gridMap;
            _sandbox = sandbox;
        }

        /// <summary>The player's army goes left of the point, the enemy's right of it. Turns the battle AI on.</summary>
        /// <returns>How many units were placed. 0 outside sandbox mode.</returns>
        public int SpawnBattle(Vector2 center, int perSide)
        {
            if (!_sandbox.IsEnabled || perSide <= 0)
                return 0;

            CollectSoldiers();
            if (_soldiers.Count == 0)
                return 0;

            var middle = _gridMap.WorldToCell(center);
            var offset = new Vector2Int(ArmyGap / 2, 0);
            var placed = SpawnArmy(middle - offset, Faction.Player, perSide) + SpawnArmy(middle + offset, Faction.Enemy, perSide);
            _sandbox.TurnBattleAiOn();
            return placed;
        }

        private int SpawnArmy(Vector2Int cell, Faction faction, int count)
        {
            var placed = 0;
            for (var i = 0; i < count; i++)
            {
                // Each unit takes the nearest free cell, so the army grows outwards from the point.
                if (_units.TrySpawn(_soldiers[i % _soldiers.Count], cell, faction, out _))
                    placed++;
            }

            return placed;
        }

        private void CollectSoldiers()
        {
            _soldiers.Clear();
            foreach (var unit in _database.Units)
            {
                if (unit != null && !unit.CanGather)
                    _soldiers.Add(unit);
            }
        }
    }
}
