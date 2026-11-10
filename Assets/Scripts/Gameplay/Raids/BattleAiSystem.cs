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
    /// CombatSystem does the walking and the hitting. In sandbox mode it runs only while the tester has switched it on;
    /// with no Castle there, enemies march on the nearest thing of the player instead.
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
        private readonly SandboxService _sandbox;
        private float _scanDelayLeft;

        public BattleAiSystem(UnitService units, BuildingService buildings, CombatService combat, RaidSettings settings,
            NormalModeSettings normalMode, SandboxService sandbox)
        {
            _units = units;
            _buildings = buildings;
            _combat = combat;
            _settings = settings;
            _normalMode = normalMode;
            _sandbox = sandbox;
        }

        public SimulationPhase Phase => SimulationPhase.AI;

        public void Tick(float deltaTime)
        {
            if (_sandbox.IsEnabled && !_sandbox.IsBattleAiOn)
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
            // No Castle (sandbox mode): the nearest building or unit of the player, however far, so raiders come.
            var march = castle.IsValid ? castle : _combat.FindNearestHostile(unit.Position, Faction.Enemy, float.MaxValue);
            var isChasingOther = unit.HasAttackTarget && unit.AttackTarget != march;
            if (isChasingOther)
            {
                if (_combat.TryGetTarget(unit.AttackTarget, unit.Position, out var target) && target.Distance > _settings.AggroRange * LeashFactor)
                    unit.ClearAttackTarget();

                return;
            }

            var near = _combat.FindNearestHostile(unit.Position, Faction.Enemy, _settings.AggroRange);
            var next = near.IsValid ? near : march;
            if (next.IsValid && unit.AttackTarget != next)
                unit.SetAttackTarget(next);
        }

        private void UpdateIdleSoldier(UnitModel unit)
        {
            // A soldier holding its position doesn't walk, so only what it can hit from here counts.
            var range = unit.IsHoldingPosition ? unit.Definition.AttackRange : _settings.AggroRange;
            var near = _combat.FindNearestHostile(unit.Position, unit.Faction, range);
            if (near.IsValid)
                unit.SpotEnemy(near);
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
