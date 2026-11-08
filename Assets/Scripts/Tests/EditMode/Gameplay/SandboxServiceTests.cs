using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class SandboxServiceTests
    {
        private static SandboxService Create(GameMode mode, IEventBus eventBus)
        {
            return new SandboxService(new GameSessionSettings { Mode = mode }, eventBus);
        }

        [Test]
        public void StartsOnThePlayersSide()
        {
            var sandbox = Create(GameMode.Sandbox, new EventBus());

            Assert.IsTrue(sandbox.IsEnabled);
            Assert.AreEqual(Faction.Player, sandbox.ActiveFaction);
        }

        [Test]
        public void ToggleFaction_SwitchesBackAndForthAndTells()
        {
            var eventBus = new EventBus();
            var sandbox = Create(GameMode.Sandbox, eventBus);
            var changed = 0;
            eventBus.Subscribe<SandboxFactionChangedEvent>(_ => changed++);

            sandbox.ToggleFaction();
            Assert.AreEqual(Faction.Enemy, sandbox.ActiveFaction);

            sandbox.ToggleFaction();
            Assert.AreEqual(Faction.Player, sandbox.ActiveFaction);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void OutsideSandboxMode_StaysOnThePlayersSide()
        {
            var eventBus = new EventBus();
            var sandbox = Create(GameMode.Normal, eventBus);
            var changed = 0;
            eventBus.Subscribe<SandboxFactionChangedEvent>(_ => changed++);

            sandbox.ToggleFaction();

            Assert.IsFalse(sandbox.IsEnabled);
            Assert.AreEqual(Faction.Player, sandbox.ActiveFaction);
            Assert.AreEqual(0, changed);
        }
    }
}
