using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.Core.Simulation
{
    /// <summary>
    /// The single gameplay loop. Ticks every registered system once per frame, ordered by phase.
    /// </summary>
    public sealed class SimulationRunner : ITickable
    {
        private readonly GameClock _clock;
        private readonly List<ISimulationSystem> _systems = new();

        public SimulationRunner(GameClock clock)
        {
            _clock = clock;
        }

        public void Add(ISimulationSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (_systems.Contains(system))
                return;

            // Insert after the last system of the same or an earlier phase,
            // so systems within a phase keep their registration order.
            var index = _systems.FindLastIndex(existing => existing.Phase <= system.Phase) + 1;
            _systems.Insert(index, system);
        }

        void ITickable.Tick()
        {
            Step(Time.unscaledDeltaTime);
        }

        /// <summary>Runs one frame with the given real delta. Public so tests can drive the loop.</summary>
        public void Step(float realDeltaTime)
        {
            var deltaTime = _clock.Advance(realDeltaTime);
            if (deltaTime <= 0f)
                return;

            for (var i = 0; i < _systems.Count; i++)
                _systems[i].Tick(deltaTime);
        }
    }
}
