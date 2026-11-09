using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Gathering
{
    /// <summary>
    /// Runs the villager's loop: walk to the node, work, carry the load to the nearest drop-off, walk back.
    /// When a node runs out the villager moves on to a nearby one of the same kind, or stops.
    /// </summary>
    public sealed class GatheringSystem : ISimulationSystem
    {
        // The working place stays inside its own cell, so the way back starts from a walkable one.
        private const float CellEdgeMargin = 0.01f;

        // Added to the walking distance when choosing where to stand. The swing goes sideways, so beside the node
        // looks right. Above it the node's sprite would hide the villager.
        private const float BelowPenalty = 3f;
        private const float AbovePenalty = 6f;
        private const float CornerPenalty = 9f;
        private const float TakenPenalty = 20f;

        private enum Stage
        {
            WalkingToNode,
            Harvesting,
            WalkingToDropOff
        }

        private sealed class Order
        {
            public UnitModel Unit;
            public ResourceKind Kind;
            public ResourceNodeModel Node;
            public GameEntityId DropOff;
            public Vector2Int Spot;
            public Stage Stage;
            public bool HasSteppedIn;
            public float WorkLeft;
            public int Carried;
        }

        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly ResourceNodeService _nodes;
        private readonly BuildingService _buildings;
        private readonly ResourceService _resources;
        private readonly GridSearch _search;
        private readonly GridMap _gridMap;
        private readonly NormalModeSettings _settings;
        private readonly IEventBus _eventBus;
        private readonly List<Order> _orders = new();
        private readonly List<Order> _suspended = new();

        public GatheringSystem(UnitService units, UnitMovementSystem movement, ResourceNodeService nodes, BuildingService buildings,
            ResourceService resources, GridSearch search, GridMap gridMap, NormalModeSettings settings, IEventBus eventBus)
        {
            _units = units;
            _movement = movement;
            _nodes = nodes;
            _buildings = buildings;
            _resources = resources;
            _search = search;
            _gridMap = gridMap;
            _settings = settings;
            _eventBus = eventBus;
        }

        public SimulationPhase Phase => SimulationPhase.Gathering;

        /// <summary>Sends the unit to the node, or to the nearest node of that kind if nothing can reach this one.</summary>
        /// <returns>False if the unit can't gather or there is nowhere to stand.</returns>
        public bool TryOrder(UnitModel unit, ResourceNodeModel node)
        {
            if (!unit.Definition.CanGather)
                return false;

            Cancel(unit.Id);
            var order = new Order { Unit = unit, Kind = node.Definition.Kind };
            if (!TryWalkToNode(order, node) && !TryWalkToNode(order, _nodes.FindNearest(order.Kind, unit.Position, node)))
                return false;

            unit.ClearAttackTarget();
            unit.SetOnJob(true);
            _orders.Add(order);
            return true;
        }

        /// <summary>Takes the unit out of gathering. Whatever it was carrying is lost.</summary>
        public void Cancel(GameEntityId unitId)
        {
            for (var i = _orders.Count - 1; i >= 0; i--)
            {
                if (_orders[i].Unit.Id == unitId)
                    End(i);
            }

            TakeSuspended(unitId);
        }

        /// <summary>Stops the work but remembers it for <see cref="Resume"/>. For a villager running from danger.</summary>
        public void Suspend(GameEntityId unitId)
        {
            for (var i = _orders.Count - 1; i >= 0; i--)
            {
                if (_orders[i].Unit.Id != unitId)
                    continue;

                _suspended.Add(_orders[i]);
                End(i);
            }
        }

        /// <summary>A suspended villager has reached a drop-off: what it carries counts now.</summary>
        public void DeliverLoad(GameEntityId unitId)
        {
            foreach (var order in _suspended)
            {
                if (order.Unit.Id == unitId)
                    Deliver(order);
            }
        }

        /// <summary>Sends the unit back to its suspended work, or to the nearest node of that kind if its own is gone.</summary>
        /// <returns>False if it had no work or there is nowhere to go.</returns>
        public bool Resume(UnitModel unit)
        {
            var order = TakeSuspended(unit.Id);
            if (order == null)
                return false;

            var node = order.Node.IsDepleted ? _nodes.FindNearest(order.Kind, unit.Position, order.Node) : order.Node;
            if (!TryWalkToNode(order, node))
                return false;

            unit.SetOnJob(true);
            _orders.Add(order);
            return true;
        }

        public bool IsGathering(GameEntityId unitId)
        {
            foreach (var order in _orders)
            {
                if (order.Unit.Id == unitId)
                    return true;
            }

            return false;
        }

        public void Tick(float deltaTime)
        {
            // Backwards, so an order can end during its own update.
            for (var i = _orders.Count - 1; i >= 0; i--)
            {
                var order = _orders[i];
                if (!_units.TryGet(order.Unit.Id, out _) || order.Unit.HasAttackTarget || !Advance(order, deltaTime))
                    End(i);
            }
        }

        /// <returns>False if the order is over.</returns>
        private bool Advance(Order order, float deltaTime)
        {
            switch (order.Stage)
            {
                case Stage.WalkingToNode:
                    return UpdateWalkToNode(order);
                case Stage.Harvesting:
                    return UpdateHarvest(order, deltaTime);
                default:
                    return UpdateWalkToDropOff(order);
            }
        }

        private bool UpdateWalkToNode(Order order)
        {
            if (order.Node.IsDepleted)
                return TryMoveToNextNode(order);

            if (order.HasSteppedIn)
                order.Node.Hold();

            if (order.Unit.IsMoving)
                return true;

            // Something got in the way and the unit stopped short.
            if (_gridMap.WorldToCell(order.Unit.Position) != order.Spot)
                return false;

            // The path ends on the middle of the cell. The last step goes right up to the node.
            if (!order.HasSteppedIn)
            {
                order.HasSteppedIn = true;
                order.Node.Hold();
                _movement.StepTo(order.Unit, GetWorkPoint(order));
                return true;
            }

            order.Stage = Stage.Harvesting;
            order.WorkLeft = order.Node.Definition.IsOneCut ? _settings.OneCutDuration : _settings.HarvestDuration;
            order.Unit.FaceTowards(GetNodeBase(order.Node));
            order.Unit.SetHarvesting(order.Kind);
            return true;
        }

        private bool UpdateHarvest(Order order, float deltaTime)
        {
            if (order.Node.IsDepleted)
            {
                order.Unit.SetHarvesting(null);
                return TryMoveToNextNode(order);
            }

            order.Node.Hold();
            order.WorkLeft -= deltaTime;
            if (order.WorkLeft > 0f)
                return true;

            order.Unit.SetHarvesting(null);
            order.Carried = order.Node.Take(order.Node.Definition.IsOneCut ? order.Node.Remaining : _settings.CarryCapacity);
            if (order.Node.IsDepleted)
                _nodes.Remove(order.Node);

            return TryWalkToDropOff(order);
        }

        private bool UpdateWalkToDropOff(Order order)
        {
            if (order.Unit.IsMoving)
                return true;

            // The drop-off was destroyed on the way, or something got in the way: look again.
            if (!_buildings.TryGet(order.DropOff, out _) || _gridMap.WorldToCell(order.Unit.Position) != order.Spot)
                return TryWalkToDropOff(order);

            Deliver(order);
            return order.Node.IsDepleted ? TryMoveToNextNode(order) : TryWalkToNode(order, order.Node);
        }

        private bool TryMoveToNextNode(Order order)
        {
            var next = _nodes.FindNearest(order.Kind, order.Unit.Position, order.Node);
            var isCloseEnough = next != null &&
                                Vector2.Distance(order.Unit.Position, next.Area.center + _gridMap.Origin) <= _settings.NextNodeRadius;
            return isCloseEnough && TryWalkToNode(order, next);
        }

        private bool TryWalkToNode(Order order, ResourceNodeModel node)
        {
            if (node == null || !TryPickWorkCell(order, node, out var spot))
                return false;

            order.Node = node;
            order.Spot = spot;
            order.Stage = Stage.WalkingToNode;
            order.HasSteppedIn = false;
            _movement.SendTo(order.Unit, spot);
            return true;
        }

        private void Deliver(Order order)
        {
            if (order.Carried <= 0)
                return;

            _resources.Add(order.Kind, order.Carried);
            _eventBus.Publish(new FloatingTextRequestedEvent($"+{order.Carried} {order.Kind}", order.Unit.Position));
            order.Carried = 0;
        }

        /// <summary>The best free cell around the node: close by, and beside it if possible.</summary>
        private bool TryPickWorkCell(Order order, ResourceNodeModel node, out Vector2Int result)
        {
            var area = node.Area;
            var level = _gridMap.GetLevel(area.position);
            var bestScore = float.MaxValue;
            result = default;

            for (var x = area.xMin - 1; x <= area.xMax; x++)
            {
                for (var y = area.yMin - 1; y <= area.yMax; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (area.Contains(cell) || !_gridMap.IsWalkable(cell) || _gridMap.GetLevel(cell) != level)
                        continue;

                    var score = Vector2.Distance(_gridMap.CellToWorldCenter(cell), order.Unit.Position) + GetPlacePenalty(area, cell);
                    if (IsWorkCellTaken(node, cell, order))
                        score += TakenPenalty;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        result = cell;
                    }
                }
            }

            return bestScore < float.MaxValue;
        }

        private static float GetPlacePenalty(RectInt area, Vector2Int cell)
        {
            var isInColumn = cell.x >= area.xMin && cell.x < area.xMax;
            var isInRow = cell.y >= area.yMin && cell.y < area.yMax;
            if (isInRow)
                return 0f;

            if (!isInColumn)
                return CornerPenalty;

            return cell.y < area.yMin ? BelowPenalty : AbovePenalty;
        }

        private bool IsWorkCellTaken(ResourceNodeModel node, Vector2Int cell, Order asking)
        {
            foreach (var other in _orders)
            {
                if (other != asking && other.Node == node && other.Stage != Stage.WalkingToDropOff && other.Spot == cell)
                    return true;
            }

            return false;
        }

        /// <summary>The point in the work cell closest to where the villager's tool lands on the node.</summary>
        private Vector2 GetWorkPoint(Order order)
        {
            var target = GetNodeBase(order.Node);
            var area = order.Node.Area;

            float side;
            if (order.Spot.x < area.xMin)
                side = -1f;
            else if (order.Spot.x >= area.xMax)
                side = 1f;
            else
                side = order.Unit.Position.x < target.x ? -1f : 1f;

            var min = _gridMap.Origin + order.Spot + Vector2.one * CellEdgeMargin;
            var max = _gridMap.Origin + order.Spot + Vector2.one * (1f - CellEdgeMargin);
            return new Vector2(
                Mathf.Clamp(target.x + side * order.Node.Definition.WorkDistance, min.x, max.x),
                Mathf.Clamp(target.y, min.y, max.y));
        }

        private Vector2 GetNodeBase(ResourceNodeModel node)
        {
            return _gridMap.AreaToWorldBottomCenter(node.Area) + node.Offset;
        }

        private Order TakeSuspended(GameEntityId unitId)
        {
            for (var i = _suspended.Count - 1; i >= 0; i--)
            {
                var order = _suspended[i];
                if (order.Unit.Id != unitId)
                    continue;

                _suspended.RemoveAt(i);
                return order;
            }

            return null;
        }

        private bool TryWalkToDropOff(Order order)
        {
            if (!TryFindDropOff(order.Unit.Position, out var dropOff) ||
                !_search.TryFindCellBeside(dropOff.Area, order.Unit.Position, out var spot))
                return false;

            order.DropOff = dropOff.Id;
            order.Spot = spot;
            order.Stage = Stage.WalkingToDropOff;
            _movement.SendTo(order.Unit, spot);
            return true;
        }

        private bool TryFindDropOff(Vector2 from, out BuildingModel nearest)
        {
            nearest = null;
            var nearestDistance = float.MaxValue;
            foreach (var building in _buildings.Buildings)
            {
                if (!building.Definition.IsDropOff || building.Faction != Faction.Player || !building.IsBuilt)
                    continue;

                var distance = (building.Area.center + _gridMap.Origin - from).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = building;
                }
            }

            return nearest != null;
        }

        private void End(int index)
        {
            _orders[index].Unit.SetHarvesting(null);
            _orders[index].Unit.SetOnJob(false);
            _orders.RemoveAt(index);
        }
    }
}
