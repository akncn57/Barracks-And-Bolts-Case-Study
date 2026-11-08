using System;
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

        [Serializable]
        private struct BuildingSprite
        {
            [SerializeField] private BuildingDefinition building;
            [SerializeField] private Sprite sprite;

            public BuildingDefinition Building => building;
            public Sprite Sprite => sprite;
        }
    }
}
