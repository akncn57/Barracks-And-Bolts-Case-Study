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
        [SerializeField, Min(1)] private int maxHp = 100;

        [Header("Production")]
        [SerializeField] private UnitDefinition[] producibles = Array.Empty<UnitDefinition>();
        [Tooltip("Cell where new units appear, relative to the bottom-left cell of the footprint. (1, -1) is below the door of a 3 cell wide building.")]
        [SerializeField] private Vector2Int spawnOffset = new(0, -1);

        public string Id => id;
        public string DisplayName => displayName;
        public Vector2Int Size => size;
        public int MaxHp => maxHp;
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
