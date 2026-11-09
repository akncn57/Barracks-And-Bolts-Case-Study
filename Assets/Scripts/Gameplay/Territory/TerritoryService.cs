using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Territory
{
    /// <summary>
    /// Which cells the player may build on in a normal game: the circles around the Castle, outposts and towers.
    /// Worked out only when one of those buildings is finished or goes. A building site opens nothing yet. Buildings left outside stay and keep working.
    /// Sandbox mode has no territory, so every cell counts as inside.
    /// </summary>
    public sealed class TerritoryService : IDisposable
    {
        private readonly GridMap _gridMap;
        private readonly BuildingService _buildings;
        private readonly CompositeDisposable _subscriptions = new();
        private readonly bool[] _isInside;

        public TerritoryService(GridMap gridMap, BuildingService buildings, SandboxService sandbox, IEventBus eventBus)
        {
            _gridMap = gridMap;
            _buildings = buildings;
            IsEnabled = !sandbox.IsEnabled;
            if (!IsEnabled)
                return;

            _isInside = new bool[gridMap.Width * gridMap.Height];
            eventBus.Subscribe<BuildingPlacedEvent>(e => RebuildIfSource(e.Building)).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingCompletedEvent>(e => RebuildIfSource(e.Building)).AddTo(_subscriptions);
            eventBus.Subscribe<BuildingDestroyedEvent>(e => RebuildIfSource(e.Building)).AddTo(_subscriptions);

            // The Castle may already stand, depending on who was created first.
            Rebuild();
        }

        public bool IsEnabled { get; }

        /// <summary>Goes up on every change, so a view can tell when to redraw.</summary>
        public int Version { get; private set; }

        public bool IsInside(Vector2Int cell)
        {
            if (!IsEnabled)
                return true;

            return _gridMap.IsInBounds(cell) && _isInside[ToIndex(cell)];
        }

        /// <summary>True only if every cell of the area is inside.</summary>
        public bool IsInside(RectInt area)
        {
            foreach (var cell in area.allPositionsWithin)
            {
                if (!IsInside(cell))
                    return false;
            }

            return true;
        }

        /// <summary>True if a building with this radius, its middle on the center, would open the cell. Center is in cell coordinates.</summary>
        public static bool IsInCircle(Vector2Int cell, Vector2 center, float radius)
        {
            // Measured to the middle of the cell, so a cell is either in or out as a whole.
            // Strictly less: exactly on the radius would leave a one-cell spike on each side of the circle.
            var cellCenter = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
            return (cellCenter - center).sqrMagnitude < radius * radius;
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void RebuildIfSource(BuildingModel building)
        {
            // A site opens nothing, so placing or losing one changes nothing. Finishing it does.
            if (IsSource(building) && building.IsBuilt)
                Rebuild();
        }

        private static bool IsSource(BuildingModel building)
        {
            return building.Faction == Faction.Player && building.Definition.TerritoryRadius > 0f;
        }

        private void Rebuild()
        {
            Array.Clear(_isInside, 0, _isInside.Length);
            foreach (var building in _buildings.Buildings)
            {
                if (IsSource(building) && building.IsBuilt)
                    MarkCircle(building.Area.center, building.Definition.TerritoryRadius);
            }

            Version++;
        }

        private void MarkCircle(Vector2 center, float radius)
        {
            var xMin = Mathf.Max(Mathf.FloorToInt(center.x - radius), 0);
            var yMin = Mathf.Max(Mathf.FloorToInt(center.y - radius), 0);
            var xMax = Mathf.Min(Mathf.CeilToInt(center.x + radius), _gridMap.Width - 1);
            var yMax = Mathf.Min(Mathf.CeilToInt(center.y + radius), _gridMap.Height - 1);

            for (var y = yMin; y <= yMax; y++)
            {
                for (var x = xMin; x <= xMax; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (IsInCircle(cell, center, radius))
                        _isInside[ToIndex(cell)] = true;
                }
            }
        }

        private int ToIndex(Vector2Int cell)
        {
            return cell.y * _gridMap.Width + cell.x;
        }
    }
}
