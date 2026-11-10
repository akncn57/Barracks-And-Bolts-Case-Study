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
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Gathering
{
    /// <summary>
    /// Villagers work farms: they step inside the fence and tend the crops, carry the harvest to the nearest drop-off
    /// and come back.
    /// A farm never runs out and takes only as many villagers as its definition allows.
    /// </summary>
    public sealed class FarmingSystem : ISimulationSystem
    {
        // How far inside the fence the feet stand, so the villager is among the crops and not on the fence.
        private const float SideFence = 0.45f;
        private const float BottomFence = 0.55f;
        private const float TopFence = 0.8f;
        private const float CellMargin = 0.1f;

        // Where the villager comes in from. From above it would work at the back of the field.
        private const float BelowPenalty = 1f;
        private const float CornerPenalty = 3f;
        private const float AbovePenalty = 6f;

        private enum Stage
        {
            WalkingToFarm,
            Working,
            WalkingToDropOff
        }

        private sealed class Order
        {
            public UnitModel Unit;
            public BuildingModel Farm;
            public GameEntityId DropOff;
            public Vector2Int Spot;
            public Stage Stage;
            public bool HasSteppedIn;
            public float WorkLeft;
            public int Carried;
        }

        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly BuildingService _buildings;
        private readonly ResourceService _resources;
        private readonly GridSearch _search;
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;
        private readonly List<Order> _orders = new();
        private readonly List<Order> _suspended = new();

        // Villagers walking out of a field: they draw over it until they are past the fence.
        private readonly List<(UnitModel Unit, RectInt Field)> _leaving = new();
        private readonly System.Random _random = new(23);

        public FarmingSystem(UnitService units, UnitMovementSystem movement, BuildingService buildings, ResourceService resources,
            GridSearch search, GridMap gridMap, IEventBus eventBus)
        {
            _units = units;
            _movement = movement;
            _buildings = buildings;
            _resources = resources;
            _search = search;
            _gridMap = gridMap;
            _eventBus = eventBus;
        }

        public SimulationPhase Phase => SimulationPhase.Gathering;

        public static bool IsFarm(BuildingModel building)
        {
            return building.Definition.FarmYield > 0;
        }

        public int CountWorkers(BuildingModel farm)
        {
            var count = 0;
            foreach (var order in _orders)
            {
                if (order.Farm == farm)
                    count++;
            }

            return count;
        }

        /// <summary>True if the farm is finished, the unit's own, and has room for one more villager.</summary>
        public bool HasRoomFor(UnitModel unit, BuildingModel farm)
        {
            return unit.Definition.CanGather && IsFarm(farm) && farm.IsBuilt && !farm.IsDead && farm.Faction == unit.Faction &&
                   CountWorkers(farm) < farm.Definition.FarmWorkers;
        }

        /// <returns>False if the unit can't farm, the farm is full or there is nowhere to stand.</returns>
        public bool TryOrder(UnitModel unit, BuildingModel farm)
        {
            if (IsFarmingAt(unit.Id, farm))
                return true;

            Cancel(unit.Id);
            if (!HasRoomFor(unit, farm))
                return false;

            var order = new Order { Unit = unit, Farm = farm };
            if (!TryWalkToFarm(order))
                return false;

            unit.ClearAttackTarget();
            unit.SetOnJob(true);
            _orders.Add(order);
            return true;
        }

        public void Cancel(GameEntityId unitId)
        {
            for (var i = _orders.Count - 1; i >= 0; i--)
            {
                if (_orders[i].Unit.Id != unitId)
                    continue;

                SetLoad(_orders[i], 0);
                End(i);
            }

            var suspended = TakeSuspended(unitId);
            if (suspended != null)
                SetLoad(suspended, 0);
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

        /// <summary>A suspended villager has reached a drop-off: its harvest counts now.</summary>
        public void DeliverLoad(GameEntityId unitId)
        {
            foreach (var order in _suspended)
            {
                if (order.Unit.Id == unitId)
                    Deliver(order);
            }
        }

        /// <returns>False if it had no farm, or the farm is gone.</returns>
        public bool Resume(UnitModel unit)
        {
            var order = TakeSuspended(unit.Id);
            if (order == null)
                return false;

            if (!_buildings.TryGet(order.Farm.Id, out _) || order.Farm.IsDead || !TryWalkToFarm(order))
            {
                SetLoad(order, 0);
                return false;
            }

            unit.SetOnJob(true);
            _orders.Add(order);
            return true;
        }

        public bool IsFarming(GameEntityId unitId)
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
                if (_units.TryGet(order.Unit.Id, out _) && !order.Unit.HasAttackTarget && Advance(order, deltaTime))
                    continue;

                SetLoad(order, 0);
                End(i);
            }

            UpdateLeaving();
        }

        private bool IsFarmingAt(GameEntityId unitId, BuildingModel farm)
        {
            foreach (var order in _orders)
            {
                if (order.Unit.Id == unitId && order.Farm == farm)
                    return true;
            }

            return false;
        }

        /// <returns>False if the order is over.</returns>
        private bool Advance(Order order, float deltaTime)
        {
            if (!_buildings.TryGet(order.Farm.Id, out _) || order.Farm.IsDead)
                return false;

            switch (order.Stage)
            {
                case Stage.WalkingToFarm:
                    return UpdateWalkToFarm(order);
                case Stage.Working:
                    return UpdateWork(order, deltaTime);
                default:
                    return UpdateWalkToDropOff(order);
            }
        }

        private bool UpdateWalkToFarm(Order order)
        {
            if (order.Unit.IsMoving)
                return true;

            // The path ends beside the fence. The last step goes over it, into the field.
            if (!order.HasSteppedIn)
            {
                // Something got in the way and the unit stopped short.
                if (_gridMap.WorldToCell(order.Unit.Position) != order.Spot)
                    return false;

                order.HasSteppedIn = true;
                StopLeaving(order.Unit);
                order.Unit.SetInField(true);
                _movement.StepTo(order.Unit, GetWorkPoint(order));
                return true;
            }

            order.Stage = Stage.Working;
            order.WorkLeft = order.Farm.Definition.FarmHarvestDuration;
            order.Unit.FaceTowards(_gridMap.Origin + order.Farm.Area.center);
            order.Unit.SetHarvesting(ResourceKind.Food);
            return true;
        }

        private bool UpdateWork(Order order, float deltaTime)
        {
            order.WorkLeft -= deltaTime;
            if (order.WorkLeft > 0f)
                return true;

            order.Unit.SetHarvesting(null);
            SetLoad(order, order.Farm.Definition.FarmYield);
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
            return TryWalkToFarm(order);
        }

        private bool TryWalkToFarm(Order order)
        {
            if (!TryPickSpot(order, out var spot))
                return false;

            order.Spot = spot;
            order.Stage = Stage.WalkingToFarm;
            order.HasSteppedIn = false;
            _movement.SendTo(order.Unit, spot);
            return true;
        }

        private bool TryWalkToDropOff(Order order)
        {
            if (!TryFindDropOff(order.Unit.Position, out var dropOff) ||
                !_search.TryFindCellBeside(dropOff.Area, order.Unit.Position, out var spot))
                return false;

            order.DropOff = dropOff.Id;
            order.Spot = spot;
            order.Stage = Stage.WalkingToDropOff;
            StartLeaving(order);
            _movement.SendTo(order.Unit, spot);
            return true;
        }

        private void Deliver(Order order)
        {
            if (order.Carried <= 0)
                return;

            _resources.Add(ResourceKind.Food, order.Carried);
            _eventBus.Publish(new FloatingTextRequestedEvent($"+{order.Carried} Food", order.Unit.Position));
            SetLoad(order, 0);
        }

        /// <summary>Sets what the villager carries. The unit model shows it, so the view can draw an icon.</summary>
        private static void SetLoad(Order order, int amount)
        {
            order.Carried = amount;
            order.Unit.SetCarrying(amount > 0 ? ResourceKind.Food : null);
        }

        /// <summary>The nearest free cell around the field, beside or below it if it can.</summary>
        private bool TryPickSpot(Order order, out Vector2Int result)
        {
            var area = order.Farm.Area;
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

        /// <summary>
        /// A random point inside the fence, in the field cell next to the spot. Only an edge cell, so the way out is one
        /// step and an enemy at the fence can reach the villager.
        /// </summary>
        private Vector2 GetWorkPoint(Order order)
        {
            var area = order.Farm.Area;
            var cell = new Vector2Int(
                Mathf.Clamp(order.Spot.x, area.xMin, area.xMax - 1),
                Mathf.Clamp(order.Spot.y, area.yMin, area.yMax - 1));
            var fieldMin = _gridMap.Origin + area.min;
            var fieldMax = _gridMap.Origin + area.max;
            var cellMin = _gridMap.Origin + cell;

            var x = PickBetween(Mathf.Max(cellMin.x + CellMargin, fieldMin.x + SideFence), Mathf.Min(cellMin.x + 1f - CellMargin, fieldMax.x - SideFence));
            var y = PickBetween(Mathf.Max(cellMin.y + CellMargin, fieldMin.y + BottomFence), Mathf.Min(cellMin.y + 1f - CellMargin, fieldMax.y - TopFence));
            return new Vector2(x, y);
        }

        private float PickBetween(float min, float max)
        {
            return min >= max ? (min + max) * 0.5f : min + (float)_random.NextDouble() * (max - min);
        }

        private void StartLeaving(Order order)
        {
            if (order.Unit.IsInField)
                _leaving.Add((order.Unit, order.Farm.Area));
        }

        private void StopLeaving(UnitModel unit)
        {
            for (var i = _leaving.Count - 1; i >= 0; i--)
            {
                if (_leaving[i].Unit == unit)
                    _leaving.RemoveAt(i);
            }
        }

        private void UpdateLeaving()
        {
            for (var i = _leaving.Count - 1; i >= 0; i--)
            {
                var (unit, field) = _leaving[i];
                if (_units.TryGet(unit.Id, out _) && field.Contains(_gridMap.WorldToCell(unit.Position)))
                    continue;

                unit.SetInField(false);
                _leaving.RemoveAt(i);
            }
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

        private void End(int index)
        {
            StartLeaving(_orders[index]);
            _orders[index].Unit.SetHarvesting(null);
            _orders[index].Unit.SetOnJob(false);
            _orders.RemoveAt(index);
        }
    }
}
