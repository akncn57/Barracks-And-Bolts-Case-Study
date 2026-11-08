using UnityEngine;

namespace BarracksAndBolts.Gameplay.Combat
{
    /// <summary>Tints a sprite for a moment when it takes a hit. Shared by unit and building views.</summary>
    public sealed class DamageFlash
    {
        private const float Duration = 0.12f;
        private static readonly Color FlashColor = new(1f, 0.35f, 0.35f);

        private float _timeLeft;

        public void Start()
        {
            _timeLeft = Duration;
        }

        public void Update(SpriteRenderer spriteRenderer, float deltaTime)
        {
            if (_timeLeft <= 0f)
                return;

            _timeLeft -= deltaTime;
            spriteRenderer.color = _timeLeft > 0f ? FlashColor : Color.white;
        }
    }
}
