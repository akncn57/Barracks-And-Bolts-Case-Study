using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>Runtime state of one placed building.</summary>
    public sealed class BuildingModel
    {
        // A building site starts with this share of its HP and gains the rest while it is built.
        private const float SiteHpShare = 0.1f;

        // HP gained by building comes in fractions; the whole points are added as they fill up.
        private float _hpToGain;

        public BuildingModel(GameEntityId id, BuildingDefinition definition, Faction faction, RectInt area)
        {
            Id = id;
            Definition = definition;
            Faction = faction;
            Area = area;
            CurrentHp = definition.MaxHp;
        }

        public GameEntityId Id { get; }
        public BuildingDefinition Definition { get; }
        public Faction Faction { get; }

        /// <summary>Cells covered by the footprint.</summary>
        public RectInt Area { get; }

        public int CurrentHp { get; private set; }
        public bool IsDead => CurrentHp <= 0;

        /// <summary>0 for a fresh building site, 1 when it stands. A site doesn't produce, house, shoot or open territory.</summary>
        public float BuildProgress { get; private set; } = 1f;
        public bool IsBuilt => BuildProgress >= 1f;

        /// <summary>The enemy this building is shooting at. None when it has no target.</summary>
        public GameEntityId DefenseTarget { get; private set; }

        /// <summary>Time until the building looks for a shot again.</summary>
        public float ShotDelayLeft { get; private set; }

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(CurrentHp - amount, 0);
        }

        /// <summary>Turns the building into a fresh site with little HP. Only right after it is placed.</summary>
        public void StartAsSite()
        {
            BuildProgress = 0f;
            CurrentHp = Mathf.Max(Mathf.RoundToInt(Definition.MaxHp * SiteHpShare), 1);
            _hpToGain = 0f;
        }

        /// <summary>Moves the work on by a share of the whole job. HP grows with it; damage taken so far stays.</summary>
        public void AddBuildProgress(float share)
        {
            if (IsBuilt || share <= 0f)
                return;

            share = Mathf.Min(share, 1f - BuildProgress);
            BuildProgress = Mathf.Min(BuildProgress + share, 1f);

            _hpToGain += Definition.MaxHp * (1f - SiteHpShare) * share;
            var whole = Mathf.FloorToInt(_hpToGain);
            _hpToGain -= whole;
            CurrentHp = Mathf.Min(CurrentHp + whole, Definition.MaxHp);
        }

        public void SetDefenseTarget(GameEntityId target)
        {
            DefenseTarget = target;
        }

        public void StartShotDelay(float seconds)
        {
            ShotDelayLeft = seconds;
        }

        public void TickShotDelay(float deltaTime)
        {
            ShotDelayLeft = Mathf.Max(ShotDelayLeft - deltaTime, 0f);
        }
    }
}
