using System.Collections.Generic;
using System.Linq;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Gameplay.Pathfinding;
using BarracksAndBolts.Gameplay.ResourceNodes;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BarracksAndBolts.Tests.EditMode.Gameplay
{
    public class ResourceDistributorTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var asset in _created)
                Object.DestroyImmediate(asset);

            _created.Clear();
        }

        private ResourceNodeDefinition CreateNode(string id, Vector2Int size)
        {
            var definition = ScriptableObject.CreateInstance<ResourceNodeDefinition>();
            _created.Add(definition);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("size").vector2IntValue = size;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private ResourceDistributionSettings CreateSettings(float clearRadius, float startRadius, int rampClearance,
            ResourceNodeDefinition definition, int deposits, int nodesPerDeposit, float spacing = 3f, bool hasStartDeposit = false)
        {
            var settings = ScriptableObject.CreateInstance<ResourceDistributionSettings>();
            _created.Add(settings);

            var serialized = new SerializedObject(settings);
            serialized.FindProperty("startClearRadius").floatValue = clearRadius;
            serialized.FindProperty("startDepositRadius").floatValue = startRadius;
            serialized.FindProperty("rampClearance").intValue = rampClearance;

            var groups = serialized.FindProperty("groups");
            groups.arraySize = 1;
            var group = groups.GetArrayElementAtIndex(0);
            var definitions = group.FindPropertyRelative("definitions");
            definitions.arraySize = 1;
            definitions.GetArrayElementAtIndex(0).objectReferenceValue = definition;
            group.FindPropertyRelative("depositCount").intValue = deposits;
            group.FindPropertyRelative("minNodesPerDeposit").intValue = nodesPerDeposit;
            group.FindPropertyRelative("maxNodesPerDeposit").intValue = nodesPerDeposit;
            group.FindPropertyRelative("minSpacing").floatValue = spacing;
            group.FindPropertyRelative("hasStartDeposit").boolValue = hasStartDeposit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return settings;
        }

        private static List<RectInt> Distribute(GridMap map, ResourceDistributionSettings settings, int seed)
        {
            var service = new ResourceNodeService(map, new GameEntityIdGenerator(), new EventBus());
            new ResourceDistributor(map, new GridSearch(map), service, settings).Distribute(seed);
            return service.Nodes.Select(node => node.Area).ToList();
        }

        private static GridMap OpenMap(int size = 40)
        {
            return new GridMap(size, size, Vector2.zero);
        }

        [Test]
        public void Distribute_SameSeed_GivesSameMap()
        {
            var settings = CreateSettings(4f, 12f, 2, CreateNode("tree", Vector2Int.one), 6, 5);

            CollectionAssert.AreEqual(Distribute(OpenMap(), settings, 7), Distribute(OpenMap(), settings, 7));
        }

        [Test]
        public void Distribute_DifferentSeed_GivesDifferentMap()
        {
            var settings = CreateSettings(4f, 12f, 2, CreateNode("tree", Vector2Int.one), 6, 5);

            CollectionAssert.AreNotEqual(Distribute(OpenMap(), settings, 7), Distribute(OpenMap(), settings, 8));
        }

        [Test]
        public void Distribute_OpenMap_PlacesRequestedNodes()
        {
            var settings = CreateSettings(4f, 12f, 2, CreateNode("tree", Vector2Int.one), 4, 3);

            Assert.AreEqual(12, Distribute(OpenMap(), settings, 1).Count);
        }

        [Test]
        public void Distribute_NodesTakeGridCells()
        {
            var map = OpenMap();
            var settings = CreateSettings(4f, 12f, 2, CreateNode("mine", new Vector2Int(2, 1)), 3, 1);

            foreach (var area in Distribute(map, settings, 3))
            {
                Assert.AreEqual(new Vector2Int(2, 1), area.size);
                Assert.IsFalse(map.IsWalkable(area.position));
            }
        }

        [Test]
        public void Distribute_KeepsStartAreaClear()
        {
            var settings = CreateSettings(10f, 16f, 2, CreateNode("tree", Vector2Int.one), 12, 8);

            var areas = Distribute(OpenMap(), settings, 5);

            Assert.IsNotEmpty(areas);
            foreach (var area in areas)
                Assert.GreaterOrEqual(Vector2.Distance(area.center, new Vector2(20f, 20f)), 10f, $"Too close: {area}");
        }

        [Test]
        public void Distribute_StartDeposit_IsNearTheMiddle()
        {
            var settings = CreateSettings(6f, 10f, 2, CreateNode("mine", Vector2Int.one), 1, 1, hasStartDeposit: true);

            for (var seed = 0; seed < 10; seed++)
            {
                var area = Distribute(OpenMap(), settings, seed).Single();
                Assert.LessOrEqual(Vector2.Distance(area.center, new Vector2(20f, 20f)), 11f);
            }
        }

        [Test]
        public void Distribute_KeepsCellsAroundRampFree()
        {
            var map = OpenMap(16);
            var ramp = new Vector2Int(2, 2);
            map.SetRamp(ramp, true);
            var settings = CreateSettings(0f, 16f, 2, CreateNode("tree", Vector2Int.one), 30, 6, 1f);

            var areas = Distribute(map, settings, 11);

            Assert.IsNotEmpty(areas);
            foreach (var area in areas)
            {
                var distance = Mathf.Max(Mathf.Abs(area.x - ramp.x), Mathf.Abs(area.y - ramp.y));
                Assert.Greater(distance, 2, $"Too close to the ramp: {area}");
            }
        }

        [Test]
        public void Distribute_NeverCutsMapApart()
        {
            // A one cell wide corridor joins the two halves. Trees must leave it open.
            var settings = CreateSettings(0f, 16f, 0, CreateNode("tree", Vector2Int.one), 20, 4, 1f);

            for (var seed = 0; seed < 20; seed++)
            {
                var map = TestMaps.FromRows(
                    ".....#.....",
                    ".....#.....",
                    "...........",
                    ".....#.....",
                    ".....#.....");
                var nodeCount = Distribute(map, settings, seed).Count;

                // 55 cells, 4 of them water.
                Assert.AreEqual(51 - nodeCount, CountReachable(map), $"Seed {seed} cut the map.");
            }
        }

        [Test]
        public void Distribute_MapWithoutWalkableCells_DoesNothing()
        {
            var map = TestMaps.FromRows("##", "##");
            var settings = CreateSettings(0f, 4f, 0, CreateNode("tree", Vector2Int.one), 3, 3);

            Assert.IsEmpty(Distribute(map, settings, 1));
        }

        private static int CountReachable(GridMap map)
        {
            var first = new Vector2Int(-1, -1);
            foreach (var cell in map.Bounds.allPositionsWithin)
            {
                if (map.IsWalkable(cell))
                {
                    first = cell;
                    break;
                }
            }

            var seen = new HashSet<Vector2Int> { first };
            var open = new Queue<Vector2Int>(seen);
            while (open.Count > 0)
            {
                var cell = open.Dequeue();
                foreach (var offset in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    var next = cell + offset;
                    if (map.IsWalkable(next) && seen.Add(next))
                        open.Enqueue(next);
                }
            }

            return seen.Count;
        }
    }
}
