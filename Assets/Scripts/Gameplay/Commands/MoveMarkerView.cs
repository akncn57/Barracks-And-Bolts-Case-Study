using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>Short shrinking ring on the ground where an order was given. Its color tells move from attack.</summary>
    public sealed class MoveMarkerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer markerRenderer;
        [SerializeField] private Color moveColor = Color.green;
        [SerializeField] private Color attackColor = Color.red;
        [SerializeField, Min(0.01f)] private float duration = 0.4f;
        [SerializeField, Min(0f)] private float startScale = 1.2f;
        [SerializeField, Min(0f)] private float endScale = 0.3f;

        private readonly CompositeDisposable _subscriptions = new();
        private Color _baseColor;
        private float _elapsed;

        [Inject]
        private void Construct(IEventBus eventBus)
        {
            eventBus.Subscribe<MoveCommandIssuedEvent>(e => Show(e.Position, moveColor)).AddTo(_subscriptions);
            eventBus.Subscribe<AttackCommandIssuedEvent>(e => Show(e.Position, attackColor)).AddTo(_subscriptions);
        }

        private void Awake()
        {
            markerRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void Show(Vector2 position, Color color)
        {
            transform.position = position;
            _baseColor = color;
            _elapsed = 0f;
            markerRenderer.enabled = true;
        }

        private void Update()
        {
            if (!markerRenderer.enabled)
                return;

            // Unscaled, so the player gets feedback even while the game is paused.
            _elapsed += Time.unscaledDeltaTime;
            var progress = _elapsed / duration;
            if (progress >= 1f)
            {
                markerRenderer.enabled = false;
                return;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
            var color = _baseColor;
            color.a *= 1f - progress;
            markerRenderer.color = color;
        }
    }
}
