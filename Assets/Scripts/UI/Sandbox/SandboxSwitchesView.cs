using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Sandbox
{
    /// <summary>Sandbox buttons that switch the battle AI and the debug view on and off. Hidden in a normal game.</summary>
    public sealed class SandboxSwitchesView : MonoBehaviour
    {
        [SerializeField] private Button battleAiButton;
        [SerializeField] private TMP_Text battleAiLabel;
        [SerializeField] private Button debugViewButton;
        [SerializeField] private TMP_Text debugViewLabel;

        private readonly CompositeDisposable _subscriptions = new();
        private SandboxService _sandbox;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(SandboxService sandbox, IEventBus eventBus)
        {
            _sandbox = sandbox;
            _eventBus = eventBus;
        }

        private void Start()
        {
            if (!_sandbox.IsEnabled)
            {
                battleAiButton.gameObject.SetActive(false);
                debugViewButton.gameObject.SetActive(false);
                return;
            }

            battleAiButton.onClick.AddListener(_sandbox.ToggleBattleAi);
            debugViewButton.onClick.AddListener(_sandbox.ToggleDebugView);
            _eventBus.Subscribe<SandboxSwitchesChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            battleAiButton.onClick.RemoveAllListeners();
            debugViewButton.onClick.RemoveAllListeners();
        }

        private void Refresh()
        {
            battleAiLabel.text = _sandbox.IsBattleAiOn ? "Enemy AI: On" : "Enemy AI: Off";
            debugViewLabel.text = _sandbox.IsDebugViewOn ? "Debug: On" : "Debug: Off";
        }
    }
}
