using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Units;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    internal static class TestDefinitions
    {
        /// <summary>A sandbox wallet: tests that aren't about prices don't have to think about them.</summary>
        public static ResourceService CreateFreeResources(IEventBus eventBus)
        {
            return new ResourceService(new SandboxService(new GameSessionSettings { Mode = GameMode.Sandbox }, eventBus), eventBus);
        }

        /// <summary>Sandbox housing: no limit, for tests that aren't about population.</summary>
        public static PopulationService CreateFreePopulation(UnitService units, BuildingService buildings, IEventBus eventBus)
        {
            return new PopulationService(new SandboxService(new GameSessionSettings { Mode = GameMode.Sandbox }, eventBus), units, buildings);
        }

        /// <summary>Destroy the result in TearDown; ScriptableObjects are not garbage collected.</summary>
        public static BuildingDefinition CreateBuilding(string id, Vector2Int size, int maxHp = 100, float visualHeight = 0f)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            definition.name = id;

            // Fields stay private so game code can't change design data; tests set them like the inspector does.
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = id;
            serialized.FindProperty("size").vector2IntValue = size;
            serialized.FindProperty("maxHp").intValue = maxHp;
            serialized.FindProperty("visualHeight").floatValue = visualHeight;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        /// <summary>A building that produces the given units at the given offset. Destroy it in TearDown.</summary>
        public static BuildingDefinition CreateProducer(string id, Vector2Int size, Vector2Int spawnOffset, params UnitDefinition[] units)
        {
            var definition = CreateBuilding(id, size);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("spawnOffset").vector2IntValue = spawnOffset;
            var producibles = serialized.FindProperty("producibles");
            producibles.arraySize = units.Length;
            for (var i = 0; i < units.Length; i++)
                producibles.GetArrayElementAtIndex(i).objectReferenceValue = units[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        /// <summary>Destroy the result in TearDown; ScriptableObjects are not garbage collected.</summary>
        public static UnitDefinition CreateUnit(string id, int maxHp = 10, int damage = 1, float attackRange = 1f, float attackCooldown = 1f)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            definition.name = id;

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = id;
            serialized.FindProperty("maxHp").intValue = maxHp;
            serialized.FindProperty("damage").intValue = damage;
            serialized.FindProperty("attackRange").floatValue = attackRange;
            serialized.FindProperty("attackCooldown").floatValue = attackCooldown;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }
    }
}
