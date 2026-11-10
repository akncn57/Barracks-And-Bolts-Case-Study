using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// Units on an attack-move fight the enemies they meet, then walk on to their cell. A player's order, so unlike
    /// BattleAiSystem it also runs while the sandbox AI is off. CombatSystem does the chasing and the hitting.
    /// </summary>
    public sealed class AttackMoveSystem : ISimulationSystem
    {
        // Looking a few times a second is enough, the same as the battle AI.
        private const float ScanInterval = 0.25f;

        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly CombatService _combat;
        private readonly GridMap _gridMap;
        private readonly RaidSettings _settings;
        private float _scanDelayLeft;

        public AttackMoveSystem(UnitService units, UnitMovementSystem movement, CombatService combat, GridMap gridMap, RaidSettings settings)
        {
            _units = units;
            _movement = movement;
            _combat = combat;
            _gridMap = gridMap;
            _settings = settings;
        }

        public SimulationPhase Phase => SimulationPhase.AI;

        public void Tick(float deltaTime)
        {
            _scanDelayLeft -= deltaTime;
            if (_scanDelayLeft > 0f)
                return;

            _scanDelayLeft = ScanInterval;
            foreach (var unit in _units.Units)
            {
                if (!unit.IsDead && unit.AttackMoveCell.HasValue && !unit.HasAttackTarget)
                    UpdateUnit(unit, unit.AttackMoveCell.Value);
            }
        }

        private void UpdateUnit(UnitModel unit, Vector2Int cell)
        {
            var enemy = _combat.FindNearestHostile(unit.Position, unit.Faction, _settings.AggroRange);
            if (enemy.IsValid)
            {
                unit.SetAttackTarget(enemy);
                return;
            }

            if (unit.IsMoving)
                return;

            // Arrived, or the fight is over and this is the way on.
            var here = _gridMap.WorldToCell(unit.Position);
            if (here == cell)
            {
                unit.ClearStance();
                return;
            }

            _movement.SendTo(unit, cell);

            // No way closer from here: the walk is over, or it would look for a path four times a second.
            if (!unit.IsMoving || _gridMap.WorldToCell(unit.Destination) == here)
                unit.ClearStance();
        }
    }
}
