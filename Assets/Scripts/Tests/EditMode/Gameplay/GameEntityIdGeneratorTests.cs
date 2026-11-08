using BarracksAndBolts.Gameplay.Entities;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class GameEntityIdGeneratorTests
    {
        [Test]
        public void Next_StartsFromOneAndNeverRepeats()
        {
            var generator = new GameEntityIdGenerator();

            var first = generator.Next();
            var second = generator.Next();

            Assert.AreEqual(1, first.Value);
            Assert.IsTrue(first.IsValid);
            Assert.AreNotEqual(first, second);
        }
    }
}
