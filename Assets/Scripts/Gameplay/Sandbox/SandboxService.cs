using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Teams;

namespace BarracksAndBolts.Gameplay.Sandbox
{
    /// <summary>
    /// Minimal sandbox: which side new buildings and units belong to.
    /// With no friendly fire, this is how a tester gets enemies to attack.
    /// </summary>
    public sealed class SandboxService
    {
        private readonly IEventBus _eventBus;

        public SandboxService(GameSessionSettings sessionSettings, IEventBus eventBus)
        {
            _eventBus = eventBus;
            IsEnabled = sessionSettings.Mode == GameMode.Sandbox;
        }

        public bool IsEnabled { get; }

        /// <summary>The side the player is currently building for. Always the player outside sandbox mode.</summary>
        public Faction ActiveFaction { get; private set; } = Faction.Player;

        public void ToggleFaction()
        {
            if (!IsEnabled)
                return;

            ActiveFaction = ActiveFaction == Faction.Player ? Faction.Enemy : Faction.Player;
            _eventBus.Publish(new SandboxFactionChangedEvent());
        }
    }

    /// <summary>The side being built for changed. Read it from <see cref="SandboxService"/>.</summary>
    public readonly struct SandboxFactionChangedEvent : IGameEvent
    {
    }
}
