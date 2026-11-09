using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Raids;
using BarracksAndBolts.Utils.Disposables;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>Shows a notice for the whole screen for a few seconds. A new one replaces the old one.</summary>
    public sealed class NoticeBannerView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color alertColor = new(1f, 0.55f, 0.5f);
        [SerializeField, Min(0f)] private float holdSeconds = 3f;
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.4f;

        private readonly CompositeDisposable _subscriptions = new();
        private IEventBus _eventBus;
        private Sequence _sequence;

        [Inject]
        private void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Start()
        {
            group.alpha = 0f;
            _eventBus.Subscribe<GameNoticeEvent>(Show).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
            _subscriptions.Dispose();
        }

        private void Show(GameNoticeEvent notice)
        {
            label.text = notice.Text;
            label.color = notice.IsAlert ? alertColor : normalColor;

            _sequence?.Kill();
            _sequence = DOTween.Sequence()
                .Append(group.DOFade(1f, fadeSeconds))
                .AppendInterval(holdSeconds)
                .Append(group.DOFade(0f, fadeSeconds))
                .SetUpdate(true);
        }
    }
}
