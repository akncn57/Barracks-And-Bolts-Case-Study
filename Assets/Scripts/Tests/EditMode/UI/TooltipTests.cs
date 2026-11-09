using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Tests.EditMode.Gameplay;
using BarracksAndBolts.UI.Tooltips;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BarracksAndBolts.Tests.EditMode.UI
{
    public class TooltipTests
    {
        private static readonly Rect Area = new(0f, 0f, 1000f, 600f);
        private static readonly Vector2 Size = new(200f, 100f);

        [Test]
        public void AnchorOnTheLeft_GoesToItsRight()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(100f, 250f, 50f, 50f), Size, Area, 10f);

            Assert.AreEqual(new Vector2(160f, 225f), position);
        }

        [Test]
        public void AnchorOnTheRight_GoesToItsLeft()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(800f, 250f, 50f, 50f), Size, Area, 10f);

            Assert.AreEqual(new Vector2(590f, 225f), position);
        }

        [Test]
        public void AnchorAtTheTop_StaysInsideTheArea()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(100f, 580f, 50f, 20f), Size, Area, 10f);

            Assert.AreEqual(500f, position.y);
        }

        [Test]
        public void AnchorAtTheBottom_StaysInsideTheArea()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(100f, 0f, 50f, 20f), Size, Area, 10f);

            Assert.AreEqual(0f, position.y);
        }

        [Test]
        public void NoRoomOnTheSide_StaysInsideTheArea()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(100f, 250f, 400f, 50f), new Vector2(600f, 100f), Area, 10f);

            Assert.AreEqual(400f, position.x);
        }

        [Test]
        public void TooltipBiggerThanTheArea_StartsAtTheEdge()
        {
            var position = TooltipPlacement.GetBottomLeft(new Rect(100f, 250f, 50f, 50f), new Vector2(2000f, 900f), Area, 10f);

            Assert.AreEqual(Vector2.zero, position);
        }

        [Test]
        public void Building_ShowsSizeAndHp()
        {
            var house = TestDefinitions.CreateBuilding("house", new Vector2Int(2, 2), maxHp: 50);

            Assert.AreEqual("Size: 2x2\nHP: 50", TooltipText.ForBuilding(house));
            Assert.AreEqual("Size: 2x2\nHP: 50", TooltipText.ForBuilding(house, isNormalMode: true), "No price, no cost line.");
            Object.DestroyImmediate(house);
        }

        [Test]
        public void ProductionBuilding_ListsItsUnits()
        {
            var knight = TestDefinitions.CreateUnit("Knight");
            var archer = TestDefinitions.CreateUnit("Archer");
            var barracks = TestDefinitions.CreateProducer("barracks", new Vector2Int(3, 3), Vector2Int.zero, knight, archer);

            StringAssert.EndsWith("Produces: Knight, Archer", TooltipText.ForBuilding(barracks));

            Object.DestroyImmediate(barracks);
            Object.DestroyImmediate(knight);
            Object.DestroyImmediate(archer);
        }

        [Test]
        public void Unit_ShowsItsStats()
        {
            var archer = TestDefinitions.CreateUnit("archer", maxHp: 10, damage: 2, attackRange: 5f);

            var text = TooltipText.ForUnit(archer);

            StringAssert.Contains("HP: 10", text);
            StringAssert.Contains("Damage: 2", text);
            StringAssert.Contains("Range: 5", text);
            Object.DestroyImmediate(archer);
        }
    }
}
