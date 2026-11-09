using BarracksAndBolts.Gameplay.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class CoverageRingTests
    {
        private const int Size = 40;
        private const int LinePixels = 2;

        private static readonly Color32 Line = new(255, 0, 0, 255);
        private static readonly Color32 Fill = new(255, 0, 0, 20);

        private Color32[] _pixels;

        [SetUp]
        public void SetUp()
        {
            _pixels = new Color32[Size * Size];
            CoverageRingView.PaintRing(_pixels, Size, LinePixels, Line, Fill);
        }

        private Color32 At(int x, int y)
        {
            return _pixels[y * Size + x];
        }

        [Test]
        public void Middle_IsFilled()
        {
            Assert.AreEqual(Fill, At(Size / 2, Size / 2));
        }

        [Test]
        public void Rim_IsTheLine()
        {
            Assert.AreEqual(Line, At(0, Size / 2), "Left.");
            Assert.AreEqual(Line, At(Size - 1, Size / 2), "Right.");
            Assert.AreEqual(Line, At(Size / 2, 0), "Bottom.");
            Assert.AreEqual(Line, At(Size / 2, Size - 1), "Top.");
        }

        [Test]
        public void JustInsideTheLine_IsFilled()
        {
            Assert.AreEqual(Fill, At(LinePixels + 1, Size / 2));
        }

        [Test]
        public void Corners_AreEmpty()
        {
            Assert.AreEqual(0, At(0, 0).a);
            Assert.AreEqual(0, At(Size - 1, Size - 1).a);
        }
    }
}
