using BarracksAndBolts.Gameplay.CameraControl;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class CameraMathTests
    {
        private const float Tolerance = 0.0001f;
        private static readonly Rect Bounds = new(0f, 0f, 40f, 30f);
        private static readonly Vector2 HalfView = new(8f, 5f);

        [Test]
        public void ClampCenter_InsideBounds_IsUnchanged()
        {
            var center = new Vector2(20f, 15f);

            Assert.AreEqual(center, CameraMath.ClampCenter(center, HalfView, Bounds));
        }

        [Test]
        public void ClampCenter_PastBottomLeft_StopsAtEdges()
        {
            var result = CameraMath.ClampCenter(new Vector2(-100f, -100f), HalfView, Bounds);

            Assert.AreEqual(new Vector2(8f, 5f), result);
        }

        [Test]
        public void ClampCenter_PastTopRight_StopsAtEdges()
        {
            var result = CameraMath.ClampCenter(new Vector2(100f, 100f), HalfView, Bounds);

            Assert.AreEqual(new Vector2(32f, 25f), result);
        }

        [Test]
        public void ClampCenter_ViewWiderThanBounds_CentersThatAxisOnly()
        {
            var narrowBounds = new Rect(0f, 0f, 10f, 30f);

            var result = CameraMath.ClampCenter(new Vector2(3f, 2f), HalfView, narrowBounds);

            Assert.AreEqual(5f, result.x, Tolerance);
            Assert.AreEqual(5f, result.y, Tolerance);
        }

        [Test]
        public void ClampCenter_ViewExactlyBoundsSize_Centers()
        {
            var result = CameraMath.ClampCenter(new Vector2(1f, 1f), new Vector2(20f, 15f), Bounds);

            Assert.AreEqual(Bounds.center, result);
        }

        [Test]
        public void ZoomTowards_KeepsFocusPointFixedOnScreen()
        {
            var center = new Vector2(10f, 10f);
            var focus = new Vector2(14f, 12f);

            var newCenter = CameraMath.ZoomTowards(center, focus, 6f, 3f);

            // The focus was 4/6 of a half view to the right; after zooming it must stay at the same screen spot.
            Assert.AreEqual((focus.x - center.x) / 6f, (focus.x - newCenter.x) / 3f, Tolerance);
            Assert.AreEqual((focus.y - center.y) / 6f, (focus.y - newCenter.y) / 3f, Tolerance);
        }

        [Test]
        public void ZoomTowards_FocusOnCenter_KeepsCenter()
        {
            var center = new Vector2(5f, 5f);

            Assert.AreEqual(center, CameraMath.ZoomTowards(center, center, 6f, 3f));
        }

        [Test]
        public void ZoomTowards_ZeroOldSize_ReturnsCenter()
        {
            var center = new Vector2(5f, 5f);

            Assert.AreEqual(center, CameraMath.ZoomTowards(center, Vector2.zero, 0f, 3f));
        }

        [Test]
        public void ScreenToWorld_ScreenCenter_ReturnsViewCenter()
        {
            var center = new Vector2(10f, 20f);

            var world = CameraMath.ScreenToWorld(new Vector2(800f, 450f), new Vector2(1600f, 900f), center, 5f);

            Assert.AreEqual(center.x, world.x, Tolerance);
            Assert.AreEqual(center.y, world.y, Tolerance);
        }

        [Test]
        public void ScreenToWorld_TopRightCorner_ReturnsViewCorner()
        {
            // 16:9 screen with size 5: the view is 10 units tall and about 17.78 units wide.
            var world = CameraMath.ScreenToWorld(new Vector2(1600f, 900f), new Vector2(1600f, 900f), Vector2.zero, 5f);

            Assert.AreEqual(5f * 16f / 9f, world.x, Tolerance);
            Assert.AreEqual(5f, world.y, Tolerance);
        }

        [Test]
        public void WorldToScreen_IsTheWayBackFromScreenToWorld()
        {
            var screen = new Vector2(1600f, 900f);
            var center = new Vector2(10f, 20f);
            var point = new Vector2(300f, 700f);

            var world = CameraMath.ScreenToWorld(point, screen, center, 5f);
            var back = CameraMath.WorldToScreen(world, screen, center, 5f);

            Assert.AreEqual(point.x, back.x, 0.01f);
            Assert.AreEqual(point.y, back.y, 0.01f);
        }

        [Test]
        public void ScreenToWorld_ZeroScreenSize_ReturnsViewCenter()
        {
            var center = new Vector2(3f, 4f);

            Assert.AreEqual(center, CameraMath.ScreenToWorld(Vector2.one, Vector2.zero, center, 5f));
        }

        [Test]
        public void GetWorldUnitsPerPixel_UsesFullViewHeight()
        {
            Assert.AreEqual(0.01f, CameraMath.GetWorldUnitsPerPixel(5f, 1000f), Tolerance);
        }

        [Test]
        public void GetWorldUnitsPerPixel_ZeroScreenHeight_ReturnsZero()
        {
            Assert.AreEqual(0f, CameraMath.GetWorldUnitsPerPixel(5f, 0f));
        }

        [TestCase(5f, 500f, -1f, 0f)]
        [TestCase(995f, 500f, 1f, 0f)]
        [TestCase(500f, 5f, 0f, -1f)]
        [TestCase(500f, 795f, 0f, 1f)]
        [TestCase(500f, 400f, 0f, 0f)]
        public void GetEdgePanDirection_ReturnsEdgeDirection(float x, float y, float expectedX, float expectedY)
        {
            var direction = CameraMath.GetEdgePanDirection(new Vector2(x, y), new Vector2(1000f, 800f), 10f);

            Assert.AreEqual(new Vector2(expectedX, expectedY), direction);
        }

        [Test]
        public void GetEdgePanDirection_Corner_IsNormalized()
        {
            var direction = CameraMath.GetEdgePanDirection(new Vector2(2f, 2f), new Vector2(1000f, 800f), 10f);

            Assert.AreEqual(1f, direction.magnitude, Tolerance);
        }

        [Test]
        public void GetEdgePanDirection_PointerOutsideWindow_ReturnsZero()
        {
            var direction = CameraMath.GetEdgePanDirection(new Vector2(-50f, 400f), new Vector2(1000f, 800f), 10f);

            Assert.AreEqual(Vector2.zero, direction);
        }
    }
}
