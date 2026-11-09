using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Sandbox;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Calendar
{
    /// <summary>Days and nights of a normal game. A raid freezes the calendar until it is over. Sandbox games have no calendar.</summary>
    public sealed class CalendarService : ISimulationSystem
    {
        // Evening and dawn fade over this many seconds, so the screen doesn't jump between day and night.
        private const float TwilightSeconds = 15f;

        private readonly RaidSettings _settings;
        private readonly IEventBus _eventBus;
        private float _timeOfDay;

        public CalendarService(RaidSettings settings, SandboxService sandbox, IEventBus eventBus)
        {
            _settings = settings;
            _eventBus = eventBus;
            IsEnabled = !sandbox.IsEnabled;
        }

        public SimulationPhase Phase => SimulationPhase.Time;

        public bool IsEnabled { get; }

        /// <summary>The current day, starting from 1. 0 until the first tick.</summary>
        public int Day { get; private set; }

        public bool IsFrozen { get; private set; }
        public bool IsNight => _timeOfDay >= _settings.DaySeconds;

        /// <summary>0 in full daylight, 1 in the dark of the night.</summary>
        public float Darkness
        {
            get
            {
                var evening = (_timeOfDay - _settings.DaySeconds) / TwilightSeconds;
                var dawn = (_settings.DayLength - _timeOfDay) / TwilightSeconds;
                return Mathf.Clamp01(Mathf.Min(evening, dawn));
            }
        }

        public void Freeze()
        {
            IsFrozen = true;
        }

        public void Resume()
        {
            IsFrozen = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || IsFrozen)
                return;

            if (Day == 0)
                StartDay(1);

            _timeOfDay += deltaTime;
            if (_timeOfDay < _settings.DayLength)
                return;

            _timeOfDay -= _settings.DayLength;
            StartDay(Day + 1);
        }

        private void StartDay(int day)
        {
            Day = day;
            _eventBus.Publish(new DayStartedEvent(day));
        }
    }

    public readonly struct DayStartedEvent : IGameEvent
    {
        public DayStartedEvent(int day)
        {
            Day = day;
        }

        public int Day { get; }
    }
}
