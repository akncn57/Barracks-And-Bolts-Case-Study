using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Placement
{
    /// <summary>
    /// State of the building placement mode: which building is being placed and where its footprint is.
    /// Input moves and confirms it; the preview only reads it.
    /// </summary>
    public sealed class PlacementService
    {
        private readonly GridMap _gridMap;
        private readonly BuildingService _buildingService;
        private readonly IEventBus _eventBus;

        public PlacementService(GridMap gridMap, BuildingService buildingService, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _buildingService = buildingService;
            _eventBus = eventBus;
        }

        public bool IsActive => Definition != null;
        public BuildingDefinition Definition { get; private set; }
        public Faction Faction { get; private set; }

        /// <summary>False until the pointer is over the map, and while it is over a UI panel.</summary>
        public bool HasArea { get; private set; }

        public RectInt Area { get; private set; }
        public bool IsAreaValid => HasArea && _gridMap.IsAreaFree(Area);

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
