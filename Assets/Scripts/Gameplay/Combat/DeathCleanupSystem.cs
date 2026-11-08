using BarracksAndBolts.Core.Simulation;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>Removes the dead at the end of the tick, after every other system has finished looping over them.</summary>
    public sealed class DeathCleanupSystem : ISimulationSystem
    {
        private readonly CombatService _combat;

        public DeathCleanupSystem(CombatService combat)
        {
            _combat = combat;
        }

        public SimulationPhase Phase => SimulationPhase.Cleanup;

        public void Tick(float deltaTime)
        {
            _combat.RemoveDead();
        }
    }
}
