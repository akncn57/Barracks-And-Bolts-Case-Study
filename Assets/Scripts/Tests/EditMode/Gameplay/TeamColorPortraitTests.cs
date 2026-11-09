using BarracksAndBolts.Gameplay.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    /// <summary>Each color set gives units their own portrait, so an enemy looks like one in panels and menus.</summary>
    public class TeamColorPortraitTests
    {
        private UnitDefinition _knight;
        private UnitDefinition _archer;
        private TeamColorDefinition _yellow;
        private Texture2D _texture;
        private Sprite _ownPortrait;
        private Sprite _yellowPortrait;

        [SetUp]
        public void SetUp()
        {
            _texture = new Texture2D(4, 4);
            _ownPortrait = Sprite.Create(_texture, new Rect(0, 0, 4, 4), Vector2.zero);
            _yellowPortrait = Sprite.Create(_texture, new Rect(0, 0, 2, 2), Vector2.zero);

            _knight = TestDefinitions.CreateUnit("knight");
            _archer = TestDefinitions.CreateUnit("archer");
            SetPortrait(_knight, _ownPortrait);
            SetPortrait(_archer, _ownPortrait);

            // The yellow set has a portrait for the knight only.
            _yellow = ScriptableObject.CreateInstance<TeamColorDefinition>();
            var serialized = new SerializedObject(_yellow);
            var units = serialized.FindProperty("unitSprites");
            units.arraySize = 2;
            units.GetArrayElementAtIndex(0).FindPropertyRelative("unit").objectReferenceValue = _knight;
            units.GetArrayElementAtIndex(0).FindPropertyRelative("portrait").objectReferenceValue = _yellowPortrait;
            units.GetArrayElementAtIndex(1).FindPropertyRelative("unit").objectReferenceValue = _archer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_yellow);
            Object.DestroyImmediate(_knight);
            Object.DestroyImmediate(_archer);
            Object.DestroyImmediate(_ownPortrait);
            Object.DestroyImmediate(_yellowPortrait);
            Object.DestroyImmediate(_texture);
        }

        private static void SetPortrait(UnitDefinition unit, Sprite portrait)
        {
            var serialized = new SerializedObject(unit);
            serialized.FindProperty("portrait").objectReferenceValue = portrait;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void ColorSetPortrait_IsUsed()
        {
            Assert.AreEqual(_yellowPortrait, _yellow.GetUnitPortrait(_knight));
        }

        [Test]
        public void NoColorSetPortrait_FallsBackToTheUnitsOwn()
        {
            Assert.AreEqual(_ownPortrait, _yellow.GetUnitPortrait(_archer), "In the set, but without a portrait.");

            var spearman = TestDefinitions.CreateUnit("spearman");
            SetPortrait(spearman, _ownPortrait);
            Assert.AreEqual(_ownPortrait, _yellow.GetUnitPortrait(spearman), "Not in the set at all.");
            Object.DestroyImmediate(spearman);
        }
    }
}
