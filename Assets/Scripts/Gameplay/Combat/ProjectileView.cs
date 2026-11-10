using System;
using BarracksAndBolts.Core.Simulation;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>
    /// One arrow in flight. Only for the eye: the damage is a scheduled hit that lands when the arrow does.
    /// It moves on the game clock, so pausing and game speed keep the two together. The sprite is expected to point right.
    /// </summary>
    public sealed class ProjectileView : MonoBehaviour
    {
        // Speed for arrows whose shooter has no flight time of its own, so they are still seen flying.
        private const float FallbackSpeed = 12f;

        private SpriteRenderer _spriteRenderer;
        private GameClock _clock;
        private Vector2 _start;
        private Vector2 _end;
        private float _leavesAt;
        private float _flightTime;
        private Action<ProjectileView> _onArrived;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <param name="releaseDelay">Game seconds until the arrow leaves the bow. It stays hidden until then.</param>
        /// <param name="flightTime">Game seconds in the air.</param>
        public void Launch(Vector2 start, Vector2 end, GameClock clock, float releaseDelay, float flightTime, Action<ProjectileView> onArrived)
        {
            _start = start;
            _end = end;
            _clock = clock;
            _leavesAt = clock.ElapsedTime + releaseDelay;
            _flightTime = flightTime > 0f ? flightTime : Vector2.Distance(start, end) / FallbackSpeed;
            _onArrived = onArrived;

            var direction = end - start;
            transform.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
            _spriteRenderer.enabled = false;
        }

        private void Update()
        {
            var flown = _clock.ElapsedTime - _leavesAt;
            if (flown < 0f)
                return;

            _spriteRenderer.enabled = true;
            var progress = _flightTime > 0f ? flown / _flightTime : 1f;
            transform.position = Vector2.Lerp(_start, _end, progress);
            if (progress >= 1f)
                _onArrived(this);
        }
    }
}
