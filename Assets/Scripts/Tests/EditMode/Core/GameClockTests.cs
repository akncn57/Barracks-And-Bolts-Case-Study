using BarracksAndBolts.Core.Simulation;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Core
{
    public class GameClockTests
    {
        private GameClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new GameClock();
        }

        [Test]
        public void Advance_ReturnsRealDelta_AtNormalSpeed()
        {
            Assert.AreEqual(0.05f, _clock.Advance(0.05f), 0.0001f);
        }

        [Test]
        public void Advance_WhenPaused_ReturnsZeroAndKeepsElapsedTime()
        {
            _clock.SetPaused(true);

            Assert.AreEqual(0f, _clock.Advance(0.05f));
            Assert.AreEqual(0f, _clock.ElapsedTime);
        }

        [Test]
        public void Advance_AppliesTimeScale()
        {
            _clock.SetTimeScale(2f);

            Assert.AreEqual(0.1f, _clock.Advance(0.05f), 0.0001f);
        }

        [Test]
        public void Advance_ClampsLongFrames()
        {
            Assert.AreEqual(GameClock.MaxFrameDelta, _clock.Advance(5f), 0.0001f);
        }

        [Test]
        public void Advance_NegativeDelta_ReturnsZero()
        {
            Assert.AreEqual(0f, _clock.Advance(-1f));
        }

        [Test]
        public void Advance_AccumulatesElapsedTime()
        {
            _clock.Advance(0.05f);
            _clock.Advance(0.05f);

            Assert.AreEqual(0.1f, _clock.ElapsedTime, 0.0001f);
        }

        [TestCase(-1f, 0f)]
        [TestCase(10f, GameClock.MaxTimeScale)]
        public void SetTimeScale_ClampsToValidRange(float requested, float expected)
        {
            _clock.SetTimeScale(requested);

            Assert.AreEqual(expected, _clock.TimeScale);
        }
    }
}
