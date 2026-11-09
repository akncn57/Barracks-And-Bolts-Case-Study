using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Core.Simulation;
using UnityEngine;
using Random = System.Random;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>
    /// Makes living resources like sheep potter around their cell. Only the look moves: the grid cell stays the same,
    /// so nothing has to find a new path because of a sheep.
    /// </summary>
    public sealed class ResourceRoamSystem : ISimulationSystem
    {
        private const float WalkSpeed = 0.5f;
        private const float MinRest = 1.5f;
        private const float MaxRest = 5f;

        private readonly ResourceNodeService _nodes;
        private readonly Random _random;

        public ResourceRoamSystem(ResourceNodeService nodes, GameSessionSettings session)
        {
            _nodes = nodes;
            _random = new Random(session.Seed);
        }

        public SimulationPhase Phase => SimulationPhase.Movement;

        public void Tick(float deltaTime)
        {
            foreach (var node in _nodes.Nodes)
            {
                var radius = node.Definition.RoamRadius;
                if (radius > 0f && node.TickRoam(deltaTime, WalkSpeed))
                    node.WalkTo(PickGoal(radius), Next(MinRest, MaxRest));
            }
        }

        // The offset starts at the bottom middle of the cell, so sideways goes both ways and up goes one way.
        private Vector2 PickGoal(float radius)
        {
            return new Vector2(Next(-radius, radius), Next(0f, radius * 2f));
        }

        private float Next(float min, float max)
        {
            return min + (float)_random.NextDouble() * (max - min);
        }
    }
}
