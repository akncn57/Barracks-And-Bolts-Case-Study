using System.Collections.Generic;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Entities;
using BarracksAndBolts.Gameplay.Teams;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Shows one unit: follows its model and plays idle, run or attack frames. The root sits on the unit's feet;
    /// every frame's pivot is on the feet too, and a Sorting Group on the root makes the unit sort by that point.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        // Spreads units over the animation by id, so a group doesn't breathe in perfect sync. A fraction of a loop per id.
        private const float PhasePerId = 0.37f;

        // The status icon bobs a little, so it catches the eye.
        private const float IconBobHeight = 0.05f;
        private const float IconBobSpeed = 4f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;

        [Header("Selection")]
        [Tooltip("Ring under the feet. Hidden until the unit is selected.")]
        [SerializeField] private SpriteRenderer selectionMarker;
        [SerializeField] private Color playerMarkerColor = Color.green;
        [SerializeField] private Color enemyMarkerColor = Color.red;

        [Header("Status icon")]
        [Tooltip("Over the head of the unit. Hidden when there is nothing to tell.")]
        [SerializeField] private SpriteRenderer statusIcon;
        [Tooltip("A player's villager with nothing to do.")]
        [SerializeField] private Sprite idleSprite;
        [Tooltip("A player's soldier that just noticed an enemy by itself.")]
        [SerializeField] private Sprite enemySpottedSprite;

        private readonly DamageFlash _damageFlash = new();
        private UnitModel _unit;
        private TeamColorDefinition.UnitSpriteSet _sprites;
        private GameClock _clock;
        private float _phase;
        private IReadOnlyList<Sprite> _shownFrames;
        private int _shownFrame = -1;
        private float _attackStartedAt = -1f;
        private bool _isPlayerVillager;
        private bool _isPlayerSoldier;
        private Vector3 _iconRestPosition;

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

            _isPlayerVillager = unit.Faction == Faction.Player && unit.Definition.CanGather;
            _isPlayerSoldier = unit.Faction == Faction.Player && !unit.Definition.CanGather;
            _iconRestPosition = statusIcon.transform.localPosition;
            statusIcon.enabled = false;
        }

        public void SetSelected(bool isSelected)
        {
            selectionMarker.enabled = isSelected;
        }

        public void PlayAttack()
        {
            _attackStartedAt = _clock.ElapsedTime;
        }

        public void ShowDamage()
        {
            _damageFlash.Start();
        }

        // LateUpdate, so the model has already moved this frame.
        private void LateUpdate()
        {
            if (_unit == null)
                return;

            transform.position = _unit.Position;
            spriteRenderer.flipX = _unit.IsFacingLeft;
            _damageFlash.Update(spriteRenderer, Time.deltaTime);
            UpdateStatusIcon();

            if (TryShowAttackFrame())
                return;

            if (_unit.HarvestingKind.HasValue)
                ShowLoopingFrame(_sprites.GetWorkFrames(_unit.HarvestingKind.Value));
            else
                ShowLoopingFrame(_unit.IsMoving ? _sprites.RunFrames : _sprites.IdleFrames);
        }

        private void UpdateStatusIcon()
        {
            var sprite = PickStatusSprite();
            statusIcon.enabled = sprite != null;
            if (sprite == null)
                return;

            statusIcon.sprite = sprite;
            statusIcon.transform.localPosition = _iconRestPosition + Vector3.up * (Mathf.Sin(_clock.ElapsedTime * IconBobSpeed) * IconBobHeight);
        }

        private Sprite PickStatusSprite()
        {
            if (_isPlayerVillager && _unit.IsIdle)
                return idleSprite;

            if (_isPlayerSoldier && _unit.HasJustSpottedEnemy)
                return enemySpottedSprite;

            return null;
        }

        private bool TryShowAttackFrame()
        {
            if (_attackStartedAt < 0f)
                return false;

            var frames = _sprites.AttackFrames;
            var frame = Mathf.FloorToInt((_clock.ElapsedTime - _attackStartedAt) * framesPerSecond);
            if (frame >= frames.Count)
            {
                _attackStartedAt = -1f;
                return false;
            }

            ShowFrame(frames, frame);
            return true;
        }

        private void ShowLoopingFrame(IReadOnlyList<Sprite> frames)
        {
            if (frames.Count == 0)
                return;

            // Follows the game clock, so units freeze on pause and speed up with the game.
            ShowFrame(frames, Mathf.FloorToInt(_clock.ElapsedTime * framesPerSecond + _phase * frames.Count) % frames.Count);
        }

        private void ShowFrame(IReadOnlyList<Sprite> frames, int frame)
        {
            if (frames == _shownFrames && frame == _shownFrame)
                return;

            _shownFrames = frames;
            _shownFrame = frame;
            spriteRenderer.sprite = frames[frame];
        }
    }
}
