using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using Random = System.Random;

namespace BarracksAndBolts.Gameplay.Raids
{
    /// <summary>
    /// Runs the raids: warns in the morning, starts the raid later that day and sends its waves one after another.
    /// The next wave comes when the last one is dead. The calendar stands still until the whole raid is beaten.
    /// In sandbox mode there is no calendar: the tester starts the raids by hand, one after another from the list.
    /// </summary>
    public sealed class RaidService : ISimulationSystem, IDisposable
    {
        private static readonly string[] Directions = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };
        private static readonly Vector2Int[] Neighbours = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        private readonly RaidSettings _settings;
        private readonly NormalModeSettings _normalMode;
        private readonly CalendarService _calendar;
        private readonly UnitService _units;
        private readonly BuildingService _buildings;
        private readonly GridSearch _search;
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;
        private readonly Random _random;
        private readonly SandboxService _sandbox;
        private readonly bool _isEnabled;
        private readonly CompositeDisposable _subscriptions = new();

        // The units of the wave on the map. Enemies the sandbox tester placed don't hold the next wave back.
        private readonly List<GameEntityId> _waveUnits = new();

        private RaidSettings.Raid _waitingRaid;
        private float _startCountdown;
        private RaidSettings.Raid _activeRaid;
        private int _nextWave;
        private float _waveDelayLeft;
        private int _nextSandboxRaid;

        public RaidService(RaidSettings settings, NormalModeSettings normalMode, CalendarService calendar, UnitService units, BuildingService buildings,
            GridSearch search, GridMap gridMap, SandboxService sandbox, GameSessionSettings session, IEventBus eventBus)
        {
            _settings = settings;
            _normalMode = normalMode;
            _calendar = calendar;
            _units = units;
            _buildings = buildings;
            _search = search;
            _gridMap = gridMap;
            _eventBus = eventBus;
            _random = new Random(session.Seed);
            _sandbox = sandbox;
            _isEnabled = !sandbox.IsEnabled;

            if (_isEnabled)
                eventBus.Subscribe<DayStartedEvent>(OnDayStarted).AddTo(_subscriptions);
            else
                eventBus.Subscribe<SandboxMapClearedEvent>(_ => StopSandboxRaid()).AddTo(_subscriptions);
        }

        public SimulationPhase Phase => SimulationPhase.AI;

        public bool IsRaidActive => _activeRaid != null;

        /// <summary>Sandbox mode only, and one raid at a time.</summary>
        public bool CanStartRaidNow => _sandbox.IsEnabled && _activeRaid == null && _settings.Raids.Count > 0;

        /// <summary>"Raid 2: wave 1 of 3" while a raid runs. Empty otherwise.</summary>
        public string Progress => _activeRaid == null ? string.Empty : $"Raid {_activeRaid.Day}: wave {Mathf.Max(_nextWave, 1)} of {_activeRaid.Waves.Count}";

        /// <summary>
        /// Sandbox mode: sends the next raid from the list right away and turns the battle AI on, so the raiders march.
        /// After the last raid it starts over from the first.
        /// </summary>
        /// <returns>False outside sandbox mode, while a raid runs, or with no raids set up.</returns>
        public bool TryStartRaidNow()
        {
            if (!CanStartRaidNow)
                return false;

            _activeRaid = _settings.Raids[_nextSandboxRaid % _settings.Raids.Count];
            _nextSandboxRaid++;
            _nextWave = 0;
            _waveDelayLeft = 0f;
            _waveUnits.Clear();
            _sandbox.TurnBattleAiOn();
            return true;
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        public void Tick(float deltaTime)
        {
            if (_activeRaid == null)
            {
                if (_isEnabled)
                    UpdateCountdown(deltaTime);

                return;
            }

            // Wait until this wave is dead.
            if (CountWaveUnitsAlive() > 0)
                return;

            if (_nextWave >= _activeRaid.Waves.Count)
            {
                EndRaid();
                return;
            }

            _waveDelayLeft -= deltaTime;
            if (_waveDelayLeft <= 0f)
                SendNextWave();
        }

        private void OnDayStarted(DayStartedEvent startedEvent)
        {
            _waitingRaid = _settings.GetRaid(startedEvent.Day);
            if (_waitingRaid == null)
                return;

            var point = _settings.EarliestStart + (float)_random.NextDouble() * (_settings.LatestStart - _settings.EarliestStart);
            _startCountdown = point * _settings.DayLength;
            _eventBus.Publish(new GameNoticeEvent("Today is a raid day!", isAlert: false));
        }

        private void UpdateCountdown(float deltaTime)
        {
            if (_waitingRaid == null)
                return;

            _startCountdown -= deltaTime;
            if (_startCountdown > 0f)
                return;

            _activeRaid = _waitingRaid;
            _waitingRaid = null;
            _nextWave = 0;
            _waveDelayLeft = 0f;
            _waveUnits.Clear();
            _calendar.Freeze();
        }

        private void SendNextWave()
        {
            var spawnCell = PickSpawnCell();
            var groups = _activeRaid.Waves[_nextWave].Groups;
            _waveUnits.Clear();
            foreach (var group in groups)
            {
                for (var i = 0; i < group.Count; i++)
                {
                    if (_units.TrySpawn(group.Unit, spawnCell, Faction.Enemy, out var raider))
                        _waveUnits.Add(raider.Id);
                }
            }

            _nextWave++;
            _waveDelayLeft = _settings.WaveDelay;
            _eventBus.Publish(new GameNoticeEvent($"Raid! Wave {_nextWave} of {_activeRaid.Waves.Count} from the {GetDirectionName(spawnCell)}", isAlert: true));
        }

        private void EndRaid()
        {
            var isLastRaid = _activeRaid.Day >= _settings.LastDay;
            _activeRaid = null;
            _eventBus.Publish(new GameNoticeEvent("Raid beaten!", isAlert: false));

            // A sandbox raid has no calendar to wake and no game to win.
            if (!_isEnabled)
                return;

            _calendar.Resume();
            _eventBus.Publish(new RaidEndedEvent(isLastRaid));
        }

        private void StopSandboxRaid()
        {
            _activeRaid = null;
            _waveUnits.Clear();
        }

        private int CountWaveUnitsAlive()
        {
            var count = 0;
            foreach (var id in _waveUnits)
            {
                if (_units.TryGet(id, out var unit) && !unit.IsDead)
                    count++;
            }

            return count;
        }

        /// <summary>A random coast cell that can walk to the Castle and is far from it.</summary>
        private Vector2Int PickSpawnCell()
        {
            var home = GetHomeCell();
            var reachable = CollectReachable(home);

            var candidates = new List<Vector2Int>();
            foreach (var cell in reachable)
            {
                if (IsCoast(cell) && Vector2Int.Distance(cell, home) >= _settings.MinSpawnDistance)
                    candidates.Add(cell);
            }

            if (candidates.Count > 0)
                return candidates[_random.Next(candidates.Count)];

            // A very small map, or one the player walled off: the farthest cell will do.
            var farthest = home;
            foreach (var cell in reachable)
            {
                if ((cell - home).sqrMagnitude > (farthest - home).sqrMagnitude)
                    farthest = cell;
            }

            return farthest;
        }

        private Vector2Int GetHomeCell()
        {
            foreach (var building in _buildings.Buildings)
            {
                if (building.Definition == _normalMode.Castle && building.Faction == Faction.Player &&
                    _search.TryFindCellBeside(building.Area, _gridMap.CellToWorldCenter(building.Area.position), out var cell))
                    return cell;
            }

            var center = new Vector2Int(_gridMap.Width / 2, _gridMap.Height / 2);
            return _search.TryFindNearestWalkable(center, out var nearest) ? nearest : center;
        }

        private HashSet<Vector2Int> CollectReachable(Vector2Int start)
        {
            var seen = new HashSet<Vector2Int> { start };
            var open = new Queue<Vector2Int>();
            open.Enqueue(start);
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var offset in Neighbours)
                {
                    var next = cell + offset;
                    if (_gridMap.IsWalkable(next) && _gridMap.CanStepBetween(cell, next) && seen.Add(next))
                        open.Enqueue(next);
                }
            }

            return seen;
        }

        private bool IsCoast(Vector2Int cell)
        {
            if (_gridMap.IsRamp(cell) || _gridMap.GetLevel(cell) != 0)
                return false;

            foreach (var offset in Neighbours)
            {
                if (!_gridMap.IsTerrainWalkable(cell + offset))
                    return true;
            }

            return false;
        }

        private string GetDirectionName(Vector2Int cell)
        {
            var toSpawn = (Vector2)(cell - GetHomeCell());
            var angle = Mathf.Atan2(toSpawn.y, toSpawn.x) * Mathf.Rad2Deg;
            var index = Mathf.RoundToInt(Mathf.Repeat(angle, 360f) / 45f) % Directions.Length;
            return Directions[index];
        }
    }

    public readonly struct RaidEndedEvent : IGameEvent
    {
        public RaidEndedEvent(bool isLastRaid)
        {
            IsLastRaid = isLastRaid;
        }

        public bool IsLastRaid { get; }
    }

    /// <summary>A short message for the whole screen, like a coming raid.</summary>
    public readonly struct GameNoticeEvent : IGameEvent
    {
        public GameNoticeEvent(string text, bool isAlert)
        {
            Text = text;
            IsAlert = isAlert;
        }

        public string Text { get; }
        public bool IsAlert { get; }
    }
}
