using System.Collections.Generic;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Shows one unit: follows its model and plays idle or run frames. The root sits on the unit's feet;
    /// every frame's pivot is on the feet too, and a Sorting Group on the root makes the unit sort by that point.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        // Spreads units over the animation by id, so a group doesn't breathe in perfect sync. A fraction of a loop per id.
        private const float PhasePerId = 0.37f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;

        [Header("Selection")]
        [Tooltip("Ring under the feet. Hidden until the unit is selected.")]
        [SerializeField] private SpriteRenderer selectionMarker;
        [SerializeField] private Color playerMarkerColor = Color.green;
        [SerializeField] private Color enemyMarkerColor = Color.red;

        private UnitModel _unit;
        private TeamColorDefinition.UnitSpriteSet _sprites;
        private GameClock _clock;
        private float _phase;
        private IReadOnlyList<Sprite> _shownFrames;
        private int _shownFrame = -1;

        public GameEntityId UnitId => _unit.Id;

        public void Bind(UnitModel unit, TeamColorDefinition.UnitSpriteSet sprites, GameClock clock)
        {
            _unit = unit;
            _sprites = sprites;
            _clock = clock;
            _phase = unit.Id.Value * PhasePerId;
            name = $"{unit.Definition.DisplayName} ({unit.Id})";

            selectionMarker.color = unit.Faction == Faction.Player ? playerMarkerColor : enemyMarkerColor;
            SetSelected(false);
        }

        public void SetSelected(bool isSelected)
        {
            selectionMarker.enabled = isSelected;
        }

        // LateUpdate, so the model has already moved this frame.
        private void LateUpdate()
        {
            if (_unit == null)
                return;

            transform.position = _unit.Position;
            spriteRenderer.flipX = _unit.IsFacingLeft;
            ShowFrame(_unit.IsMoving ? _sprites.RunFrames : _sprites.IdleFrames);
        }

        private void ShowFrame(IReadOnlyList<Sprite> frames)
        {
            if (frames.Count == 0)
                return;

            // Follows the game clock, so units freeze on pause and speed up with the game.
            var frame = Mathf.FloorToInt(_clock.ElapsedTime * framesPerSecond + _phase * frames.Count) % frames.Count;
            if (frames == _shownFrames && frame == _shownFrame)
                return;

            _shownFrames = frames;
            _shownFrame = frame;
            spriteRenderer.sprite = frames[frame];
        }
    }
}
