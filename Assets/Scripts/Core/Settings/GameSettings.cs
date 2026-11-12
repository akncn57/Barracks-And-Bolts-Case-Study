using System;

namespace BarracksAndBolts.Core.Settings
{
    public enum UiSize
    {
        Small,
        Normal,
        Large
    }

    public enum CameraSpeed
    {
        Slow,
        Normal,
        Fast
    }

    /// <summary>
    /// The player's settings. They live for the whole app, are saved at once on every change
    /// and are read back on the next start. Each change raises <see cref="Changed"/>.
    /// </summary>
    public sealed class GameSettings
    {
        private const string FullscreenKey = "settings.fullscreen";
        private const string VSyncKey = "settings.vsync";
        private const string UiSizeKey = "settings.uiSize";
        private const string CameraSpeedKey = "settings.cameraSpeed";
        private const string EdgePanKey = "settings.edgePan";
        private const string StatsKey = "settings.stats";

        private readonly ISettingsStorage _storage;

        public GameSettings(ISettingsStorage storage)
        {
            _storage = storage;
            // Windowed on first start, like the player settings; fullscreen is one click in Settings.
            IsFullscreen = storage.GetInt(FullscreenKey, 0) == 1;
            IsVSyncOn = storage.GetInt(VSyncKey, 1) == 1;
            UiSize = ToEnum(storage.GetInt(UiSizeKey, (int)UiSize.Normal), UiSize.Normal);
            CameraSpeed = ToEnum(storage.GetInt(CameraSpeedKey, (int)CameraSpeed.Normal), CameraSpeed.Normal);
            IsEdgePanOn = storage.GetInt(EdgePanKey, 1) == 1;
            IsStatsShown = storage.GetInt(StatsKey, 1) == 1;
        }

        public event Action Changed;

        public bool IsFullscreen { get; private set; }
        public bool IsVSyncOn { get; private set; }
        public UiSize UiSize { get; private set; }
        public CameraSpeed CameraSpeed { get; private set; }
        public bool IsEdgePanOn { get; private set; }

        /// <summary>The FPS and pathfinding panel in the top right corner.</summary>
        public bool IsStatsShown { get; private set; }

        /// <summary>How much bigger than the 1920x1080 layout the UI is drawn.</summary>
        public float UiScale => GetUiScale(UiSize);

        /// <summary>Multiplies the camera's pan speed.</summary>
        public float CameraSpeedFactor => GetCameraSpeedFactor(CameraSpeed);

        public static float GetUiScale(UiSize size)
        {
            switch (size)
            {
                case UiSize.Small:
                    return 0.9f;
                case UiSize.Large:
                    return 1.15f;
                default:
                    return 1f;
            }
        }

        public static float GetCameraSpeedFactor(CameraSpeed speed)
        {
            switch (speed)
            {
                case CameraSpeed.Slow:
                    return 0.6f;
                case CameraSpeed.Fast:
                    return 1.6f;
                default:
                    return 1f;
            }
        }

        public void ToggleFullscreen()
        {
            IsFullscreen = !IsFullscreen;
            Save(FullscreenKey, IsFullscreen ? 1 : 0);
        }

        public void ToggleVSync()
        {
            IsVSyncOn = !IsVSyncOn;
            Save(VSyncKey, IsVSyncOn ? 1 : 0);
        }

        /// <summary>Small, Normal, Large, then Small again.</summary>
        public void CycleUiSize()
        {
            UiSize = Next(UiSize);
            Save(UiSizeKey, (int)UiSize);
        }

        /// <summary>Slow, Normal, Fast, then Slow again.</summary>
        public void CycleCameraSpeed()
        {
            CameraSpeed = Next(CameraSpeed);
            Save(CameraSpeedKey, (int)CameraSpeed);
        }

        public void ToggleEdgePan()
        {
            IsEdgePanOn = !IsEdgePanOn;
            Save(EdgePanKey, IsEdgePanOn ? 1 : 0);
        }

        public void ToggleStats()
        {
            IsStatsShown = !IsStatsShown;
            Save(StatsKey, IsStatsShown ? 1 : 0);
        }

        private void Save(string key, int value)
        {
            _storage.SetInt(key, value);
            _storage.Save();
            Changed?.Invoke();
        }

        private static T Next<T>(T value) where T : Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var index = Array.IndexOf(values, value);
            return values[(index + 1) % values.Length];
        }

        // A value saved by another version of the game may not exist any more.
        private static T ToEnum<T>(int value, T fallback) where T : struct, Enum
        {
            return Enum.IsDefined(typeof(T), value) ? (T)(object)value : fallback;
        }
    }
}
