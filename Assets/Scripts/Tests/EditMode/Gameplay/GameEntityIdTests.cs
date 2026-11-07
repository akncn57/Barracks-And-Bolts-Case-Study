using System;
using BarracksAndBolts.Gameplay.Entities;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class GameEntityIdTests
    {
        [Test]
        public void Default_IsNoneAndInvalid()
        {
            Assert.AreEqual(GameEntityId.None, default(GameEntityId));
            Assert.IsFalse(default(GameEntityId).IsValid);
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Constructor_NonPositiveValue_Throws(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GameEntityId(value));
        }

        [Test]
        public void Equality_ComparesValues()
        {
            Assert.IsTrue(new GameEntityId(5) == new GameEntityId(5));
            Assert.IsTrue(new GameEntityId(5) != new GameEntityId(6));
        }
    }
}
