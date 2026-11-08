using System;
using TMPro;
using UnityEngine;

namespace BarracksAndBolts.UI.Feedback
{
    /// <summary>One text that rises and fades in the world, then hands itself back to its pool.</summary>
    public sealed class FloatingTextView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro label;
        [SerializeField, Min(0.01f)] private float lifetime = 1.1f;
        [SerializeField, Min(0f)] private float riseSpeed = 1f;
        [Tooltip("Drawn above buildings and units.")]
        [SerializeField] private int sortingOrder = 300;

        private Action<FloatingTextView> _onFinished;
        private Color _color;
        private float _elapsed;

        private void Awake()
        {
            label.sortingOrder = sortingOrder;
        }

        public void Play(string text, Vector2 position, Color color, Action<FloatingTextView> onFinished)
        {
            transform.position = position;
            label.text = text;
            label.color = color;
            _color = color;
            _elapsed = 0f;
            _onFinished = onFinished;
        }

        private void Update()
        {
            // Unscaled, so messages still play while the game is paused.
            var deltaTime = Time.unscaledDeltaTime;
            _elapsed += deltaTime;
            if (_elapsed >= lifetime)
            {
                _onFinished(this);
                return;
            }

            transform.position += Vector3.up * (riseSpeed * deltaTime);

            // Fully readable for the first half, then fades out.
            var color = _color;
            color.a *= Mathf.Clamp01(2f * (1f - _elapsed / lifetime));
            label.color = color;
        }
    }
}
