using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Sandbox
{
    /// <summary>
    /// The sandbox tool column beside the production menu: battle AI and debug switches, a raid on demand,
    /// clearing the map and a test battle. Hidden in a normal game. The stats panel is its own view (StatsPanelView).
    /// </summary>
    public sealed class SandboxSwitchesView : MonoBehaviour
    {
        // A second click within this time clears the map. One stray click shouldn't wipe a setup.
        private const float ClearConfirmSeconds = 3f;

        [SerializeField] private Button battleAiButton;
        [SerializeField] private TMP_Text battleAiLabel;
        [SerializeField] private Button debugViewButton;
        [SerializeField] private TMP_Text debugViewLabel;

        [Header("Tools")]
        [SerializeField] private Button raidButton;
        [SerializeField] private TMP_Text raidLabel;
        [SerializeField] private Button clearMapButton;
        [SerializeField] private TMP_Text clearMapLabel;
        [SerializeField] private Button battleButton;
        [SerializeField] private TMP_Text battleLabel;
        [Tooltip("Soldiers on each side of the test battle.")]
        [SerializeField, Min(1)] private int soldiersPerSide = 50;

        private readonly CompositeDisposable _subscriptions = new();
        private SandboxService _sandbox;
        private RaidService _raids;
        private DeletionService _deletion;
        private SandboxBattleSpawner _battle;
        private CameraController _camera;
        private IEventBus _eventBus;

        private float _clearConfirmUntil = -1f;

        [Inject]
        private void Construct(SandboxService sandbox, RaidService raids, DeletionService deletion, SandboxBattleSpawner battle,
            CameraController cameraController, IEventBus eventBus)
        {
            _sandbox = sandbox;
            _raids = raids;
            _deletion = deletion;
            _battle = battle;
            _camera = cameraController;
            _eventBus = eventBus;
        }

        private void Start()
        {
            if (!_sandbox.IsEnabled)
            {
                battleAiButton.gameObject.SetActive(false);
                debugViewButton.gameObject.SetActive(false);
                raidButton.gameObject.SetActive(false);
                clearMapButton.gameObject.SetActive(false);
                battleButton.gameObject.SetActive(false);
                enabled = false;
                return;
            }

            battleAiButton.onClick.AddListener(_sandbox.ToggleBattleAi);
            debugViewButton.onClick.AddListener(_sandbox.ToggleDebugView);
            raidButton.onClick.AddListener(OnRaidClicked);
            clearMapButton.onClick.AddListener(OnClearMapClicked);
            battleButton.onClick.AddListener(OnBattleClicked);
            _eventBus.Subscribe<SandboxSwitchesChangedEvent>(_ => Refresh()).AddTo(_subscriptions);

            battleLabel.text = $"+{soldiersPerSide * 2} Units";
            clearMapLabel.text = "Clear Map";
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            battleAiButton.onClick.RemoveAllListeners();
            debugViewButton.onClick.RemoveAllListeners();
            raidButton.onClick.RemoveAllListeners();
            clearMapButton.onClick.RemoveAllListeners();
            battleButton.onClick.RemoveAllListeners();
        }

        private void Update()
        {
            raidButton.interactable = _raids.CanStartRaidNow;
            raidLabel.text = _raids.IsRaidActive ? _raids.Progress : "Start Raid";

            // Unscaled, so the confirmation runs out while the game is paused too.
            if (_clearConfirmUntil > 0f && Time.unscaledTime > _clearConfirmUntil)
            {
                _clearConfirmUntil = -1f;
                clearMapLabel.text = "Clear Map";
            }
        }

        private void Refresh()
        {
            battleAiLabel.text = _sandbox.IsBattleAiOn ? "Enemy AI: On" : "Enemy AI: Off";
            debugViewLabel.text = _sandbox.IsDebugViewOn ? "Debug: On" : "Debug: Off";
        }

        private void OnRaidClicked()
        {
            _raids.TryStartRaidNow();
        }

        private void OnClearMapClicked()
        {
            if (_clearConfirmUntil < 0f)
            {
                _clearConfirmUntil = Time.unscaledTime + ClearConfirmSeconds;
                clearMapLabel.text = "Sure? Click again";
                return;
            }

            _clearConfirmUntil = -1f;
            clearMapLabel.text = "Clear Map";
            _deletion.ClearMap();
        }

        private void OnBattleClicked()
        {
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            _battle.SpawnBattle(_camera.ScreenToWorld(screenCenter), soldiersPerSide);
        }
    }
}
