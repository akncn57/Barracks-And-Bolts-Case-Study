using BarracksAndBolts.Core.Session;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>
    /// Fills the map with resources when the gameplay scene starts.
    /// IStartable, not IInitializable: Initialize runs while the scope is still being built,
    /// before the view spawner is injected, so its views would miss the placed events.
    /// </summary>
    public sealed class ResourceMapInitializer : IStartable
    {
        private readonly ResourceDistributor _distributor;
        private readonly GameSessionSettings _session;

        public ResourceMapInitializer(ResourceDistributor distributor, GameSessionSettings session)
        {
            _distributor = distributor;
            _session = session;
        }

        public void Start()
        {
            // Logged so a map with a problem can be made again.
            Debug.Log($"Map seed: {_session.Seed}");
            _distributor.Distribute(_session.Seed);
        }
    }
}
