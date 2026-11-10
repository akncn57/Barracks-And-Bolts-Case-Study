using System;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.Core.Settings
{
    /// <summary>Puts the window and VSync settings into effect at start and whenever they change.</summary>
    public sealed class DisplaySettingsApplier : IStartable, IDisposable
    {
        private readonly GameSettings _settings;

        public DisplaySettingsApplier(GameSettings settings)
        {
            _settings = settings;
        }

        public void Start()
        {
            _settings.Changed += Apply;
            Apply();
        }

        public void Dispose()
        {
            _settings.Changed -= Apply;
        }

        private void Apply()
        {
            // Borderless fullscreen switches instantly and keeps alt-tab cheap.
            var mode = _settings.IsFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.fullScreenMode != mode)
                Screen.fullScreenMode = mode;

            QualitySettings.vSyncCount = _settings.IsVSyncOn ? 1 : 0;
        }
    }
}
