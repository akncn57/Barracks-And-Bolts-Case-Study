using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Scenes;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Utils.Disposables;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>The screen shown when a normal game is won or lost.</summary>
    public sealed class GameEndView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible screen. Hidden until the game ends; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button menuButton;

        private readonly CompositeDisposable _subscriptions = new();
        private SceneLoader _sceneLoader;
        private GameSessionSettings _session;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(SceneLoader sceneLoader, GameSessionSettings session, IEventBus eventBus)
        {
            _sceneLoader = sceneLoader;
            _session = session;
            _eventBus = eventBus;
        }

        private void Start()
        {
            content.SetActive(false);
            playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
            _eventBus.Subscribe<GameEndedEvent>(Show).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            menuButton.onClick.RemoveListener(OnMenuClicked);
            _subscriptions.Dispose();
        }

        private void Show(GameEndedEvent endedEvent)
        {
            titleLabel.text = endedEvent.IsVictory ? "Victory" : "Defeat";
            messageLabel.text = endedEvent.Message;
            content.SetActive(true);
        }

        private void OnPlayAgainClicked()
        {
            _session.Seed = Environment.TickCount;
            Leave(SceneNames.Gameplay);
        }

        private void OnMenuClicked()
        {
            Leave(SceneNames.MainMenu);
        }

        private void Leave(string scene)
        {
            playAgainButton.interactable = false;
            menuButton.interactable = false;
            _sceneLoader.LoadAsync(scene).Forget();
        }
    }
}
