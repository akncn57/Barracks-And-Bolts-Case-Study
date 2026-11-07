using UnityEngine;

namespace BarracksAndBolts.Core.Simulation
{
    /// <summary>
    /// Owns gameplay time. Pause and game speed live here instead of Time.timeScale,
    /// so menus and UI tweens keep running while the game is paused.
    /// </summary>
    public sealed class GameClock
    {
        public const float MaxTimeScale = 3f;

        // A long hitch (loading, breakpoint) would otherwise move units through walls in one step.
        public const float MaxFrameDelta = 0.1f;

        public bool IsPaused { get; private set; }
        public float TimeScale { get; private set; } = 1f;

        /// <summary>Total gameplay seconds since the session started.</summary>
        public float ElapsedTime { get; private set; }

        public void SetPaused(bool isPaused)
        {
            IsPaused = isPaused;
        }

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Clamp(timeScale, 0f, MaxTimeScale);
        }

        /// <summary>Converts a real frame delta into gameplay delta and advances the clock.</summary>
        public float Advance(float realDeltaTime)
        {
            if (IsPaused || realDeltaTime <= 0f)
                return 0f;

            var gameDeltaTime = Mathf.Min(realDeltaTime, MaxFrameDelta) * TimeScale;
            ElapsedTime += gameDeltaTime;
            return gameDeltaTime;
        }
    }
}
