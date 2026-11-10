using BarracksAndBolts.Gameplay.TimeControl;
using BarracksAndBolts.Popup;
using BarracksAndBolts.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Settings
{
    /// <summary>
    /// Every key and mouse action of the game in one list. Opens with F1 in a game and from the Settings window,
    /// on the main menu too. In a game it pauses while open, and the game runs on only if it was running before.
    /// </summary>
    public sealed class ControlsPopup : PopupBase
    {
        [Tooltip("Inside the scroll view's content; the content's layout gives it the height it needs.")]
        [SerializeField] private TMP_Text body;
        [SerializeField] private ScrollRect scroll;

        // Only a game has time to stop; on the main menu there is none.
        private TimeControlService _time;
        private bool _wasPaused;

        [Inject]
        private void Construct(IObjectResolver resolver)
        {
            resolver.TryResolve(out _time);
        }

        protected override void Awake()
        {
            base.Awake();
            ButtonFeedback.AddToChildren(transform);
        }

        public override void OnOpened()
        {
            body.text = ControlsText.Body;

            // The window grows from scale 0. At that scale the scroll view can't measure itself and clamps the list
            // to a wrong place, so it waits switched off with the list at its top until the window has its size.
            scroll.enabled = false;
            scroll.content.anchoredPosition = Vector2.zero;

            if (_time == null)
                return;

            _wasPaused = _time.IsPaused;
            _time.SetPaused(true);
        }

        private void Update()
        {
            if (scroll.enabled || transform.localScale.x < 1f)
                return;

            scroll.content.anchoredPosition = Vector2.zero;
            scroll.enabled = true;
        }

        protected override void OnCloseButtonClicked()
        {
            // The game runs on as the window starts to close, not after its animation.
            if (_time != null && !_wasPaused)
                _time.SetPaused(false);

            base.OnCloseButtonClicked();
        }
    }
}
