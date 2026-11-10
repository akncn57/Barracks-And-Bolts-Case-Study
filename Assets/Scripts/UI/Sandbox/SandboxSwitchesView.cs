using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Sandbox
{
    /// <summary>
    /// The sandbox tool column beside the production menu: battle AI and debug switches, a raid on demand,
    /// clearing the map, a test battle and a small stats panel. Hidden in a normal game.
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

        [Header("Stats")]
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField, Min(0.1f)] private float statsInterval = 0.5f;

        private readonly CompositeDisposable _subscriptions = new();
        private SandboxService _sandbox;
        private RaidService _raids;
        private DeletionService _deletion;
        private SandboxBattleSpawner _battle;
        private UnitService _units;
        private BuildingService _buildings;
        private AStarPathfinder _pathfinder;
        private CameraController _camera;
        private IEventBus _eventBus;

        private float _clearConfirmUntil = -1f;
        private int _statsFrames;
        private float _statsTime;

        // Draw state counters exist only in the editor and development builds; elsewhere the line is left out.
        private ProfilerRecorder _setPassRecorder;
        private ProfilerRecorder _drawCallRecorder;

        [Inject]
        private void Construct(SandboxService sandbox, RaidService raids, DeletionService deletion, SandboxBattleSpawner battle,
            UnitService units, BuildingService buildings, AStarPathfinder pathfinder, CameraController cameraController, IEventBus eventBus)
        {
            _sandbox = sandbox;
            _raids = raids;
            _deletion = deletion;
            _battle = battle;
            _units = units;
            _buildings = buildings;
            _pathfinder = pathfinder;
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
                statsPanel.SetActive(false);
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
            _setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _drawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            _setPassRecorder.Dispose();
            _drawCallRecorder.Dispose();
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

            UpdateStats();
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

        private void UpdateStats()
        {
            _statsFrames++;
            _statsTime += Time.unscaledDeltaTime;
            if (_statsTime < statsInterval)
                return;

            _pathfinder.TakeStats(out var searches, out var totalMilliseconds, out var longestMilliseconds);
            var fps = _statsFrames / _statsTime;
            var pathsPerSecond = searches / _statsTime;
            var averageMilliseconds = searches > 0 ? totalMilliseconds / searches : 0.0;

            var text = FormattableString.Invariant(
                $"FPS {fps:0}\nUnits {_units.Units.Count} | Buildings {_buildings.Buildings.Count}\nPaths {pathsPerSecond:0}/s\navg {averageMilliseconds:0.00} ms | max {longestMilliseconds:0.00} ms");
            if (_setPassRecorder.Valid && _setPassRecorder.LastValue > 0)
                text += FormattableString.Invariant($"\nSetPass {_setPassRecorder.LastValue} | Draws {_drawCallRecorder.LastValue}");

            statsLabel.text = text;
            _statsFrames = 0;
            _statsTime = 0f;
        }
    }
}
