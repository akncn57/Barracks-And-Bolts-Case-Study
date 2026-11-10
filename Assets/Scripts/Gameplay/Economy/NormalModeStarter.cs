using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>Sets up the player's base when a normal game starts, with more or less to start with by difficulty. Sandbox games start empty.</summary>
    public sealed class NormalModeStarter : IStartable
    {
        private readonly SandboxService _sandbox;
        private readonly NormalModeSettings _settings;
        private readonly ResourceService _resources;
        private readonly BuildingService _buildings;
        private readonly UnitService _units;
        private readonly GridMap _gridMap;
        private readonly GameSessionSettings _session;

        public NormalModeStarter(SandboxService sandbox, NormalModeSettings settings, ResourceService resources,
            BuildingService buildings, UnitService units, GridMap gridMap, GameSessionSettings session)
        {
            _session = session;
            _sandbox = sandbox;
            _settings = settings;
            _resources = resources;
            _buildings = buildings;
            _units = units;
            _gridMap = gridMap;
        }

        public void Start()
        {
            if (_sandbox.IsEnabled)
                return;

            foreach (var part in _settings.StartingResources)
                _resources.Add(part.Kind, _settings.ScaleStartingAmount(part.Amount, _session.Difficulty));

            var castle = _settings.Castle;
            var origin = new Vector2Int((_gridMap.Width - castle.Size.x) / 2, (_gridMap.Height - castle.Size.y) / 2);
            if (!_buildings.TryPlace(castle, origin, Faction.Player, out _))
            {
                Debug.LogError($"The Castle doesn't fit at {origin}. The middle of the map must be free.");
                return;
            }

            var spawnCell = origin + castle.SpawnOffset;
            for (var i = 0; i < _settings.StartingVillagers; i++)
                _units.TrySpawn(_settings.Villager, spawnCell, Faction.Player, out _);
        }
    }
}
