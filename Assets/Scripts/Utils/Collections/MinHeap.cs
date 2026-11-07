using System;

namespace BarracksAndBolts.Utils.Collections
{
    /// <summary>
    /// Binary min-heap ordered by priority, then by tie breaker. Used as the A* open list.
    /// .NET Standard 2.1 has no PriorityQueue, so this is our own small implementation.
    /// </summary>
    public sealed class MinHeap<T>
    {
        private struct Node
        {
            public T Item;
            public float Priority;
            public float TieBreaker;
        }

        private Node[] _nodes;

        public MinHeap(int capacity = 16)
        {
            _nodes = new Node[Math.Max(1, capacity)];
        }

        public int Count { get; private set; }

        public void Push(T item, float priority, float tieBreaker = 0f)
        {
            if (Count == _nodes.Length)
                Array.Resize(ref _nodes, _nodes.Length * 2);

            _nodes[Count] = new Node { Item = item, Priority = priority, TieBreaker = tieBreaker };
            SiftUp(Count);
            Count++;
        }

        public T Pop()
        {
            if (Count == 0)
                throw new InvalidOperationException("Cannot pop from an empty heap.");

            var top = _nodes[0].Item;
            Count--;
            _nodes[0] = _nodes[Count];
            _nodes[Count] = default;

            if (Count > 0)
                SiftDown(0);

            return top;
        }

        /// <summary>Empties the heap but keeps its memory for the next use.</summary>
        public void Clear()
        {
            // Clearing the slots releases references, so a heap of objects doesn't keep them alive.
            Array.Clear(_nodes, 0, Count);
            Count = 0;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                var parent = (index - 1) / 2;
                if (!IsLess(index, parent))
                    return;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            while (true)
            {
                var left = index * 2 + 1;
                var right = left + 1;
                var smallest = index;

                if (left < Count && IsLess(left, smallest))
                    smallest = left;
                if (right < Count && IsLess(right, smallest))
                    smallest = right;

                if (smallest == index)
                    return;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private bool IsLess(int a, int b)
        {
            var nodeA = _nodes[a];
            var nodeB = _nodes[b];
            if (nodeA.Priority != nodeB.Priority)
                return nodeA.Priority < nodeB.Priority;

            return nodeA.TieBreaker < nodeB.TieBreaker;
        }

        private void Swap(int a, int b)
        {
            (_nodes[a], _nodes[b]) = (_nodes[b], _nodes[a]);
        }
    }
}
