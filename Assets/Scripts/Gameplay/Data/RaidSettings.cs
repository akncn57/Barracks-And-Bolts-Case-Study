using System;
using System.Collections.Generic;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>The calendar of a normal game and the enemies of every raid. Sandbox mode doesn't use this.</summary>
    [CreateAssetMenu(fileName = "Data_Config_Raids", menuName = "Barracks & Bolts/Config/Raids")]
    public sealed class RaidSettings : ScriptableObject
    {
        [Serializable]
        public struct UnitGroup
        {
            [SerializeField] private UnitDefinition unit;
            [SerializeField, Min(0)] private int count;

            public UnitDefinition Unit => unit;
            public int Count => count;
        }

        [Serializable]
        public sealed class Wave
        {
            [SerializeField] private UnitGroup[] groups = Array.Empty<UnitGroup>();

            public IReadOnlyList<UnitGroup> Groups => groups;
        }

        [Serializable]
        public sealed class Raid
        {
            [SerializeField, Min(1)] private int day = 1;
            [SerializeField] private Wave[] waves = Array.Empty<Wave>();

            public int Day => day;
            public IReadOnlyList<Wave> Waves => waves;
        }

        [Header("Calendar")]
        [SerializeField, Min(1f)] private float daySeconds = 120f;
        [SerializeField, Min(1f)] private float nightSeconds = 60f;

        [Header("Raids")]
        [Tooltip("A raid starts somewhere between these two points of its day. 0 is dawn, 1 is the end of the night.")]
        [SerializeField, Range(0f, 1f)] private float earliestStart = 0.2f;
        [SerializeField, Range(0f, 1f)] private float latestStart = 0.8f;
        [Tooltip("Seconds between the end of one wave and the next one.")]
        [SerializeField, Min(0f)] private float waveDelay = 8f;
        [Tooltip("Enemies appear on the coast at least this far from the Castle. In cells.")]
        [SerializeField, Min(0f)] private float minSpawnDistance = 20f;
        [Tooltip("Surviving the raid of the last day wins the game.")]
        [SerializeField] private Raid[] raids = Array.Empty<Raid>();

        [Header("Fighting")]
        [Tooltip("How far units notice something to attack. In cells.")]
        [SerializeField, Min(0f)] private float aggroRange = 5f;
        [Tooltip("A villager runs to the Castle when an enemy comes this close. In cells. Keep it above the aggro range.")]
        [SerializeField, Min(0f)] private float shelterRadius = 7f;

        public float DaySeconds => daySeconds;
        public float NightSeconds => nightSeconds;
        public float DayLength => daySeconds + nightSeconds;
        public float EarliestStart => earliestStart;
        public float LatestStart => Mathf.Max(earliestStart, latestStart);
        public float WaveDelay => waveDelay;
        public float MinSpawnDistance => minSpawnDistance;
        public IReadOnlyList<Raid> Raids => raids;
        public float AggroRange => aggroRange;
        public float ShelterRadius => shelterRadius;

        public int LastDay
        {
            get
            {
                var last = 0;
                foreach (var raid in raids)
                    last = Mathf.Max(last, raid.Day);

                return last;
            }
        }

        /// <summary>The raid of that day, or null if the day is peaceful.</summary>
        public Raid GetRaid(int day)
        {
            foreach (var raid in raids)
            {
                if (raid.Day == day)
                    return raid;
            }

            return null;
        }
    }
}
