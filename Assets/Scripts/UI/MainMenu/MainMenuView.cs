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
            sandboxButton.onClick.AddListener(OnSandboxClicked);
            quitButton.onClick.AddListener(OnQuitClicked);
        }

        private void OnDisable()
        {
            sandboxButton.onClick.RemoveListener(OnSandboxClicked);
            quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnSandboxClicked()
        {
            _sessionSettings.Mode = GameMode.Sandbox;
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
            sandboxButton.interactable = isInteractable;
            quitButton.interactable = isInteractable;
        }
    }
}
