using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// Shows one building. The root sits on the bottom-center of the footprint; a Sorting Group on the root
    /// makes the building sort by its base instead of the sprite center, so units behind it are drawn behind it.
    /// </summary>
    public sealed class BuildingView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private BuildingDefenderView defender;

        [Header("Shown while selected")]
        [SerializeField] private CoverageRingView defenseRing;
        [SerializeField] private CoverageRingView territoryRing;

        private readonly DamageFlash _damageFlash = new();
        private BuildingDefinition _definition;
        private Vector2 _footprintCenter;
        private bool _opensTerritory;

        public GameEntityId BuildingId { get; private set; }

        /// <param name="opensTerritory">False in sandbox mode and for the enemy, who have no territory to show.</param>
        public void Bind(BuildingModel building, Sprite sprite, Vector2 position, bool opensTerritory)
        {
            BuildingId = building.Id;
            name = $"{building.Definition.DisplayName} ({building.Id})";
            transform.position = position;
            BuildingSpriteLayout.PlaceOnFootprint(spriteRenderer, sprite);

            _definition = building.Definition;
            _footprintCenter = new Vector2(0f, building.Area.height * 0.5f);
            _opensTerritory = opensTerritory && _definition.TerritoryRadius > 0f;
        }

        public void SetSelected(bool isSelected)
        {
            SetRing(defenseRing, isSelected && _definition.HasDefense, _definition.DefenseRange);
            SetRing(territoryRing, isSelected && _opensTerritory, _definition.TerritoryRadius);
        }

        private void SetRing(CoverageRingView ring, bool isShown, float radius)
        {
            if (isShown)
                ring.Show(_footprintCenter, radius);
            else
                ring.Hide();
        }

        public void ShowDefender(TeamColorDefinition.UnitSpriteSet sprites, Vector2 localPosition, GameClock clock)
        {
            defender.Show(sprites, localPosition, clock);
        }

        public void PlayDefenderShot(Vector2 aimPoint)
        {
            defender.PlayShot(aimPoint);
        }

        public void ShowDamage()
        {
            _damageFlash.Start();
        }

        private void Update()
        {
            _damageFlash.Update(spriteRenderer, Time.deltaTime);
        }
    }
}
