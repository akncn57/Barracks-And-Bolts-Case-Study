using BarracksAndBolts.Gameplay.Data;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    internal static class TestDefinitions
    {
        /// <summary>Destroy the result in TearDown; ScriptableObjects are not garbage collected.</summary>
        public static BuildingDefinition CreateBuilding(string id, Vector2Int size, int maxHp = 100)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            definition.name = id;

            // Fields stay private so game code can't change design data; tests set them like the inspector does.
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = id;
            serialized.FindProperty("size").vector2IntValue = size;
            serialized.FindProperty("maxHp").intValue = maxHp;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }
    }
}
