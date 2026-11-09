using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Territory;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Placement
{
    public enum PlacementBlock
    {
        None,

        /// <summary>The pointer isn't over the map, or it is on a panel.</summary>
        NoArea,

        OutsideMap,

        OutsideTerritory,

        /// <summary>A building or unwalkable terrain is in the way.</summary>
        Occupied,

        UnitInTheWay,

        NotEnoughResources
    }

    /// <summary>
    /// State of the building placement mode: which building is being placed and where its footprint is.
    /// Input moves and confirms it; the preview only reads it.
    /// </summary>
    public sealed class PlacementService
    {
        private readonly GridMap _gridMap;
        private readonly BuildingService _buildingService;
        private readonly UnitService _units;
        private readonly ResourceService _resources;
        private readonly TerritoryService _territory;
        private readonly IEventBus _eventBus;

        public PlacementService(GridMap gridMap, BuildingService buildingService, UnitService units, ResourceService resources,
            TerritoryService territory, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _buildingService = buildingService;
            _units = units;
            _resources = resources;
            _territory = territory;
            _eventBus = eventBus;
        }

        public bool IsActive => Definition != null;
        public BuildingDefinition Definition { get; private set; }
        public Faction Faction { get; private set; }

        /// <summary>False until the pointer is over the map, and while it is over a UI panel.</summary>
        public bool HasArea { get; private set; }

        public RectInt Area { get; private set; }

        public bool IsAreaValid => Block == PlacementBlock.None;

        /// <summary>
        /// The building being placed opens territory and could stand here as far as territory goes,
        /// so the player should see what it would open. Price and obstacles don't matter for that.
        /// </summary>
        public bool ShowsTerritoryPreview => HasArea && Definition.TerritoryRadius > 0f && _territory.IsInside(Area);

        /// <summary>Why the building can't go where the footprint is now.</summary>
        public PlacementBlock Block
        {
            get
            {
                if (!HasArea)
                    return PlacementBlock.NoArea;

                if (!_gridMap.IsInBounds(Area))
                    return PlacementBlock.OutsideMap;

                if (!_territory.IsInside(Area))
                    return PlacementBlock.OutsideTerritory;

                if (!_gridMap.IsAreaFree(Area))
                    return PlacementBlock.Occupied;

                // Units don't block the grid, but a building placed on one would trap it.
                if (_units.HasUnitIn(Area))
                    return PlacementBlock.UnitInTheWay;

                return _resources.CanAfford(Definition.Cost) ? PlacementBlock.None : PlacementBlock.NotEnoughResources;
            }
        }

        /// <summary>True if this cell of the footprint doesn't stop the building from being placed.</summary>
        public bool IsCellFree(Vector2Int cell)
        {
            // On uneven ground the footprint's center decides which level is the right one.
            var center = new Vector2Int(Area.x + Area.width / 2, Area.y + Area.height / 2);
            return _gridMap.IsWalkable(cell) && !_gridMap.IsRamp(cell) && _territory.IsInside(cell) &&
                   _gridMap.GetLevel(cell) == _gridMap.GetLevel(center) && !_units.HasUnitAt(cell);
        }

        /// <summary>Starts placing the building. Calling it while active swaps the building, no cancel needed.</summary>
        public void Begin(BuildingDefinition definition, Faction faction)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Definition = definition;
            Faction = faction;
            HasArea = false;
            _eventBus.Publish(new PlacementStartedEvent(definition, faction));
        }

        /// <summary>Centers the footprint on the world position.</summary>
        public void MoveTo(Vector2 worldPosition)
        {
            if (!IsActive)
                return;

            Area = _gridMap.GetAreaCenteredAt(worldPosition, Definition.Size);
            HasArea = true;
        }

        public void HideArea()
        {
            HasArea = false;
        }

        /// <param name="keepPlacing">Stay in placement mode with the same building, for placing several in a row.</param>
        /// <returns>False if the footprint is hidden or not free; the mode stays active so the player can try elsewhere.</returns>
        public bool TryConfirm(bool keepPlacing)
        {
            if (!IsAreaValid || !_buildingService.TryPlace(Definition, Area.position, Faction, out _))
                return false;

            _resources.TrySpend(Definition.Cost);

            if (!keepPlacing)
                End();

            return true;
        }

        public void Cancel()
        {
            if (IsActive)
                End();
        }

        private void End()
        {
            Definition = null;
            HasArea = false;
            _eventBus.Publish(new PlacementEndedEvent());
        }
    }
}
