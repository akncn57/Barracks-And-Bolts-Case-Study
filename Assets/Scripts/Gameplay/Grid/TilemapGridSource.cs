using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace BarracksAndBolts.Gameplay.Grid
{
    /// <summary>
    /// Builds the logical <see cref="GridMap"/> from the painted tilemaps.
    /// A cell is walkable if it has a ground tile and no tile on any blocking tilemap.
    /// </summary>
    public sealed class TilemapGridSource : MonoBehaviour
    {
        [SerializeField] private Tilemap groundTilemap;

        [Tooltip("Tiles on these tilemaps block movement and building, e.g. rocks or cliffs.")]
        [SerializeField] private Tilemap[] blockingTilemaps = Array.Empty<Tilemap>();

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = true;

        private GridMap _gridMap;

        public GridMap BuildGridMap()
        {
            if (groundTilemap == null)
                throw new InvalidOperationException($"{name}: ground tilemap is not assigned.");

            // GridMap treats one cell as one world unit.
            if (groundTilemap.layoutGrid.cellSize != Vector3.one)
                throw new InvalidOperationException($"{name}: grid cell size must be (1, 1, 1), got {groundTilemap.layoutGrid.cellSize}.");

            // Painting and erasing can leave empty rows at the edges; shrink to the painted area.
            groundTilemap.CompressBounds();
            var bounds = groundTilemap.cellBounds;
            if (bounds.size.x <= 0 || bounds.size.y <= 0)
                throw new InvalidOperationException($"{name}: ground tilemap is empty.");

            var origin = (Vector2)groundTilemap.CellToWorld(bounds.min);
            _gridMap = new GridMap(bounds.size.x, bounds.size.y, origin);

            foreach (var tilePosition in bounds.allPositionsWithin)
            {
                var cell = new Vector2Int(tilePosition.x - bounds.xMin, tilePosition.y - bounds.yMin);
                var isWalkable = groundTilemap.HasTile(tilePosition) && !IsBlocked(tilePosition);
                _gridMap.SetTerrainWalkable(cell, isWalkable);
            }

            return _gridMap;
        }

        private bool IsBlocked(Vector3Int tilePosition)
        {
            foreach (var tilemap in blockingTilemaps)
            {
                if (tilemap != null && tilemap.HasTile(tilePosition))
                    return true;
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos || _gridMap == null)
                return;

            var bounds = _gridMap.Bounds;
            var center = _gridMap.Origin + new Vector2(bounds.width, bounds.height) * 0.5f;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(center, new Vector3(bounds.width, bounds.height));

            foreach (var cell in bounds.allPositionsWithin)
            {
                if (_gridMap.IsWalkable(cell))
                    continue;

                // Red: blocked terrain. Yellow: occupied by an entity.
                Gizmos.color = _gridMap.IsTerrainWalkable(cell) ? new Color(1f, 0.9f, 0f, 0.4f) : new Color(1f, 0f, 0f, 0.4f);
                Gizmos.DrawCube(_gridMap.CellToWorldCenter(cell), new Vector3(0.9f, 0.9f));
            }
        }
    }
}
