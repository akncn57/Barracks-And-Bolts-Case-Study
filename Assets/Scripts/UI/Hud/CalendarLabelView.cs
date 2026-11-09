using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>The day and the time of day on the top bar. Hidden in sandbox mode, which has no calendar.</summary>
    public sealed class CalendarLabelView : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private readonly CompositeDisposable _subscriptions = new();
        private CalendarService _calendar;
        private RaidSettings _settings;
        private IEventBus _eventBus;
        private bool _shownNight;

        [Inject]
        private void Construct(CalendarService calendar, RaidSettings settings, IEventBus eventBus)
        {
            _calendar = calendar;
            _settings = settings;
            _eventBus = eventBus;
        }

        private void Start()
        {
            if (!_calendar.IsEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            _eventBus.Subscribe<DayStartedEvent>(_ => Refresh()).AddTo(_subscriptions);
            _eventBus.Subscribe<RaidEndedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void Update()
        {
            // No event for nightfall; nothing but this label cares about the exact moment.
            if (_shownNight != _calendar.IsNight || (_calendar.IsFrozen && !label.text.EndsWith("Raid!")))
                Refresh();
        }

        private void Refresh()
        {
            _shownNight = _calendar.IsNight;
            var time = _calendar.IsFrozen ? "Raid!" : _shownNight ? "Night" : "Day";
            label.text = $"Day {Mathf.Max(_calendar.Day, 1)} / {_settings.LastDay}  -  {time}";
        }
    }
}
