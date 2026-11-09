using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Data;
using UnityEngine;

namespace BarracksAndBolts.Gameplay.Buildings
{
    /// <summary>
    /// The unit that stands on a defending building. Only for the eye: it is part of the building,
    /// so it can't be selected and takes no population. Hidden on buildings that don't fight.
    /// </summary>
    public sealed class BuildingDefenderView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;

        private TeamColorDefinition.UnitSpriteSet _sprites;
        private GameClock _clock;
        private float _shotStartedAt = -1f;

        public void Show(TeamColorDefinition.UnitSpriteSet sprites, Vector2 localPosition, GameClock clock)
        {
            _sprites = sprites;
            _clock = clock;
            transform.localPosition = localPosition;
            gameObject.SetActive(true);
        }

        public void PlayShot(Vector2 aimPoint)
        {
            _shotStartedAt = _clock.ElapsedTime;
            spriteRenderer.flipX = aimPoint.x < transform.position.x;
        }

        // Follows the game clock, so the defender freezes on pause like the units do.
        private void LateUpdate()
        {
            var frames = _sprites.AttackFrames;
            var shotFrame = Mathf.FloorToInt((_clock.ElapsedTime - _shotStartedAt) * framesPerSecond);
            if (_shotStartedAt >= 0f && shotFrame < frames.Count)
            {
                spriteRenderer.sprite = frames[shotFrame];
                return;
            }

            _shotStartedAt = -1f;
            var idleFrames = _sprites.IdleFrames;
            if (idleFrames.Count > 0)
                spriteRenderer.sprite = idleFrames[Mathf.FloorToInt(_clock.ElapsedTime * framesPerSecond) % idleFrames.Count];
        }
    }
}
