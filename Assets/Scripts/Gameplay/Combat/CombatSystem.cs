using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Units;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// Every simulation tick: units with a target walk into range and strike when their cooldown allows.
    /// A unit whose target is gone stops where it is. A unit holding its position drops a target out of range instead of chasing.
    /// </summary>
    public sealed class CombatSystem : ISimulationSystem
    {
        // A unit that stands exactly one cell from its target must count as in range despite float rounding.
        private const float RangeTolerance = 0.01f;

        // A chasing unit doesn't look for a new path more often than this. Keeps walled-off units from running A* every frame.
        private const float RepathInterval = 0.25f;

        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly CombatService _combat;
        private readonly IEventBus _eventBus;

        public CombatSystem(UnitService units, UnitMovementSystem movement, CombatService combat, IEventBus eventBus)
        {
            _units = units;
            _movement = movement;
            _combat = combat;
            _eventBus = eventBus;
        }

        public SimulationPhase Phase => SimulationPhase.Combat;

        public void Tick(float deltaTime)
        {
            _combat.TickScheduledHits(deltaTime);
            foreach (var unit in _units.Units)
            {
                unit.TickCombatTimers(deltaTime);
                if (!unit.IsDead && unit.HasAttackTarget)
                    UpdateAttacker(unit);
            }
        }

        private void UpdateAttacker(UnitModel unit)
        {
            if (!_combat.TryGetTarget(unit.AttackTarget, unit.Position, out var target))
            {
                unit.ClearAttackTarget();
                unit.Stop();
                return;
            }

            if (target.Distance <= unit.Definition.AttackRange + RangeTolerance)
                Strike(unit, target);
            else if (unit.IsHoldingPosition)
                unit.ClearAttackTarget();
            else
                Chase(unit, target);
        }

        private void Strike(UnitModel unit, in CombatTarget target)
        {
            unit.Stop();
            unit.FaceTowards(target.AimPoint);
            if (unit.AttackCooldownLeft > 0f)
                return;

            unit.StartAttackCooldown();

            // The animation starts now; the blow lands on its strike frame, an arrow when it arrives.
            var definition = unit.Definition;
            var flightTime = definition.GetFlightTime(target.Distance);
            _eventBus.Publish(new UnitAttackedEvent(unit, target.AimPoint, definition.HitDelay, flightTime));
            _combat.ScheduleHit(unit.AttackTarget, definition.Damage, unit.Id, definition.HitDelay, definition.HitDelay + flightTime);
        }

        private void Chase(UnitModel unit, in CombatTarget target)
        {
            // Still walking to the right cell, or searched a moment ago.
            if (unit.IsMoving && unit.ChasedCell == target.ChaseCell)
                return;

            if (unit.RepathDelayLeft > 0f)
                return;

            _movement.SendTo(unit, target.ChaseCell);
            unit.MarkChase(target.ChaseCell, RepathInterval);
        }
    }
}
