using System;
using BarracksAndBolts.Core.Settings;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Units;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>
    /// The small panel in the top right corner: FPS, unit and building counts and how long A* searches take.
    /// Shown while the FPS Panel setting is on, in both modes.
    /// </summary>
    public sealed class StatsPanelView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0.1f)] private float interval = 0.5f;

        private GameSettings _settings;
        private UnitService _units;
        private BuildingService _buildings;
        private AStarPathfinder _pathfinder;
        private int _frames;
        private float _time;

        // Draw state counters exist only in the editor and development builds; elsewhere the line is left out.
        private ProfilerRecorder _setPassRecorder;
        private ProfilerRecorder _drawCallRecorder;

        [Inject]
        private void Construct(GameSettings settings, UnitService units, BuildingService buildings, AStarPathfinder pathfinder)
        {
            _settings = settings;
            _units = units;
            _buildings = buildings;
            _pathfinder = pathfinder;
        }

        private void Start()
        {
            _setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            _drawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            _settings.Changed += ApplyVisibility;
            ApplyVisibility();
        }

        private void OnDestroy()
        {
            _setPassRecorder.Dispose();
            _drawCallRecorder.Dispose();
            if (_settings != null)
                _settings.Changed -= ApplyVisibility;
        }

        private void Update()
        {
            _frames++;
            _time += Time.unscaledDeltaTime;
            if (_time < interval)
                return;

            // Read even while hidden, so the numbers start fresh when it is shown again.
            _pathfinder.TakeStats(out var searches, out var totalMilliseconds, out var longestMilliseconds);
            if (label.enabled)
                label.text = Format(searches, totalMilliseconds, longestMilliseconds);

            _frames = 0;
            _time = 0f;
        }

        private string Format(int searches, double totalMilliseconds, double longestMilliseconds)
        {
            var fps = _frames / _time;
            var average = searches > 0 ? totalMilliseconds / searches : 0.0;
            var text = FormattableString.Invariant(
                $"FPS {fps:0}\nUnits {_units.Units.Count} | Buildings {_buildings.Buildings.Count}\nPaths {searches / _time:0}/s\navg {average:0.00} ms | max {longestMilliseconds:0.00} ms");
            if (_setPassRecorder.Valid && _setPassRecorder.LastValue > 0)
                text += FormattableString.Invariant($"\nSetPass {_setPassRecorder.LastValue} | Draws {_drawCallRecorder.LastValue}");

            return text;
        }

        // The components are switched, not the object, so Update keeps running and can show it again.
        private void ApplyVisibility()
        {
            background.enabled = _settings.IsStatsShown;
            label.enabled = _settings.IsStatsShown;
        }
    }
}
