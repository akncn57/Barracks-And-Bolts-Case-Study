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
        [SerializeField] private UnitSpriteSet[] unitSprites = Array.Empty<UnitSpriteSet>();

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

        /// <returns>The grey look of the building while it is a site, or its normal sprite if there is none.</returns>
        public Sprite GetBuildingSiteSprite(BuildingDefinition building)
        {
            foreach (var entry in buildingSprites)
            {
                if (entry.Building == building)
                    return entry.SiteSprite != null ? entry.SiteSprite : entry.Sprite;
            }

            return GetBuildingSprite(building);
        }

        /// <returns>The unit's portrait in this color, or the unit's own portrait if the set has none.</returns>
        public Sprite GetUnitPortrait(UnitDefinition unit)
        {
            foreach (var entry in unitSprites)
            {
                if (entry.Unit == unit && entry.Portrait != null)
                    return entry.Portrait;
            }

            return unit != null ? unit.Portrait : null;
        }

        /// <returns>The unit's animations, or an empty set if this color has none for the unit.</returns>
        public UnitSpriteSet GetUnitSprites(UnitDefinition unit)
        {
            foreach (var entry in unitSprites)
            {
                if (entry.Unit == unit)
                    return entry;
            }

            Debug.LogWarning($"{name}: no sprites for {(unit != null ? unit.name : "null")}.", this);
            return default;
        }

        [Serializable]
        private struct BuildingSprite
        {
            [SerializeField] private BuildingDefinition building;
            [SerializeField] private Sprite sprite;
            [Tooltip("Grey version shown while the building is a site. Empty for buildings that are never built by villagers.")]
            [SerializeField] private Sprite siteSprite;

            public BuildingDefinition Building => building;
            public Sprite Sprite => sprite;
            public Sprite SiteSprite => siteSprite;
        }

        [Serializable]
        public struct UnitSpriteSet
        {
            [SerializeField] private UnitDefinition unit;
            [Tooltip("Picture in panels and menus. Empty uses the unit's own portrait.")]
            [SerializeField] private Sprite portrait;
            [SerializeField] private Sprite[] idleFrames;
            [SerializeField] private Sprite[] runFrames;
            [SerializeField] private Sprite[] attackFrames;
            [Tooltip("Work on trees and sheep. Empty for units that don't gather.")]
            [SerializeField] private Sprite[] chopFrames;
            [Tooltip("Work on stone and gold. Empty for units that don't gather.")]
            [SerializeField] private Sprite[] mineFrames;
            [Tooltip("Hammer work on building sites. Empty for units that don't build.")]
            [SerializeField] private Sprite[] buildFrames;

            public UnitDefinition Unit => unit;
            public Sprite Portrait => portrait;
            public IReadOnlyList<Sprite> BuildFrames => buildFrames ?? Array.Empty<Sprite>();

            // The default set of a missing unit has null arrays; readers always get a list.
            public IReadOnlyList<Sprite> IdleFrames => idleFrames ?? Array.Empty<Sprite>();
            public IReadOnlyList<Sprite> RunFrames => runFrames ?? Array.Empty<Sprite>();
            public IReadOnlyList<Sprite> AttackFrames => attackFrames ?? Array.Empty<Sprite>();

            public IReadOnlyList<Sprite> GetWorkFrames(ResourceKind kind)
            {
                var isMining = kind == ResourceKind.Stone || kind == ResourceKind.Gold;
                return (isMining ? mineFrames : chopFrames) ?? Array.Empty<Sprite>();
            }
        }
    }
}
