using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// Every simulation tick: buildings with a defense shoot enemy units in range. A building stays on its target
    /// until the target dies or leaves the range, then takes the nearest one. Works in every game mode.
    /// </summary>
    public sealed class BuildingDefenseSystem : ISimulationSystem
    {
        // With nobody in range, looking a few times a second is enough.
        private const float ScanInterval = 0.25f;

        // Arrows leave from the defender's chest, not its feet.
        private static readonly Vector2 MuzzleOffset = new(0f, 0.5f);

        private readonly BuildingService _buildings;
        private readonly CombatService _combat;
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;

        public BuildingDefenseSystem(BuildingService buildings, CombatService combat, GridMap gridMap, IEventBus eventBus)
        {
            _buildings = buildings;
            _combat = combat;
            _gridMap = gridMap;
            _eventBus = eventBus;
        }

        public SimulationPhase Phase => SimulationPhase.Combat;

        public void Tick(float deltaTime)
        {
            foreach (var building in _buildings.Buildings)
            {
                // The archer only climbs up once the tower is finished.
                if (!building.Definition.HasDefense || building.IsDead || !building.IsBuilt)
                    continue;

                building.TickShotDelay(deltaTime);
                if (building.ShotDelayLeft <= 0f)
                    TryShoot(building);
            }
        }

        private void TryShoot(BuildingModel building)
        {
            var definition = building.Definition;
            var center = _gridMap.Origin + building.Area.center;
            if (!IsInRange(building.DefenseTarget, center, definition.DefenseRange))
                building.SetDefenseTarget(_combat.FindNearestHostileUnit(center, building.Faction, definition.DefenseRange));

            if (!_combat.TryGetTarget(building.DefenseTarget, center, out var target))
            {
                building.StartShotDelay(ScanInterval);
                return;
            }

            building.StartShotDelay(definition.DefenseCooldown);
            var muzzle = _gridMap.AreaToWorldBottomCenter(building.Area) + definition.DefenderPosition + MuzzleOffset;
            _eventBus.Publish(new BuildingFiredEvent(building, muzzle, target.AimPoint));
            _combat.ApplyDamage(building.DefenseTarget, definition.DefenseDamage, building.Id);
        }

        private bool IsInRange(GameEntityId target, Vector2 center, float range)
        {
            return _combat.TryGetTarget(target, center, out var found) && found.Distance <= range;
        }
    }
}
