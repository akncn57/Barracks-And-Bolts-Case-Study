using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Teams;

namespace BarracksAndBolts.Gameplay.Sandbox
{
    /// <summary>
    /// Sandbox switches: which side new buildings and units belong to, whether the battle AI runs,
    /// and whether the debug view is on. With no friendly fire, the side is how a tester gets enemies to attack.
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

        /// <summary>Enemies and idle soldiers pick targets by themselves. Off until the tester turns it on.</summary>
        public bool IsBattleAiOn { get; private set; }

        /// <summary>The grid and the paths of selected units are drawn on the map.</summary>
        public bool IsDebugViewOn { get; private set; }

        public void ToggleFaction()
        {
            if (!IsEnabled)
                return;

            ActiveFaction = ActiveFaction == Faction.Player ? Faction.Enemy : Faction.Player;
            _eventBus.Publish(new SandboxFactionChangedEvent());
        }

        public void ToggleBattleAi()
        {
            if (!IsEnabled)
                return;

            IsBattleAiOn = !IsBattleAiOn;
            _eventBus.Publish(new SandboxSwitchesChangedEvent());
        }

        /// <summary>For tools that start a fight: a raid or a test battle is no use with nobody picking targets.</summary>
        public void TurnBattleAiOn()
        {
            if (!IsEnabled || IsBattleAiOn)
                return;

            IsBattleAiOn = true;
            _eventBus.Publish(new SandboxSwitchesChangedEvent());
        }

        public void ToggleDebugView()
        {
            if (!IsEnabled)
                return;

            IsDebugViewOn = !IsDebugViewOn;
            _eventBus.Publish(new SandboxSwitchesChangedEvent());
        }
    }

    /// <summary>The side being built for changed. Read it from <see cref="SandboxService"/>.</summary>
    public readonly struct SandboxFactionChangedEvent : IGameEvent
    {
    }

    /// <summary>The battle AI or the debug view was switched. Read them from <see cref="SandboxService"/>.</summary>
    public readonly struct SandboxSwitchesChangedEvent : IGameEvent
    {
    }

    /// <summary>Everything was taken off the sandbox map. A running raid stops with it.</summary>
    public readonly struct SandboxMapClearedEvent : IGameEvent
    {
    }
}
