using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Input;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Popup;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.UI.Settings
{
    /// <summary>
    /// F1 opens and closes the Controls page in a game. A moment after the game starts, a notice tells the player
    /// about it once, so nobody has to find the keys by chance.
    /// </summary>
    public sealed class ControlsLauncher : IStartable, IPostTickable, IDisposable
    {
        private const float HintDelay = 2f;
        private const string Hint = "Press F1 to see the controls";

        private readonly PopupSystem _popups;
        private readonly PopupData _controlsPopup;
        private readonly IEventBus _eventBus;
        private readonly GameInputActions.GameplayActions _actions;
        private PopupBase _open;
        private float _hintAt;
        private bool _isHintShown;

        public ControlsLauncher(PopupSystem popups, PopupData controlsPopup, IEventBus eventBus, GameInputActions inputActions)
        {
            _popups = popups;
            _controlsPopup = controlsPopup;
            _eventBus = eventBus;
            _actions = inputActions.Gameplay;
        }

        public void Start()
        {
            _popups.Closed += OnPopupClosed;
            _hintAt = Time.unscaledTime + HintDelay;
        }

        public void Dispose()
        {
            if (_popups != null)
                _popups.Closed -= OnPopupClosed;
        }

        public void PostTick()
        {
            if (!_isHintShown && Time.unscaledTime >= _hintAt)
            {
                _isHintShown = true;
                _eventBus.Publish(new GameNoticeEvent(Hint, isAlert: false));
            }

            if (!_actions.enabled || !_actions.ShowControls.WasPressedThisFrame())
                return;

            if (_open != null)
                _popups.CloseTop();
            else
                _open = _popups.Open(_controlsPopup);
        }

        private void OnPopupClosed(PopupBase popup)
        {
            if (popup == _open)
                _open = null;
        }
    }
}
