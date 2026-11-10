using System;
using BarracksAndBolts.Core.Scenes;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Popup;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.MainMenu
{
    /// <summary>
    /// Play asks for the difficulty first and then starts a normal game; Sandbox starts at once, its difficulty is a
    /// button inside the game.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button sandboxButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private PopupData difficultyPopup;

        [Header("Entrance")]
        [Tooltip("Pop in one after another, from the top. Settings is here too; its click is handled by SettingsButtonView.")]
        [SerializeField] private RectTransform[] entranceOrder;
        [SerializeField, Min(0f)] private float entranceDelay = 0.35f;
        [SerializeField, Min(0f)] private float entranceStep = 0.08f;

        private SceneLoader _sceneLoader;
        private GameSessionSettings _sessionSettings;
        private PopupSystem _popups;
        private DifficultyPopup _openPopup;

        [Inject]
        private void Construct(SceneLoader sceneLoader, GameSessionSettings sessionSettings, PopupSystem popups)
        {
            _sceneLoader = sceneLoader;
            _sessionSettings = sessionSettings;
            _popups = popups;
        }

        private void Start()
        {
            // Scale only: the layout group owns the buttons' positions.
            for (var i = 0; i < entranceOrder.Length; i++)
            {
                var item = entranceOrder[i];
                item.localScale = Vector3.zero;
                item.DOScale(1f, 0.35f).SetDelay(entranceDelay + i * entranceStep).SetEase(Ease.OutBack).SetLink(item.gameObject);
            }
        }

        private void OnEnable()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            sandboxButton.onClick.AddListener(OnSandboxClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            sandboxButton.onClick.RemoveListener(OnSandboxClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnPlayClicked()
        {
            // A second click while it is open would stack a second window.
            if (_openPopup != null)
                return;

            _openPopup = _popups.Open<DifficultyPopup>(difficultyPopup);
            _openPopup.Chosen += OnDifficultyChosen;
            _popups.Closed += OnPopupClosed;
        }

        private void OnDifficultyChosen(Difficulty difficulty)
        {
            _sessionSettings.Difficulty = difficulty;
            StartGame(GameMode.Normal);
        }

        private void OnPopupClosed(PopupBase popup)
        {
            if (popup != _openPopup)
                return;

            _openPopup.Chosen -= OnDifficultyChosen;
            _openPopup = null;
            _popups.Closed -= OnPopupClosed;
        }

        private void OnSandboxClicked()
        {
            StartGame(GameMode.Sandbox);
        }

        private void StartGame(GameMode mode)
        {
            _sessionSettings.Mode = mode;
            _sessionSettings.Seed = Environment.TickCount;
            SetButtonsInteractable(false);
            _sceneLoader.LoadAsync(SceneNames.Gameplay).Forget();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetButtonsInteractable(bool isInteractable)
        {
            playButton.interactable = isInteractable;
            sandboxButton.interactable = isInteractable;
            quitButton.interactable = isInteractable;
        }
    }
}
