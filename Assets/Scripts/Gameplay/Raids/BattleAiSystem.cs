using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Raids
{
    /// <summary>
    /// Picks targets in a normal game. Enemies march on the Castle and turn on whatever comes near; since buildings
    /// count too, a wall in their way gets knocked down. Idle soldiers of the player attack what comes near them.
    /// CombatSystem does the walking and the hitting. Sandbox games keep every order in the player's hands.
    /// </summary>
    public sealed class BattleAiSystem : ISimulationSystem
    {
        // Targets don't change every frame, so looking a few times a second is enough.
        private const float ScanInterval = 0.25f;

        // A target that ran this many times the aggro range away is given up on.
        private const float LeashFactor = 2f;

        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly CombatService _combat;
        private readonly RaidSettings _settings;
        private readonly NormalModeSettings _normalMode;
        private readonly bool _isEnabled;
        private float _scanDelayLeft;

        public BattleAiSystem(UnitService units, BuildingService buildings, CombatService combat, RaidSettings settings,
            NormalModeSettings normalMode, SandboxService sandbox)
        {
            _units = units;
            _buildings = buildings;
            _combat = combat;
            _settings = settings;
            _normalMode = normalMode;
            _isEnabled = !sandbox.IsEnabled;
        }

        public SimulationPhase Phase => SimulationPhase.AI;

        public void Tick(float deltaTime)
        {
            if (!_isEnabled)
                return;

            _scanDelayLeft -= deltaTime;
            if (_scanDelayLeft > 0f)
                return;

            _scanDelayLeft = ScanInterval;
            var castle = FindCastle();
            foreach (var unit in _units.Units)
            {
                if (unit.IsDead)
                    continue;

                if (unit.Faction == Faction.Enemy)
                    UpdateEnemy(unit, castle);
                else if (!unit.Definition.CanGather && !unit.IsMoving && !unit.HasAttackTarget)
                    UpdateIdleSoldier(unit);
            }
        }

        private void UpdateEnemy(UnitModel unit, GameEntityId castle)
        {
            var isChasingOther = unit.HasAttackTarget && unit.AttackTarget != castle;
            if (isChasingOther)
            {
                if (_combat.TryGetTarget(unit.AttackTarget, unit.Position, out var target) && target.Distance > _settings.AggroRange * LeashFactor)
                    unit.ClearAttackTarget();

                return;
            }

            var near = _combat.FindNearestHostile(unit.Position, Faction.Enemy, _settings.AggroRange);
            var next = near.IsValid ? near : castle;
            if (next.IsValid && unit.AttackTarget != next)
                unit.SetAttackTarget(next);
        }

        private void UpdateIdleSoldier(UnitModel unit)
        {
            var near = _combat.FindNearestHostile(unit.Position, unit.Faction, _settings.AggroRange);
            if (near.IsValid)
                unit.SetAttackTarget(near);
        }

        private GameEntityId FindCastle()
        {
            foreach (var building in _buildings.Buildings)
            {
                if (building.Definition == _normalMode.Castle && building.Faction == Faction.Player)
                    return building.Id;
            }

            return GameEntityId.None;
        }
    }
}
