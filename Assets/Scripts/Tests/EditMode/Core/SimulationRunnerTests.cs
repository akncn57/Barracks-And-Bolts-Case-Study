using System.Collections.Generic;
using BarracksAndBolts.Core.Simulation;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Core
{
    public class SimulationRunnerTests
    {
        private sealed class RecordingSystem : ISimulationSystem
        {
            private readonly string _name;
            private readonly List<string> _log;

            public RecordingSystem(string name, SimulationPhase phase, List<string> log)
            {
                _name = name;
                Phase = phase;
                _log = log;
            }

            public SimulationPhase Phase { get; }
            public float LastDeltaTime { get; private set; }

            public void Tick(float deltaTime)
            {
                LastDeltaTime = deltaTime;
                _log.Add(_name);
            }
        }

        private GameClock _clock;
        private SimulationRunner _runner;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _clock = new GameClock();
            _runner = new SimulationRunner(_clock);
            _log = new List<string>();
        }

        [Test]
        public void Step_RunsSystemsInPhaseOrder_RegardlessOfAddOrder()
        {
            _runner.Add(new RecordingSystem("combat", SimulationPhase.Combat, _log));
            _runner.Add(new RecordingSystem("input", SimulationPhase.Input, _log));
            _runner.Add(new RecordingSystem("movement", SimulationPhase.Movement, _log));

            _runner.Step(0.016f);

            CollectionAssert.AreEqual(new[] { "input", "movement", "combat" }, _log);
        }

        [Test]
        public void Step_SamePhase_KeepsAddOrder()
        {
            _runner.Add(new RecordingSystem("first", SimulationPhase.AI, _log));
            _runner.Add(new RecordingSystem("second", SimulationPhase.AI, _log));

            _runner.Step(0.016f);

            CollectionAssert.AreEqual(new[] { "first", "second" }, _log);
        }

        [Test]
        public void Step_WhenPaused_DoesNotTickSystems()
        {
            _runner.Add(new RecordingSystem("system", SimulationPhase.AI, _log));
            _clock.SetPaused(true);

            _runner.Step(0.016f);

            Assert.IsEmpty(_log);
        }

        [Test]
        public void Step_PassesScaledDeltaTime()
        {
            var system = new RecordingSystem("system", SimulationPhase.AI, _log);
            _runner.Add(system);
            _clock.SetTimeScale(2f);

            _runner.Step(0.05f);

            Assert.AreEqual(0.1f, system.LastDeltaTime, 0.0001f);
        }

        [Test]
        public void Add_SameSystemTwice_TicksOnce()
        {
            var system = new RecordingSystem("system", SimulationPhase.AI, _log);
            _runner.Add(system);
            _runner.Add(system);

            _runner.Step(0.016f);

            Assert.AreEqual(1, _log.Count);
        }
    }
}
