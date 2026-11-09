using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>
    /// Design data of a building type. Sprites are not here: they depend on the team color,
    /// see <see cref="TeamColorDefinition"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Data_Building_New", menuName = "Barracks & Bolts/Data/Building")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [Tooltip("Stable key for save files. Don't change it once saves exist.")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [Tooltip("Footprint in cells. The sprite can be taller; the roof covers the cells above.")]
        [SerializeField] private Vector2Int size = new(2, 2);
        [Tooltip("How tall the building looks, in cells, counted from the bottom of the footprint. Clicks and hover use this. Never less than the footprint.")]
        [SerializeField, Min(0f)] private float visualHeight;
        [SerializeField, Min(1)] private int maxHp = 100;
        [Tooltip("Price in a normal game. Sandbox mode ignores it.")]
        [SerializeField] private ResourceCost[] cost = Array.Empty<ResourceCost>();
        [Tooltip("Villagers can bring what they gather here.")]
        [SerializeField] private bool isDropOff;
        [Tooltip("How many units this building houses in a normal game.")]
        [SerializeField, Min(0)] private int populationCapacity;

        [Header("Production")]
        [SerializeField] private UnitDefinition[] producibles = Array.Empty<UnitDefinition>();
        [Tooltip("Made here only in a normal game. The sandbox follows the case rule that only barracks produce.")]
        [SerializeField] private UnitDefinition[] normalModeProducibles = Array.Empty<UnitDefinition>();
        [Tooltip("Cell where new units appear, relative to the bottom-left cell of the footprint. (1, -1) is below the door of a 3 cell wide building.")]
        [SerializeField] private Vector2Int spawnOffset = new(0, -1);

        public string Id => id;
        public string DisplayName => displayName;
        public Vector2Int Size => size;
        public float VisualHeight => Mathf.Max(visualHeight, size.y);
        public int MaxHp => maxHp;
        public IReadOnlyList<ResourceCost> Cost => cost;
        public bool IsDropOff => isDropOff;
        public int PopulationCapacity => populationCapacity;
        public IReadOnlyList<UnitDefinition> NormalModeProducibles => normalModeProducibles;
        public IReadOnlyList<UnitDefinition> Producibles => producibles;
        public bool CanProduce => producibles.Length > 0;
        public Vector2Int SpawnOffset => spawnOffset;

        private void OnValidate()
        {
            size = Vector2Int.Max(size, Vector2Int.one);
            maxHp = Mathf.Max(maxHp, 1);
            producibles ??= Array.Empty<UnitDefinition>();

            if (string.IsNullOrWhiteSpace(id))
                Debug.LogWarning($"{name}: id is empty.", this);

            for (var i = 0; i < producibles.Length; i++)
            {
                if (producibles[i] == null)
                    Debug.LogWarning($"{name}: producible slot {i} is empty.", this);
            }
        }
    }
}
