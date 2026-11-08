using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>How many resources the map gets and where they may go.</summary>
    [CreateAssetMenu(fileName = "Data_Config_ResourceDistribution", menuName = "Barracks & Bolts/Config/Resource Distribution")]
    public sealed class ResourceDistributionSettings : ScriptableObject
    {
        [Serializable]
        public sealed class Group
        {
            [Tooltip("A node is picked at random from these. All of them should have the same size.")]
            [SerializeField] private ResourceNodeDefinition[] definitions = Array.Empty<ResourceNodeDefinition>();
            [Tooltip("A deposit is one forest, one quarry or one flock.")]
            [SerializeField, Min(0)] private int depositCount = 1;
            [SerializeField, Min(1)] private int minNodesPerDeposit = 1;
            [SerializeField, Min(1)] private int maxNodesPerDeposit = 1;
            [Tooltip("Smallest distance between the centers of two deposits of this group.")]
            [SerializeField, Min(0f)] private float minSpacing = 8f;
            [Tooltip("The first deposit goes close to the start area, so every game starts with this resource nearby.")]
            [SerializeField] private bool hasStartDeposit;

            public IReadOnlyList<ResourceNodeDefinition> Definitions => definitions;
            public int DepositCount => depositCount;
            public int MinNodesPerDeposit => minNodesPerDeposit;
            public int MaxNodesPerDeposit => Mathf.Max(minNodesPerDeposit, maxNodesPerDeposit);
            public float MinSpacing => minSpacing;
            public bool HasStartDeposit => hasStartDeposit;
        }

        [Tooltip("No resources this close to the middle of the map. The Castle and the first buildings go there.")]
        [SerializeField, Min(0f)] private float startClearRadius = 8f;
        [Tooltip("Start deposits go between the clear radius and this one.")]
        [SerializeField, Min(0f)] private float startDepositRadius = 16f;
        [Tooltip("Cells kept free around stairs, so units can always get on and off them.")]
        [SerializeField, Min(0)] private int rampClearance = 2;
        [Tooltip("Placed in this order. Put the rare resources first, so the forests can't take their space.")]
        [SerializeField] private Group[] groups = Array.Empty<Group>();

        public float StartClearRadius => startClearRadius;
        public float StartDepositRadius => startDepositRadius;
        public int RampClearance => rampClearance;
        public IReadOnlyList<Group> Groups => groups;
    }
}
