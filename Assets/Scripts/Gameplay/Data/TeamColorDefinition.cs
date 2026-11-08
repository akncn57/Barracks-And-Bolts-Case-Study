using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Session;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>
    /// All sprites of one team color. Adding a new color only needs a new texture folder and a new set.
    /// </summary>
    [CreateAssetMenu(fileName = "Data_TeamColor_New", menuName = "Barracks & Bolts/Data/Team Color")]
    public sealed class TeamColorDefinition : ScriptableObject
    {
        [SerializeField] private TeamColor color;
        [SerializeField] private BuildingSprite[] buildingSprites = Array.Empty<BuildingSprite>();
        [SerializeField] private UnitSprites[] unitSprites = Array.Empty<UnitSprites>();

        public TeamColor Color => color;

        /// <returns>The sprite, or null if this set has none for the building.</returns>
        public Sprite GetBuildingSprite(BuildingDefinition building)
        {
            // A handful of entries, so a linear search is cheaper than keeping a dictionary in sync.
            foreach (var entry in buildingSprites)
            {
                if (entry.Building == building)
                    return entry.Sprite;
            }

            Debug.LogWarning($"{name}: no sprite for {(building != null ? building.name : "null")}.", this);
            return null;
        }

        /// <returns>The idle animation, or an empty list if this set has none for the unit.</returns>
        public IReadOnlyList<Sprite> GetUnitIdleFrames(UnitDefinition unit)
        {
            foreach (var entry in unitSprites)
            {
                if (entry.Unit == unit)
                    return entry.IdleFrames;
            }

            Debug.LogWarning($"{name}: no sprites for {(unit != null ? unit.name : "null")}.", this);
            return Array.Empty<Sprite>();
        }

        [Serializable]
        private struct BuildingSprite
        {
            [SerializeField] private BuildingDefinition building;
            [SerializeField] private Sprite sprite;

            public BuildingDefinition Building => building;
            public Sprite Sprite => sprite;
        }

        [Serializable]
        private struct UnitSprites
        {
            [SerializeField] private UnitDefinition unit;
            [SerializeField] private Sprite[] idleFrames;

            public UnitDefinition Unit => unit;
            public IReadOnlyList<Sprite> IdleFrames => idleFrames;
        }
    }
}
