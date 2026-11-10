using UnityEngine;

namespace BarracksAndBolts.Core.Settings
{
    /// <summary>Where <see cref="GameSettings"/> keeps its values. Tests use a dictionary instead of PlayerPrefs.</summary>
    public interface ISettingsStorage
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class PlayerPrefsSettingsStorage : ISettingsStorage
    {
        public int GetInt(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue);
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
