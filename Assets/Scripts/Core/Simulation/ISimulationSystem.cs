namespace BarracksAndBolts.Core.Simulation
{
    /// <summary>
    /// A gameplay system updated by <see cref="SimulationRunner"/> instead of its own Update.
    /// </summary>
    public interface ISimulationSystem
    {
        SimulationPhase Phase { get; }

        /// <param name="deltaTime">Gameplay seconds; already includes pause and game speed.</param>
        void Tick(float deltaTime);
    }
}
