using System;
using BarracksAndBolts.Core.Scenes;
using BarracksAndBolts.Core.Session;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.MainMenu
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button sandboxButton;
        [SerializeField] private Button quitButton;

        private SceneLoader _sceneLoader;
        private GameSessionSettings _sessionSettings;

        [Inject]
        private void Construct(SceneLoader sceneLoader, GameSessionSettings sessionSettings)
        {
            _sceneLoader = sceneLoader;
            _sessionSettings = sessionSettings;
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
            StartGame(GameMode.Normal);
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
