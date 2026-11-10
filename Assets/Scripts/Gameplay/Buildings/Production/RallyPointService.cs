using System;
using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings.Production
{
    /// <summary>
    /// Where units made in a building go. A right click with the building selected sets it; every new unit then gets the
    /// same order as a right click there: walk, attack an enemy, gather a resource or build a site.
    /// </summary>
    public sealed class RallyPointService : IDisposable
    {
        private readonly BuildingService _buildings;
        private readonly UnitProductionService _production;
        private readonly UnitCommandService _commands;

        // Reused for the one unit each order is for.
        private readonly List<GameEntityId> _newUnit = new(1);

        public RallyPointService(BuildingService buildings, UnitProductionService production, UnitCommandService commands)
        {
            _buildings = buildings;
            _production = production;
            _commands = commands;
            _production.UnitProduced += OnUnitProduced;
        }

        public void Dispose()
        {
            _production.UnitProduced -= OnUnitProduced;
        }

        /// <summary>The player's finished buildings that make units can have one.</summary>
        public bool CanHaveRallyPoint(BuildingModel building)
        {
            return building.Faction == Faction.Player && building.IsBuilt && !building.IsDead &&
                   _production.GetProducibles(building.Definition).Count > 0;
        }

        /// <returns>False if the id isn't such a building.</returns>
        public bool TrySet(GameEntityId buildingId, Vector2 point)
        {
            if (!_buildings.TryGet(buildingId, out var building) || !CanHaveRallyPoint(building))
                return false;

            building.SetRallyPoint(point);
            return true;
        }

        private void OnUnitProduced(UnitModel unit, BuildingModel building)
        {
            if (!building.RallyPoint.HasValue)
                return;

            _newUnit.Clear();
            _newUnit.Add(unit.Id);
            _commands.CommandAt(_newUnit, building.RallyPoint.Value);
        }
    }
}
