using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Utils.Disposables;

namespace BarracksAndBolts.Gameplay.TimeControl
{
    /// <summary>
    /// Pause and game speed for the player, in both modes. Only the game clock changes, so the calendar, raids and
    /// every system follow it, while menus keep running. Once the game is over, nothing here can start the clock again.
    /// </summary>
    public sealed class TimeControlService : IDisposable
    {
        private static readonly float[] SpeedSteps = { 1f, 2f, 3f };

        private readonly GameClock _clock;
        private readonly IEventBus _eventBus;
        private readonly CompositeDisposable _subscriptions = new();
        private bool _isGameOver;

        public TimeControlService(GameClock clock, IEventBus eventBus)
        {
            _clock = clock;
            _eventBus = eventBus;
            eventBus.Subscribe<GameEndedEvent>(_ => _isGameOver = true).AddTo(_subscriptions);
        }

        public static IReadOnlyList<float> Speeds => SpeedSteps;

        public bool IsPaused => _clock.IsPaused;
        public float Speed => _clock.TimeScale;

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }

        public void SetPaused(bool isPaused)
        {
            if (_isGameOver || isPaused == IsPaused)
                return;

            _clock.SetPaused(isPaused);
            _eventBus.Publish(new GameSpeedChangedEvent());
        }

        /// <summary>Picking a speed also ends a pause, like in most strategy games.</summary>
        public void SetSpeed(float speed)
        {
            if (_isGameOver)
                return;

            _clock.SetTimeScale(speed);
            _clock.SetPaused(false);
            _eventBus.Publish(new GameSpeedChangedEvent());
        }

        public void SpeedUp()
        {
            SetSpeed(SpeedSteps[Math.Min(GetStepIndex() + 1, SpeedSteps.Length - 1)]);
        }

        public void SpeedDown()
        {
            SetSpeed(SpeedSteps[Math.Max(GetStepIndex() - 1, 0)]);
        }

        // The step nearest to the clock's speed, so a speed set from elsewhere still steps sensibly.
        private int GetStepIndex()
        {
            var nearest = 0;
            for (var i = 1; i < SpeedSteps.Length; i++)
            {
                if (Math.Abs(SpeedSteps[i] - Speed) < Math.Abs(SpeedSteps[nearest] - Speed))
                    nearest = i;
            }

            return nearest;
        }
    }

    /// <summary>The game was paused, resumed or sped up. Read it from <see cref="TimeControlService"/>.</summary>
    public readonly struct GameSpeedChangedEvent : IGameEvent
    {
    }
}
