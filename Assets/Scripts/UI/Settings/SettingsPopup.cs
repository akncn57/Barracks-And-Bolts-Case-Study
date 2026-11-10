using BarracksAndBolts.Core.Settings;
using BarracksAndBolts.Popup;
using BarracksAndBolts.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Settings
{
    /// <summary>
    /// The settings window. Each row is a button that steps through its choices and is saved at once.
    /// In a game it pauses while open and has a way back to the main menu.
    /// </summary>
    public sealed class SettingsPopup : PopupBase
    {
        [SerializeField] private Button windowButton;
        [SerializeField] private TMP_Text windowLabel;
        [SerializeField] private Button vSyncButton;
        [SerializeField] private TMP_Text vSyncLabel;
        [SerializeField] private Button uiSizeButton;
        [SerializeField] private TMP_Text uiSizeLabel;
        [SerializeField] private Button cameraSpeedButton;
        [SerializeField] private TMP_Text cameraSpeedLabel;
        [SerializeField] private Button edgePanButton;
        [SerializeField] private TMP_Text edgePanLabel;
        [SerializeField] private Button statsButton;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private Button controlsButton;
        [Tooltip("The Controls page, opened over this window.")]
        [SerializeField] private PopupData controlsPopup;
        [SerializeField] private Button menuButton;

        private GameSettings _settings;
        private ISettingsContext _context;

        [Inject]
        private void Construct(GameSettings settings, ISettingsContext context)
        {
            _settings = settings;
            _context = context;
        }

        protected override void Awake()
        {
            base.Awake();
            ButtonFeedback.AddToChildren(transform);
        }

        public override void OnOpened()
        {
            windowButton.onClick.AddListener(_settings.ToggleFullscreen);
            vSyncButton.onClick.AddListener(_settings.ToggleVSync);
            uiSizeButton.onClick.AddListener(_settings.CycleUiSize);
            cameraSpeedButton.onClick.AddListener(_settings.CycleCameraSpeed);
            edgePanButton.onClick.AddListener(_settings.ToggleEdgePan);
            statsButton.onClick.AddListener(_settings.ToggleStats);
            controlsButton.onClick.AddListener(OnControlsClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
            menuButton.gameObject.SetActive(_context.CanLeaveToMenu);
            _settings.Changed += Refresh;

            _context.OnSettingsOpened();
            Refresh();
        }

        private void OnControlsClicked()
        {
            _popupSystem.Open(controlsPopup);
        }

        protected override void OnCloseButtonClicked()
        {
            // The game runs on as the window starts to close, not after its animation.
            _context.OnSettingsClosed();
            base.OnCloseButtonClicked();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_settings != null)
                _settings.Changed -= Refresh;

            windowButton.onClick.RemoveAllListeners();
            vSyncButton.onClick.RemoveAllListeners();
            uiSizeButton.onClick.RemoveAllListeners();
            cameraSpeedButton.onClick.RemoveAllListeners();
            edgePanButton.onClick.RemoveAllListeners();
            controlsButton.onClick.RemoveAllListeners();
            statsButton.onClick.RemoveAllListeners();
            menuButton.onClick.RemoveAllListeners();
        }

        private void OnMenuClicked()
        {
            menuButton.interactable = false;
            _context.LeaveToMenu();
        }

        private void Refresh()
        {
            windowLabel.text = _settings.IsFullscreen ? "Window: Fullscreen" : "Window: Windowed";
            vSyncLabel.text = _settings.IsVSyncOn ? "VSync: On" : "VSync: Off";
            uiSizeLabel.text = $"UI Size: {_settings.UiSize}";
            cameraSpeedLabel.text = $"Camera Speed: {_settings.CameraSpeed}";
            edgePanLabel.text = _settings.IsEdgePanOn ? "Edge Scrolling: On" : "Edge Scrolling: Off";
            statsLabel.text = _settings.IsStatsShown ? "FPS Panel: On" : "FPS Panel: Off";
        }
    }
}
