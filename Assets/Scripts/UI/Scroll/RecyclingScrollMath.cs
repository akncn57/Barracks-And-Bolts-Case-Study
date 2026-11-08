using System;
using UnityEngine;

namespace BarracksAndBolts.UI.Scroll
{
    /// <summary>
    /// Layout math of <see cref="RecyclingScrollView"/>. Rows are counted from the top and go on forever
    /// in both directions; row -1 is the row above row 0.
    /// </summary>
    public static class RecyclingScrollMath
    {
        /// <summary>Like %, but never negative, so row -1 maps to the last item instead of an invalid index.</summary>
        public static int PositiveModulo(int value, int divisor)
        {
            if (divisor <= 0)
                throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "Divisor must be positive.");

            var remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }

        public static int GetFirstVisibleRow(float scrollOffset, float rowHeight)
        {
            return Mathf.FloorToInt(scrollOffset / rowHeight);
        }

        /// <summary>
        /// Rows needed to fill the viewport. One extra, because while scrolling a row can be cut at the top
        /// and another at the bottom at the same time.
        /// </summary>
        public static int GetPooledRowCount(float viewportHeight, float rowHeight)
        {
            return Mathf.Max(1, Mathf.CeilToInt(viewportHeight / rowHeight) + 1);
        }

        public static int GetDataIndex(int row, int column, int columns, int itemCount)
        {
            return PositiveModulo(row * columns + column, itemCount);
        }

        /// <summary>
        /// Rows after which the same items show up in the same places again.
        /// 5 items in 2 columns repeat after 5 rows (10 slots), 4 items in 2 columns after 2 rows.
        /// </summary>
        public static int GetCycleRowCount(int itemCount, int columns)
        {
            return itemCount / GreatestCommonDivisor(itemCount, columns);
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            while (b != 0)
                (a, b) = (b, a % b);

            return a;
        }
    }
}
