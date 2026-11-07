namespace BarracksAndBolts.Core.Simulation
{
    /// <summary>
    /// Execution order of simulation systems within a frame. Gaps leave room for new phases.
    /// </summary>
    public enum SimulationPhase
    {
        Input = 0,
        Time = 100,
        AI = 200,
        Movement = 300,
        Combat = 400,
        Cleanup = 500,
        Presentation = 600
    }
}
