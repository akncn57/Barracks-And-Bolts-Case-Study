using System.Collections.Generic;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Entities;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Units
{
    /// <summary>
    /// Shows one unit and plays its frames. The root sits on the unit's feet; every frame's pivot is on the feet too,
    /// and a Sorting Group on the root makes the unit sort by that point.
    /// </summary>
    public sealed class UnitView : MonoBehaviour
    {
        // Spreads units over the animation by id, so a group doesn't breathe in perfect sync. A fraction of a loop per id.
        private const float PhasePerId = 0.37f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;

        private IReadOnlyList<Sprite> _frames;
        private GameClock _clock;
        private float _phase;
        private int _shownFrame = -1;

        public GameEntityId UnitId { get; private set; }

        public void Bind(UnitModel unit, IReadOnlyList<Sprite> idleFrames, GameClock clock)
        {
            UnitId = unit.Id;
            name = $"{unit.Definition.DisplayName} ({unit.Id})";
            transform.position = unit.Position;

            _frames = idleFrames;
            _clock = clock;
            _phase = unit.Id.Value * PhasePerId * idleFrames.Count;
            _shownFrame = -1;
        }

        private void Update()
        {
            if (_frames == null || _frames.Count == 0)
                return;

            // Follows the game clock, so units freeze on pause and speed up with the game.
            var frame = Mathf.FloorToInt(_clock.ElapsedTime * framesPerSecond + _phase) % _frames.Count;
            if (frame == _shownFrame)
                return;

            _shownFrame = frame;
            spriteRenderer.sprite = _frames[frame];
        }
    }
}
