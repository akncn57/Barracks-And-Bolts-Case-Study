using System;
using System.Collections.Generic;
using BarracksAndBolts.Utils.Collections;
using NUnit.Framework;

namespace BarracksAndBolts.Tests.EditMode.Utils
{
    public class MinHeapTests
    {
        [Test]
        public void Pop_ReturnsItemsInPriorityOrder()
        {
            var heap = new MinHeap<string>();
            heap.Push("c", 3f);
            heap.Push("a", 1f);
            heap.Push("d", 4f);
            heap.Push("b", 2f);

            var popped = new List<string>();
            while (heap.Count > 0)
                popped.Add(heap.Pop());

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, popped);
        }

        [Test]
        public void Pop_EqualPriority_UsesTieBreaker()
        {
            var heap = new MinHeap<string>();
            heap.Push("far", 5f, 3f);
            heap.Push("near", 5f, 1f);

            Assert.AreEqual("near", heap.Pop());
        }

        [Test]
        public void Push_BeyondInitialCapacity_Grows()
        {
            var heap = new MinHeap<int>(2);
            for (var i = 10; i > 0; i--)
                heap.Push(i, i);

            Assert.AreEqual(10, heap.Count);
            Assert.AreEqual(1, heap.Pop());
        }

        [Test]
        public void Clear_EmptiesHeap()
        {
            var heap = new MinHeap<int>();
            heap.Push(1, 1f);

            heap.Clear();

            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Pop_Empty_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new MinHeap<int>().Pop());
        }
    }
}
