using BarracksAndBolts.Popup;
using BarracksAndBolts.UI.Common;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Settings
{
    /// <summary>Opens the settings popup. The same view sits on the main menu's button and on the gear in a game.</summary>
    public sealed class SettingsButtonView : MonoBehaviour, IEscapeMenu
    {
        [SerializeField] private Button button;
        [SerializeField] private PopupData settingsPopup;

        private PopupSystem _popups;
        private PopupBase _open;

        [Inject]
        private void Construct(PopupSystem popups)
        {
            _popups = popups;
        }

        private void Start()
        {
            button.onClick.AddListener(Open);
            _popups.Closed += OnPopupClosed;
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(Open);
            if (_popups != null)
                _popups.Closed -= OnPopupClosed;
        }

        public void Open()
        {
            // A second click while it is open would stack a second window.
            if (_open == null)
                _open = _popups.Open(settingsPopup);
        }

        public bool TryCloseTop()
        {
            return _popups.CloseTop();
        }

        private void OnPopupClosed(PopupBase popup)
        {
            if (popup == _open)
                _open = null;
        }
    }
}
