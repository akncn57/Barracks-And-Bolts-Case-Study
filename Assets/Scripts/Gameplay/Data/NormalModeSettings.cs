using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>How a normal game starts and how fast villagers gather. Sandbox mode doesn't use this.</summary>
    [CreateAssetMenu(fileName = "Data_Config_NormalMode", menuName = "Barracks & Bolts/Config/Normal Mode")]
    public sealed class NormalModeSettings : ScriptableObject
    {
        [Header("Start")]
        [Tooltip("Placed in the middle of the map. The player can't build another one.")]
        [SerializeField] private BuildingDefinition castle;
        [SerializeField] private UnitDefinition villager;
        [SerializeField, Min(0)] private int startingVillagers = 3;
        [SerializeField] private ResourceCost[] startingResources = Array.Empty<ResourceCost>();

        [Header("Gathering")]
        [Tooltip("How much a villager brings back in one trip.")]
        [SerializeField, Min(1)] private int carryCapacity = 10;
        [Tooltip("Seconds of work to fill the villager's hands.")]
        [SerializeField, Min(0.1f)] private float harvestDuration = 4f;
        [Tooltip("Seconds of work on something that goes down in one cut, like a sheep.")]
        [SerializeField, Min(0.1f)] private float oneCutDuration = 0.6f;
        [Tooltip("When a node runs out, how far the villager looks for another one of the same kind. In cells.")]
        [SerializeField, Min(0f)] private float nextNodeRadius = 12f;

        [Header("Sheep")]
        [Tooltip("New sheep every morning, as long as the limit allows.")]
        [SerializeField, Min(0)] private int sheepPerMorning = 3;
        [SerializeField, Min(0)] private int maxSheep = 30;

        public BuildingDefinition Castle => castle;
        public UnitDefinition Villager => villager;
        public int StartingVillagers => startingVillagers;
        public IReadOnlyList<ResourceCost> StartingResources => startingResources;
        public int CarryCapacity => carryCapacity;
        public float HarvestDuration => harvestDuration;
        public float OneCutDuration => oneCutDuration;
        public float NextNodeRadius => nextNodeRadius;
        public int SheepPerMorning => sheepPerMorning;
        public int MaxSheep => maxSheep;
    }
}
