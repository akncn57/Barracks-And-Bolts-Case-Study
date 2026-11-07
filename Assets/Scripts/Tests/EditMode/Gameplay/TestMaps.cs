using System.Collections.Generic;
using BarracksAndBolts.Gameplay.Grid;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    internal static class TestMaps
    {
        /// <summary>
        /// Builds a map from rows written top to bottom, as they look on screen: '.' is walkable, '#' is blocked.
        /// </summary>
        public static GridMap FromRows(params string[] rows)
        {
            var height = rows.Length;
            var width = rows[0].Length;
            var map = new GridMap(width, height, Vector2.zero);

            for (var row = 0; row < height; row++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (rows[row][x] == '#')
                        map.SetTerrainWalkable(new Vector2Int(x, height - 1 - row), false);
                }
            }

            return map;
        }

        public static void AssertWalkableAndContiguous(GridMap map, Vector2Int start, IReadOnlyList<Vector2Int> path)
        {
            var previous = start;
            foreach (var cell in path)
            {
                Assert.IsTrue(map.IsWalkable(cell), $"Path goes through blocked cell {cell}.");

                var step = cell - previous;
                Assert.IsTrue(Mathf.Abs(step.x) <= 1 && Mathf.Abs(step.y) <= 1 && step != Vector2Int.zero,
                    $"Path jumps from {previous} to {cell}.");
                previous = cell;
            }
        }
    }
}
