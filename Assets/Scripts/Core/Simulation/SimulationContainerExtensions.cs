using VContainer;

namespace BarracksAndBolts.Core.Simulation
{
    public static class SimulationContainerExtensions
    {
        /// <summary>
        /// Registers a system and adds it to the <see cref="SimulationRunner"/> once the container is built.
        /// </summary>
        public static RegistrationBuilder RegisterSimulationSystem<TSystem>(this IContainerBuilder builder)
            where TSystem : class, ISimulationSystem
        {
            // VContainer can't resolve an empty collection, so systems are added one by one
            // instead of injecting IEnumerable<ISimulationSystem> into the runner.
            builder.RegisterBuildCallback(container =>
                container.Resolve<SimulationRunner>().Add(container.Resolve<TSystem>()));

            return builder.Register<TSystem>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }
    }
}
