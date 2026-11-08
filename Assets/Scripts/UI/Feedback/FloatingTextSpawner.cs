using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;

namespace BarracksAndBolts.UI.Feedback
{
    /// <summary>Shows every requested floating text. The texts are pooled, so frequent messages create no garbage objects.</summary>
    public sealed class FloatingTextSpawner : MonoBehaviour
    {
        [SerializeField] private FloatingTextView textPrefab;
        [SerializeField] private Color infoColor = Color.white;
        [SerializeField] private Color warningColor = new(1f, 0.45f, 0.35f);

        private readonly CompositeDisposable _subscriptions = new();
        private ObjectPool<FloatingTextView> _pool;

        [Inject]
        private void Construct(IEventBus eventBus)
        {
            eventBus.Subscribe<FloatingTextRequestedEvent>(OnRequested).AddTo(_subscriptions);
        }

        private void Awake()
        {
            _pool = new ObjectPool<FloatingTextView>(
                () => Instantiate(textPrefab, transform),
                view => view.gameObject.SetActive(true),
                view => view.gameObject.SetActive(false),
                view => Destroy(view.gameObject));
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            _pool.Dispose();
        }

        private void OnRequested(FloatingTextRequestedEvent request)
        {
            var color = request.Kind == FloatingTextKind.Warning ? warningColor : infoColor;
            _pool.Get().Play(request.Text, request.Position, color, _pool.Release);
        }
    }
}
