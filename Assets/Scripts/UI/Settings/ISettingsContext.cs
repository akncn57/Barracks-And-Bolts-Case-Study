using BarracksAndBolts.Core.Scenes;
using BarracksAndBolts.Gameplay.TimeControl;
using Cysharp.Threading.Tasks;

namespace BarracksAndBolts.UI.Settings
{
    /// <summary>What the settings popup does differently in the main menu and in a game.</summary>
    public interface ISettingsContext
    {
        /// <summary>Shows the "Main Menu" button. Only in a game.</summary>
        bool CanLeaveToMenu { get; }

        void OnSettingsOpened();
        void OnSettingsClosed();
        void LeaveToMenu();
    }

    public sealed class MainMenuSettingsContext : ISettingsContext
    {
        public bool CanLeaveToMenu => false;

        public void OnSettingsOpened()
        {
        }

        public void OnSettingsClosed()
        {
        }

        public void LeaveToMenu()
        {
        }
    }

    /// <summary>The game waits while the settings are open, and runs on afterwards only if it was running before.</summary>
    public sealed class GameplaySettingsContext : ISettingsContext
    {
        private readonly TimeControlService _time;
        private readonly SceneLoader _sceneLoader;
        private bool _wasPaused;

        public GameplaySettingsContext(TimeControlService time, SceneLoader sceneLoader)
        {
            _time = time;
            _sceneLoader = sceneLoader;
        }

        public bool CanLeaveToMenu => true;

        public void OnSettingsOpened()
        {
            _wasPaused = _time.IsPaused;
            _time.SetPaused(true);
        }

        public void OnSettingsClosed()
        {
            if (!_wasPaused)
                _time.SetPaused(false);
        }

        public void LeaveToMenu()
        {
            _sceneLoader.LoadAsync(SceneNames.MainMenu).Forget();
        }
    }
}
