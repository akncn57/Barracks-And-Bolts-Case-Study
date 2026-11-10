using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.ResourceNodes;
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
    /// State of the placement mode: which building, unit or resource is being placed and where its footprint is.
    /// Input moves and confirms it; the preview only reads it. Placing units and resources straight onto the map is
    /// a sandbox tool; a normal game makes units in buildings.
    /// </summary>
    public sealed class PlacementService
    {
        private readonly GridMap _gridMap;
        private readonly BuildingService _buildingService;
        private readonly UnitService _units;
        private readonly ResourceNodeService _nodes;
        private readonly ResourceService _resources;
        private readonly TerritoryService _territory;
        private readonly IEventBus _eventBus;

        public PlacementService(GridMap gridMap, BuildingService buildingService, UnitService units, ResourceNodeService nodes,
            ResourceService resources, TerritoryService territory, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _buildingService = buildingService;
            _units = units;
            _nodes = nodes;
            _resources = resources;
            _territory = territory;
            _eventBus = eventBus;
        }

        public bool IsActive => Definition != null || Unit != null || Node != null;

        /// <summary>The building being placed. Null while a unit or a resource is placed.</summary>
        public BuildingDefinition Definition { get; private set; }

        /// <summary>The unit being placed. Null otherwise.</summary>
        public UnitDefinition Unit { get; private set; }

        /// <summary>The resource being placed: a tree, rock, mine or sheep. Null otherwise.</summary>
        public ResourceNodeDefinition Node { get; private set; }

        public bool IsPlacingUnit => Unit != null;
        public bool IsPlacingNode => Node != null;
        public Faction Faction { get; private set; }

        /// <summary>False until the pointer is over the map, and while it is over a UI panel.</summary>
        public bool HasArea { get; private set; }

        public RectInt Area { get; private set; }

        public bool IsAreaValid => Block == PlacementBlock.None;

        /// <summary>
        /// The building being placed opens territory and could stand here as far as territory goes,
        /// so the player should see what it would open. Price and obstacles don't matter for that.
        /// </summary>
        public bool ShowsTerritoryPreview => HasArea && Definition != null && Definition.TerritoryRadius > 0f && _territory.IsInside(Area);

        /// <summary>Why the building can't go where the footprint is now.</summary>
        public PlacementBlock Block
        {
            get
            {
                if (!HasArea)
                    return PlacementBlock.NoArea;

                if (!_gridMap.IsInBounds(Area))
                    return PlacementBlock.OutsideMap;

                // A unit needs one walkable cell. Other units there are fine: it steps to the nearest free cell.
                if (IsPlacingUnit)
                {
                    if (!_gridMap.IsWalkable(Area.position))
                        return PlacementBlock.Occupied;

                    return _resources.CanAfford(Unit.Cost) ? PlacementBlock.None : PlacementBlock.NotEnoughResources;
                }

                if (!_territory.IsInside(Area))
                    return PlacementBlock.OutsideTerritory;

                if (!_gridMap.IsAreaFree(Area))
                    return PlacementBlock.Occupied;

                // Units don't block the grid, but a building or resource placed on one would trap it.
                if (_units.HasUnitIn(Area))
                    return PlacementBlock.UnitInTheWay;

                // Resources are free; only the sandbox places them.
                return IsPlacingNode || _resources.CanAfford(Definition.Cost) ? PlacementBlock.None : PlacementBlock.NotEnoughResources;
            }
        }

        /// <summary>True if this cell of the footprint doesn't stop the building from being placed.</summary>
        public bool IsCellFree(Vector2Int cell)
        {
            if (IsPlacingUnit)
                return _gridMap.IsWalkable(cell);

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
            Unit = null;
            Node = null;
            Faction = faction;
            HasArea = false;
            _eventBus.Publish(new PlacementStartedEvent(definition, faction));
        }

        /// <summary>Starts placing a unit straight onto the map. Calling it while active swaps what is placed.</summary>
        public void BeginUnit(UnitDefinition unit, Faction faction)
        {
            if (unit == null)
                throw new ArgumentNullException(nameof(unit));

            Definition = null;
            Unit = unit;
            Node = null;
            Faction = faction;
            HasArea = false;
            _eventBus.Publish(new PlacementStartedEvent(null, faction, unit));
        }

        /// <summary>Starts placing a resource. Resources belong to nobody, so there is no side.</summary>
        public void BeginNode(ResourceNodeDefinition node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Definition = null;
            Unit = null;
            Node = node;
            Faction = Faction.Player;
            HasArea = false;
            _eventBus.Publish(new PlacementStartedEvent(null, Faction.Player));
        }

        /// <summary>Centers the footprint on the world position.</summary>
        public void MoveTo(Vector2 worldPosition)
        {
            if (!IsActive)
                return;

            Area = _gridMap.GetAreaCenteredAt(worldPosition, GetSize());
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
            if (IsPlacingUnit)
                return TryConfirmUnit(keepPlacing);

            if (IsPlacingNode)
                return TryConfirmNode(keepPlacing);

            // A normal game builds over time: the building starts as a site for villagers. Sandbox mode builds at once.
            var asSite = !_resources.IsUnlimited && Definition.BuildTime > 0f;
            if (!IsAreaValid || !_buildingService.TryPlace(Definition, Area.position, Faction, out _, asSite))
                return false;

            _resources.TrySpend(Definition.Cost);

            if (!keepPlacing)
                End();

            return true;
        }

        private bool TryConfirmUnit(bool keepPlacing)
        {
            if (!IsAreaValid || !_units.TrySpawn(Unit, Area.position, Faction, out _))
                return false;

            _resources.TrySpend(Unit.Cost);

            if (!keepPlacing)
                End();

            return true;
        }

        private bool TryConfirmNode(bool keepPlacing)
        {
            if (!IsAreaValid || !_nodes.TryPlace(Node, Area.position, out _))
                return false;

            if (!keepPlacing)
                End();

            return true;
        }

        private Vector2Int GetSize()
        {
            if (IsPlacingUnit)
                return Vector2Int.one;

            return IsPlacingNode ? Node.Size : Definition.Size;
        }

        public void Cancel()
        {
            if (IsActive)
                End();
        }

        private void End()
        {
            Definition = null;
            Unit = null;
            Node = null;
            HasArea = false;
            _eventBus.Publish(new PlacementEndedEvent());
        }
    }
}
