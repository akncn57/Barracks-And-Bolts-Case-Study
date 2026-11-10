using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.TimeControl;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>Pause and speed buttons on the resource bar. The active one is bright, the others dimmed.</summary>
    public sealed class TimeControlView : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [Tooltip("One button per speed, in the order of TimeControlService.Speeds: 1x, 2x, 3x.")]
        [SerializeField] private Button[] speedButtons;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new(0.6f, 0.6f, 0.6f, 1f);

        private readonly CompositeDisposable _subscriptions = new();
        private TimeControlService _timeControl;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(TimeControlService timeControl, IEventBus eventBus)
        {
            _timeControl = timeControl;
            _eventBus = eventBus;
        }

        private void Start()
        {
            pauseButton.onClick.AddListener(_timeControl.TogglePause);
            for (var i = 0; i < speedButtons.Length && i < TimeControlService.Speeds.Count; i++)
            {
                var speed = TimeControlService.Speeds[i];
                speedButtons[i].onClick.AddListener(() => _timeControl.SetSpeed(speed));
            }

            _eventBus.Subscribe<GameSpeedChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();

            // Listeners live as long as the buttons, which go with this view; nothing outside holds them.
            pauseButton.onClick.RemoveAllListeners();
            foreach (var button in speedButtons)
                button.onClick.RemoveAllListeners();
        }

        private void Refresh()
        {
            SetColor(pauseButton, _timeControl.IsPaused);
            for (var i = 0; i < speedButtons.Length && i < TimeControlService.Speeds.Count; i++)
                SetColor(speedButtons[i], !_timeControl.IsPaused && Mathf.Approximately(_timeControl.Speed, TimeControlService.Speeds[i]));
        }

        private void SetColor(Button button, bool isActive)
        {
            if (button.targetGraphic != null)
                button.targetGraphic.color = isActive ? activeColor : inactiveColor;
        }
    }
}
