using BarracksAndBolts.UI.Hud;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.UI
{
    public class OffScreenArrowMathTests
    {
        private static readonly Vector2 Screen = new(1000f, 500f);

        [Test]
        public void PointInside_IsOnScreen()
        {
            Assert.IsTrue(OffScreenArrowMath.IsOnScreen(new Vector2(10f, 490f), Screen));
            Assert.IsFalse(OffScreenArrowMath.IsOnScreen(new Vector2(-1f, 250f), Screen));
            Assert.IsFalse(OffScreenArrowMath.IsOnScreen(new Vector2(500f, 501f), Screen));
        }

        [Test]
        public void TargetToTheRight_ArrowOnTheRightEdge()
        {
            var edge = OffScreenArrowMath.GetEdgePoint(new Vector2(3000f, 250f), Screen, 50f);

            Assert.AreEqual(950f, edge.x, 0.001f);
            Assert.AreEqual(250f, edge.y, 0.001f);
            Assert.AreEqual(0f, OffScreenArrowMath.GetAngle(new Vector2(3000f, 250f), Screen), 0.001f);
        }

        [Test]
        public void TargetUpAndRight_ArrowStopsAtTheNearerEdge()
        {
            // Half extents with the margin: 450 x 200. Going (+1000, +1000) the top edge comes first.
            var edge = OffScreenArrowMath.GetEdgePoint(new Vector2(1500f, 1250f), Screen, 50f);

            Assert.AreEqual(700f, edge.x, 0.001f);
            Assert.AreEqual(450f, edge.y, 0.001f);
            Assert.AreEqual(45f, OffScreenArrowMath.GetAngle(new Vector2(1500f, 1250f), Screen), 0.001f);
        }

        [Test]
        public void TargetStraightDown_ArrowOnTheBottomEdge()
        {
            var edge = OffScreenArrowMath.GetEdgePoint(new Vector2(500f, -800f), Screen, 50f);

            Assert.AreEqual(500f, edge.x, 0.001f);
            Assert.AreEqual(50f, edge.y, 0.001f);
            Assert.AreEqual(-90f, OffScreenArrowMath.GetAngle(new Vector2(500f, -800f), Screen), 0.001f);
        }
    }
}
