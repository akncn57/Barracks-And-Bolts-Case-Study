using System;
using System.Collections.Generic;
using BarracksAndBolts.Core.Session;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Data
{
    /// <summary>
    /// Entry point to all design data. Registered once in the gameplay scope, so systems ask for this
    /// instead of holding their own asset references.
    /// </summary>
    [CreateAssetMenu(fileName = "Data_Database_Game", menuName = "Barracks & Bolts/Data/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        [Tooltip("The production menu shows the buildings in this order.")]
        [SerializeField] private BuildingDefinition[] buildings = Array.Empty<BuildingDefinition>();
        [SerializeField] private UnitDefinition[] units = Array.Empty<UnitDefinition>();
        [SerializeField] private TeamColorDefinition[] teamColors = Array.Empty<TeamColorDefinition>();
        [SerializeField] private ResourceDistributionSettings resourceDistribution;

        public IReadOnlyList<BuildingDefinition> Buildings => buildings;
        public IReadOnlyList<UnitDefinition> Units => units;
        public ResourceDistributionSettings ResourceDistribution => resourceDistribution;

        public TeamColorDefinition GetTeamColor(TeamColor color)
        {
            foreach (var definition in teamColors)
            {
                if (definition != null && definition.Color == color)
                    return definition;
            }

            throw new InvalidOperationException($"{name}: there is no team color set for {color}.");
        }

        private void OnValidate()
        {
            WarnAboutEmptySlots(buildings, nameof(buildings));
            WarnAboutEmptySlots(units, nameof(units));
            WarnAboutEmptySlots(teamColors, nameof(teamColors));
            WarnAboutDuplicateIds();
        }

        private void WarnAboutEmptySlots<T>(T[] entries, string listName) where T : UnityEngine.Object
        {
            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i] == null)
                    Debug.LogWarning($"{name}: {listName} slot {i} is empty.", this);
            }
        }

        private void WarnAboutDuplicateIds()
        {
            // Ids are save file keys, so two definitions with the same id would load as the wrong type.
            var ids = new HashSet<string>();
            foreach (var building in buildings)
            {
                if (building != null && !ids.Add(building.Id))
                    Debug.LogWarning($"{name}: building id '{building.Id}' is used more than once.", this);
            }

            ids.Clear();
            foreach (var unit in units)
            {
                if (unit != null && !ids.Add(unit.Id))
                    Debug.LogWarning($"{name}: unit id '{unit.Id}' is used more than once.", this);
            }
        }
    }
}
