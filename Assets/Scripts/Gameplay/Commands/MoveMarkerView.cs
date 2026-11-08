using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.Commands
{
    /// <summary>Short shrinking ring on the ground where a move order was given.</summary>
    public sealed class MoveMarkerView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer markerRenderer;
        [SerializeField, Min(0.01f)] private float duration = 0.4f;
        [SerializeField, Min(0f)] private float startScale = 1.2f;
        [SerializeField, Min(0f)] private float endScale = 0.3f;

        private readonly CompositeDisposable _subscriptions = new();
        private Color _baseColor;
        private float _elapsed;

        [Inject]
        private void Construct(IEventBus eventBus)
        {
            eventBus.Subscribe<MoveCommandIssuedEvent>(OnMoveCommandIssued).AddTo(_subscriptions);
        }

        private void Awake()
        {
            _baseColor = markerRenderer.color;
            markerRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnMoveCommandIssued(MoveCommandIssuedEvent issuedEvent)
        {
            transform.position = issuedEvent.Position;
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
