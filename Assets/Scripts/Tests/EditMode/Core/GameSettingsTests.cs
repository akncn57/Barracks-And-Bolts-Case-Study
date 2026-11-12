using System.Collections.Generic;
using BarracksAndBolts.Core.Settings;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Core
{
    public class GameSettingsTests
    {
        private sealed class MemoryStorage : ISettingsStorage
        {
            public readonly Dictionary<string, int> Values = new();
            public int SaveCount { get; private set; }

            public int GetInt(string key, int defaultValue)
            {
                return Values.TryGetValue(key, out var value) ? value : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                Values[key] = value;
            }

            public void Save()
            {
                SaveCount++;
            }
        }

        [Test]
        public void FreshStart_HasTheDefaults()
        {
            var settings = new GameSettings(new MemoryStorage());

            Assert.IsFalse(settings.IsFullscreen);
            Assert.IsTrue(settings.IsVSyncOn);
            Assert.AreEqual(UiSize.Normal, settings.UiSize);
            Assert.AreEqual(CameraSpeed.Normal, settings.CameraSpeed);
            Assert.IsTrue(settings.IsEdgePanOn);
            Assert.IsTrue(settings.IsStatsShown);
            Assert.AreEqual(1f, settings.UiScale);
            Assert.AreEqual(1f, settings.CameraSpeedFactor);
        }

        [Test]
        public void Changes_AreSavedAtOnce_AndReadBackNextTime()
        {
            var storage = new MemoryStorage();
            var settings = new GameSettings(storage);
            var changes = 0;
            settings.Changed += () => changes++;

            settings.ToggleFullscreen();
            settings.ToggleVSync();
            settings.CycleUiSize();
            settings.CycleCameraSpeed();
            settings.ToggleEdgePan();
            settings.ToggleStats();

            Assert.AreEqual(6, changes);
            Assert.AreEqual(6, storage.SaveCount, "Saved on every change, so a crash loses nothing.");

            var nextStart = new GameSettings(storage);
            Assert.IsTrue(nextStart.IsFullscreen);
            Assert.IsFalse(nextStart.IsVSyncOn);
            Assert.AreEqual(UiSize.Large, nextStart.UiSize);
            Assert.AreEqual(CameraSpeed.Fast, nextStart.CameraSpeed);
            Assert.IsFalse(nextStart.IsEdgePanOn);
            Assert.IsFalse(nextStart.IsStatsShown);
        }

        [Test]
        public void Cycling_WrapsAround()
        {
            var settings = new GameSettings(new MemoryStorage());

            settings.CycleUiSize();
            settings.CycleUiSize();
            Assert.AreEqual(UiSize.Small, settings.UiSize);
            Assert.Less(settings.UiScale, 1f);

            settings.CycleCameraSpeed();
            settings.CycleCameraSpeed();
            Assert.AreEqual(CameraSpeed.Slow, settings.CameraSpeed);
            Assert.Less(settings.CameraSpeedFactor, 1f);
        }

        [Test]
        public void UnknownSavedValue_FallsBackToNormal()
        {
            var storage = new MemoryStorage();
            storage.Values["settings.uiSize"] = 42;
            storage.Values["settings.cameraSpeed"] = -3;

            var settings = new GameSettings(storage);

            Assert.AreEqual(UiSize.Normal, settings.UiSize);
            Assert.AreEqual(CameraSpeed.Normal, settings.CameraSpeed);
        }
    }
}
