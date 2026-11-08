using System;
using BarracksAndBolts.UI.Scroll;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.UI
{
    public class RecyclingScrollMathTests
    {
        [TestCase(7, 3, 1)]
        [TestCase(-1, 3, 2)]
        [TestCase(-3, 3, 0)]
        [TestCase(-7, 3, 2)]
        public void PositiveModulo_IsNeverNegative(int value, int divisor, int expected)
        {
            Assert.AreEqual(expected, RecyclingScrollMath.PositiveModulo(value, divisor));
        }

        [Test]
        public void PositiveModulo_ZeroDivisor_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RecyclingScrollMath.PositiveModulo(1, 0));
        }

        [TestCase(0f, 0)]
        [TestCase(99.9f, 0)]
        [TestCase(100f, 1)]
        [TestCase(-0.1f, -1)]
        public void GetFirstVisibleRow_FloorsOffset(float offset, int expected)
        {
            Assert.AreEqual(expected, RecyclingScrollMath.GetFirstVisibleRow(offset, 100f));
        }

        [TestCase(300f, 4)]
        [TestCase(350f, 5)]
        [TestCase(0f, 1)]
        public void GetPooledRowCount_FillsViewportPlusOneRow(float viewportHeight, int expected)
        {
            Assert.AreEqual(expected, RecyclingScrollMath.GetPooledRowCount(viewportHeight, 100f));
        }

        [Test]
        public void GetDataIndex_WrapsPastLastItem()
        {
            // 5 items in 2 columns: row 2 holds item 4 and then item 0 again.
            Assert.AreEqual(4, RecyclingScrollMath.GetDataIndex(2, 0, 2, 5));
            Assert.AreEqual(0, RecyclingScrollMath.GetDataIndex(2, 1, 2, 5));
        }

        [Test]
        public void GetDataIndex_RowsAboveTop_ShowLastItems()
        {
            Assert.AreEqual(3, RecyclingScrollMath.GetDataIndex(-1, 0, 2, 5));
            Assert.AreEqual(4, RecyclingScrollMath.GetDataIndex(-1, 1, 2, 5));
        }

        [Test]
        public void GetDataIndex_FewerItemsThanColumns_RepeatsToFillRow()
        {
            Assert.AreEqual(0, RecyclingScrollMath.GetDataIndex(0, 1, 2, 1));
            Assert.AreEqual(0, RecyclingScrollMath.GetDataIndex(3, 0, 2, 1));
        }

        [TestCase(4, 2, 2)]
        [TestCase(5, 2, 5)]
        [TestCase(6, 4, 3)]
        [TestCase(1, 2, 1)]
        public void GetCycleRowCount_RowsUntilLayoutRepeats(int itemCount, int columns, int expected)
        {
            Assert.AreEqual(expected, RecyclingScrollMath.GetCycleRowCount(itemCount, columns));
        }

        [TestCase(5, 2)]
        [TestCase(6, 4)]
        public void GetCycleRowCount_RowAfterCycleShowsSameItems(int itemCount, int columns)
        {
            var cycle = RecyclingScrollMath.GetCycleRowCount(itemCount, columns);

            for (var column = 0; column < columns; column++)
            {
                Assert.AreEqual(
                    RecyclingScrollMath.GetDataIndex(0, column, columns, itemCount),
                    RecyclingScrollMath.GetDataIndex(cycle, column, columns, itemCount));
            }
        }
    }
}
