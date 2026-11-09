using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Units;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Construction
{
    /// <summary>
    /// Villagers finish building sites. Each one walks up to the site, works on it with a hammer and every few seconds
    /// moves to another spot around it. More villagers build faster. Only normal games have sites.
    /// </summary>
    public sealed class ConstructionSystem : ISimulationSystem
    {
        // A villager works this long on one spot before it moves to another one around the site.
        private const float MinStaySeconds = 4f;
        private const float MaxStaySeconds = 7f;

        // Where the feet stand from the wall: the hammer reaches about this far to the side.
        private const float SideGap = 0.4f;
        private const float BelowGap = 0.15f;

        // The working place stays inside its own cell, so the next path starts from a walkable one.
        private const float CellEdgeMargin = 0.01f;

        // Added to the walking distance when choosing a spot. Beside and below the site look right;
        // above it the roof would hide the villager.
        private const float BelowPenalty = 1f;
        private const float CornerPenalty = 3f;
        private const float AbovePenalty = 6f;
        private const float TakenPenalty = 20f;

        // Makes a villager on the move pick different spots, not the same two again and again.
        private const float MoveAroundJitter = 3f;

        private enum Stage
        {
            Walking,
            Working
        }

        private sealed class Order
        {
            public UnitModel Unit;
            public BuildingModel Site;
            public Vector2Int Spot;
            public Stage Stage;
            public bool HasSteppedIn;
            public float StayLeft;
        }

        private readonly UnitService _units;
        private readonly UnitMovementSystem _movement;
        private readonly BuildingService _buildings;
        private readonly GridMap _gridMap;
        private readonly IEventBus _eventBus;
        private readonly List<Order> _orders = new();
        private readonly List<Order> _suspended = new();

        // Only for how long a villager stays on a spot and where it goes next; a fixed seed keeps tests repeatable.
        private readonly System.Random _random = new(17);

        public ConstructionSystem(UnitService units, UnitMovementSystem movement, BuildingService buildings, GridMap gridMap, IEventBus eventBus)
        {
            _units = units;
            _movement = movement;
            _buildings = buildings;
            _gridMap = gridMap;
            _eventBus = eventBus;
        }

        public SimulationPhase Phase => SimulationPhase.Gathering;

        /// <summary>True if a unit of this side could work on the building right now.</summary>
        public static bool CanWorkOn(UnitModel unit, BuildingModel site)
        {
            return unit.Definition.CanGather && !site.IsBuilt && !site.IsDead && site.Faction == unit.Faction;
        }

        /// <summary>Sends the villager to work on the site.</summary>
        /// <returns>False if the unit can't build, the site needs no work or there is nowhere to stand.</returns>
        public bool TryOrder(UnitModel unit, BuildingModel site)
        {
            if (!CanWorkOn(unit, site))
                return false;

            Cancel(unit.Id);
            var order = new Order { Unit = unit, Site = site };
            if (!TryWalkToSpot(order, isMovingAround: false))
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

        /// <summary>Sends the villager back to its suspended site.</summary>
        /// <returns>False if it had no site, or the site is finished or gone.</returns>
        public bool Resume(UnitModel unit)
        {
            var order = TakeSuspended(unit.Id);
            if (order == null || !_buildings.TryGet(order.Site.Id, out _) || !CanWorkOn(unit, order.Site))
                return false;

            if (!TryWalkToSpot(order, isMovingAround: false))
                return false;

            unit.SetOnJob(true);
            _orders.Add(order);
            return true;
        }

        public bool IsBuilding(GameEntityId unitId)
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
            if (!_buildings.TryGet(order.Site.Id, out _) || order.Site.IsBuilt)
                return false;

            return order.Stage == Stage.Walking ? UpdateWalk(order) : UpdateWork(order, deltaTime);
        }

        private bool UpdateWalk(Order order)
        {
            if (order.Unit.IsMoving)
                return true;

            // Something got in the way and the unit stopped short.
            if (_gridMap.WorldToCell(order.Unit.Position) != order.Spot)
                return false;

            // The path ends on the middle of the cell. The last step goes right up to the wall.
            if (!order.HasSteppedIn)
            {
                order.HasSteppedIn = true;
                _movement.StepTo(order.Unit, GetWorkPoint(order));
                return true;
            }

            order.Stage = Stage.Working;
            order.StayLeft = Mathf.Lerp(MinStaySeconds, MaxStaySeconds, (float)_random.NextDouble());
            order.Unit.FaceTowards(_gridMap.Origin + order.Site.Area.center);
            order.Unit.SetBuilding(true);
            return true;
        }

        private bool UpdateWork(Order order, float deltaTime)
        {
            var site = order.Site;
            site.AddBuildProgress(site.Definition.BuildTime > 0f ? deltaTime / site.Definition.BuildTime : 1f);
            if (site.IsBuilt)
            {
                _eventBus.Publish(new BuildingCompletedEvent(site));
                return false;
            }

            order.StayLeft -= deltaTime;
            if (order.StayLeft > 0f)
                return true;

            // Time for another spot. With none free, the villager just keeps working where it is.
            order.Unit.SetBuilding(false);
            if (!TryWalkToSpot(order, isMovingAround: true))
            {
                order.Unit.SetBuilding(true);
                order.StayLeft = MinStaySeconds;
            }

            return true;
        }

        private bool TryWalkToSpot(Order order, bool isMovingAround)
        {
            if (!TryPickSpot(order, isMovingAround, out var spot))
                return false;

            order.Spot = spot;
            order.Stage = Stage.Walking;
            order.HasSteppedIn = false;
            _movement.SendTo(order.Unit, spot);
            return true;
        }

        /// <summary>The best free cell around the site: close by and beside or below it.</summary>
        private bool TryPickSpot(Order order, bool isMovingAround, out Vector2Int result)
        {
            var area = order.Site.Area;
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

                    if (isMovingAround && cell == order.Spot)
                        continue;

                    var score = Vector2.Distance(_gridMap.CellToWorldCenter(cell), order.Unit.Position) + GetPlacePenalty(area, cell);
                    if (IsSpotTaken(order, cell))
                        score += TakenPenalty;

                    if (isMovingAround)
                        score += (float)_random.NextDouble() * MoveAroundJitter;

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

        private bool IsSpotTaken(Order asking, Vector2Int cell)
        {
            foreach (var other in _orders)
            {
                if (other != asking && other.Site == asking.Site && other.Spot == cell)
                    return true;
            }

            return false;
        }

        /// <summary>The point in the spot's cell right against the wall, where the hammer lands on the building.</summary>
        private Vector2 GetWorkPoint(Order order)
        {
            var area = order.Site.Area;
            var spot = order.Spot;
            var point = _gridMap.CellToWorldCenter(spot);
            var min = _gridMap.Origin + area.min;
            var max = _gridMap.Origin + area.max;

            if (spot.x < area.xMin)
                point.x = min.x - SideGap;
            else if (spot.x >= area.xMax)
                point.x = max.x + SideGap;

            if (spot.y < area.yMin)
                point.y = min.y - BelowGap;
            else if (spot.y >= area.yMax)
                point.y = max.y + BelowGap;

            var cellMin = _gridMap.Origin + spot + Vector2.one * CellEdgeMargin;
            var cellMax = _gridMap.Origin + spot + Vector2.one * (1f - CellEdgeMargin);
            return new Vector2(Mathf.Clamp(point.x, cellMin.x, cellMax.x), Mathf.Clamp(point.y, cellMin.y, cellMax.y));
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
            _orders[index].Unit.SetBuilding(false);
            _orders[index].Unit.SetOnJob(false);
            _orders.RemoveAt(index);
        }
    }
}
