using BarracksAndBolts.UI.Common;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.UI
{
    public class PanelSizeMathTests
    {
        [Test]
        public void ContentBetweenLimits_PanelHugsIt()
        {
            Assert.AreEqual(560f, PanelSizeMath.FitHeight(500f, 60f, 300f, 800f, 1000f));
        }

        [Test]
        public void SmallContent_PanelStaysAtMinimum()
        {
            Assert.AreEqual(300f, PanelSizeMath.FitHeight(100f, 60f, 300f, 800f, 1000f));
        }

        [Test]
        public void TallContent_PanelStopsAtMaximum()
        {
            Assert.AreEqual(800f, PanelSizeMath.FitHeight(2000f, 60f, 300f, 800f, 1000f));
        }

        [Test]
        public void SmallScreen_BeatsMaximum()
        {
            Assert.AreEqual(600f, PanelSizeMath.FitHeight(2000f, 60f, 300f, 800f, 600f));
        }

        [Test]
        public void VerySmallScreen_BeatsMinimumToo()
        {
            Assert.AreEqual(200f, PanelSizeMath.FitHeight(100f, 60f, 300f, 800f, 200f));
        }

        [Test]
        public void MaximumBelowMinimum_MinimumWins()
        {
            Assert.AreEqual(300f, PanelSizeMath.FitHeight(500f, 60f, 300f, 100f, 1000f));
        }
    }
}
