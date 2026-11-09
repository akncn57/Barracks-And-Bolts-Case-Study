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

        [Header("Building site")]
        [Tooltip("Hidden child that shows how far the work has come.")]
        [SerializeField] private BuildProgressBarView progressBar;
        [Tooltip("Tint of the grey site sprite nobody has worked on yet.")]
        [SerializeField] private Color siteStartColor = new(0.85f, 0.85f, 0.85f, 0.7f);
        [Tooltip("Tint just before the site is finished. The colored sprite comes when it stands.")]
        [SerializeField] private Color siteEndColor = new(1f, 1f, 1f, 0.95f);

        private readonly DamageFlash _damageFlash = new();
        private BuildingModel _building;
        private BuildingDefinition _definition;
        private Sprite _sprite;
        private Sprite _siteSprite;
        private Vector2 _footprintCenter;
        private bool _opensTerritory;

        public GameEntityId BuildingId { get; private set; }

        /// <param name="siteSprite">Grey look while the building is a site.</param>
        /// <param name="opensTerritory">False in sandbox mode and for the enemy, who have no territory to show.</param>
        public void Bind(BuildingModel building, Sprite sprite, Sprite siteSprite, Vector2 position, bool opensTerritory)
        {
            BuildingId = building.Id;
            name = $"{building.Definition.DisplayName} ({building.Id})";
            transform.position = position;

            _building = building;
            _definition = building.Definition;
            _sprite = sprite;
            _siteSprite = siteSprite;

            if (!building.IsBuilt)
                progressBar.Show(building.Area.width, _definition.VisualHeight);

            // Right away, so a new site doesn't show in full color for its first frame.
            UpdateLook();
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
            if (_building == null)
                return;

            UpdateLook();
        }

        private void UpdateLook()
        {
            var shownSprite = _building.IsBuilt ? _sprite : _siteSprite;
            if (spriteRenderer.sprite != shownSprite)
                BuildingSpriteLayout.PlaceOnFootprint(spriteRenderer, shownSprite);

            if (progressBar.isActiveAndEnabled)
            {
                if (_building.IsBuilt)
                    progressBar.Hide();
                else
                    progressBar.SetProgress(_building.BuildProgress);
            }

            var restColor = GetRestColor();
            _damageFlash.Update(spriteRenderer, Time.deltaTime, restColor);
            if (!_damageFlash.IsActive && spriteRenderer.color != restColor)
                spriteRenderer.color = restColor;
        }

        // A site is see-through and gets solid as the villagers work, so the progress shows on the map.
        private Color GetRestColor()
        {
            return _building.IsBuilt ? Color.white : Color.Lerp(siteStartColor, siteEndColor, _building.BuildProgress);
        }
    }
}
