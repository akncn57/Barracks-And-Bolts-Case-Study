using System;
using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Grid
{
    /// <summary>
    /// Logical map used by placement and pathfinding. One cell is one world unit.
    /// Cell (0, 0) is the bottom-left cell; its bottom-left corner sits at <see cref="Origin"/>.
    /// </summary>
    public sealed class GridMap
    {
        private readonly bool[] _isTerrainWalkable;
        private readonly GameEntityId[] _occupants;

        public GridMap(int width, int height, Vector2 origin)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), $"Grid size must be positive, got {width}x{height}.");

            Width = width;
            Height = height;
            Origin = origin;
            _isTerrainWalkable = new bool[width * height];
            _occupants = new GameEntityId[width * height];

            Array.Fill(_isTerrainWalkable, true);
        }

        public int Width { get; }
        public int Height { get; }
        public Vector2 Origin { get; }
        public RectInt Bounds => new(0, 0, Width, Height);

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
        }

        public bool IsInBounds(RectInt area)
        {
            return area.width > 0 && area.height > 0 &&
                   area.xMin >= 0 && area.yMin >= 0 && area.xMax <= Width && area.yMax <= Height;
        }

        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            // Floor instead of a cast: a cast rounds -0.5 to 0 and would put it in the wrong cell.
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x - Origin.x),
                Mathf.FloorToInt(worldPosition.y - Origin.y));
        }

        public Vector2 CellToWorldCenter(Vector2Int cell)
        {
            return new Vector2(Origin.x + cell.x + 0.5f, Origin.y + cell.y + 0.5f);
        }

        /// <summary>World position of the bottom-center of an area. Buildings are pivoted there.</summary>
        public Vector2 AreaToWorldBottomCenter(RectInt area)
        {
            return new Vector2(Origin.x + area.x + area.width * 0.5f, Origin.y + area.y);
        }

        /// <summary>Area of the given size centered as close as possible to the world position. It may stick out of the map.</summary>
        public RectInt GetAreaCenteredAt(Vector2 worldPosition, Vector2Int size)
        {
            // Rounding the corner centers odd sizes on the cell under the point and even sizes on the nearest grid line.
            // Floor(x + 0.5) instead of RoundToInt, which rounds .5 to the even number and would jump back and forth.
            var corner = worldPosition - Origin - (Vector2)size * 0.5f;
            return new RectInt(Mathf.FloorToInt(corner.x + 0.5f), Mathf.FloorToInt(corner.y + 0.5f), size.x, size.y);
        }

        public bool IsTerrainWalkable(Vector2Int cell)
        {
            return IsInBounds(cell) && _isTerrainWalkable[ToIndex(cell)];
        }

        public void SetTerrainWalkable(Vector2Int cell, bool isWalkable)
        {
            EnsureInBounds(cell);
            _isTerrainWalkable[ToIndex(cell)] = isWalkable;
        }

        public GameEntityId GetOccupant(Vector2Int cell)
        {
            return IsInBounds(cell) ? _occupants[ToIndex(cell)] : GameEntityId.None;
        }

        /// <summary>True if a unit can stand on the cell: walkable terrain and nothing built on it.</summary>
        public bool IsWalkable(Vector2Int cell)
        {
            if (!IsInBounds(cell))
                return false;

            var index = ToIndex(cell);
            return _isTerrainWalkable[index] && !_occupants[index].IsValid;
        }

        /// <summary>True if every cell of the area is inside the map, walkable and empty.</summary>
        public bool IsAreaFree(RectInt area)
        {
            if (!IsInBounds(area))
                return false;

            foreach (var cell in area.allPositionsWithin)
            {
                if (!IsWalkable(cell))
                    return false;
            }

            return true;
        }

        /// <summary>Marks the area as owned by the entity. Does nothing and returns false if any cell is taken.</summary>
        public bool TryOccupy(RectInt area, GameEntityId occupant)
        {
            if (!occupant.IsValid)
                throw new ArgumentException("Cannot occupy cells with an invalid entity id.", nameof(occupant));

            // Check everything first, so a failed call never leaves the area half occupied.
            if (!IsAreaFree(area))
                return false;

            foreach (var cell in area.allPositionsWithin)
                _occupants[ToIndex(cell)] = occupant;

            return true;
        }

        /// <summary>Frees the cells of the area that belong to the entity. Cells owned by others are left untouched.</summary>
        public void Release(RectInt area, GameEntityId occupant)
        {
            foreach (var cell in area.allPositionsWithin)
            {
                if (!IsInBounds(cell))
                    continue;

                var index = ToIndex(cell);
                if (_occupants[index] == occupant)
                    _occupants[index] = GameEntityId.None;
            }
        }

        private int ToIndex(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }

        private void EnsureInBounds(Vector2Int cell)
        {
            if (!IsInBounds(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, $"Cell is outside the {Width}x{Height} grid.");
        }
    }
}
