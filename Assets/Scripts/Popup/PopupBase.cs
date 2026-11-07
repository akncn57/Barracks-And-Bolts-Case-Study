using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupBase : MonoBehaviour
    {
        [SerializeField] private Button closeButton;

        protected Button CloseButton => closeButton;

        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Tween _activeTween;
        private Vector2 _originalPos;

        protected PopupSystem _popupSystem;

        [Inject]
        private void Construct(PopupSystem popupSystem)
        {
            _popupSystem = popupSystem;
        }

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalPos = _rectTransform.anchoredPosition;

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        protected virtual void OnDestroy()
        {
            _activeTween?.Kill();

            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnCloseButtonClicked);
        }

        // Override to add extra logic before closing (e.g. deselect building).
        protected virtual void OnCloseButtonClicked()
        {
            _popupSystem.Close(this);
        }

        // Asks the popup to close as if its own close button was pressed.
        // Routed through OnCloseButtonClicked so any per-popup cleanup still
        // runs; calling Close directly from outside would skip it.
        //
        // The close button decides. A popup whose button is hidden or greyed
        // out is deliberately not dismissable yet — a tutorial step waiting to
        // be read, a reward still being claimed — and the dark background must
        // not become a way around that. A popup with no close button at all
        // was built to end on its own terms, so it is left alone too.
        // Hides this popup while another one sits on top of it, and brings it
        // back untouched when that one closes.
        //
        // The canvas group is used rather than deactivating the object, because
        // the popup must return exactly as the player left it: a half-written
        // message, a scrolled list, a chosen tab. Deactivating would fire
        // OnDisable and OnEnable, and any popup that rebuilds itself there
        // would quietly throw that state away.
        internal void SetHiddenBehind(bool hidden)
        {
            if (_canvasGroup == null) return;

            _canvasGroup.alpha = hidden ? 0f : 1f;
            _canvasGroup.interactable = !hidden;
            _canvasGroup.blocksRaycasts = !hidden;
        }

        internal void RequestClose()
        {
            if (!CanClose) return;

            OnCloseButtonClicked();
        }

        // Whether the player is allowed to dismiss this popup right now.
        private bool CanClose =>
            closeButton != null
            && closeButton.isActiveAndEnabled
            && closeButton.interactable;

        public virtual void OnOpened() { }
        public virtual void OnClosed() { }

        // Tweens run on unscaled time, so popups still animate while the game is paused (settings) or in a hit stop.
        internal void PlayOpenAnimation(PopupAnimationData data, Action onComplete = null)
        {
            _activeTween?.Kill(complete: true);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            switch (data.Type)
            {
                case PopupAnimationType.None:
                    _canvasGroup.alpha = 1f;
                    onComplete?.Invoke();
                    break;

                case PopupAnimationType.Fade:
                    _canvasGroup.alpha = 0f;
                    _activeTween = _canvasGroup
                        .DOFade(1f, data.Duration)
                        .SetEase(data.Ease)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.Scale:
                    _canvasGroup.alpha = 1f;
                    _rectTransform.localScale = Vector3.zero;
                    _activeTween = _rectTransform
                        .DOScale(Vector3.one, data.Duration)
                        .SetEase(data.Ease)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.Punch:
                    _canvasGroup.alpha = 1f;
                    var punchStrength = data.Magnitude > 0f ? data.Magnitude : 0.2f;
                    _activeTween = _rectTransform
                        .DOPunchScale(Vector3.one * punchStrength, data.Duration, 5, 0.5f)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.SlideFromBottom:
                    PlaySlideOpen(Vector2.down, data, onComplete); break;
                case PopupAnimationType.SlideFromTop:
                    PlaySlideOpen(Vector2.up, data, onComplete); break;
                case PopupAnimationType.SlideFromLeft:
                    PlaySlideOpen(Vector2.left, data, onComplete); break;
                case PopupAnimationType.SlideFromRight:
                    PlaySlideOpen(Vector2.right, data, onComplete); break;
            }
        }

        internal void PlayCloseAnimation(PopupAnimationData data, Action onComplete)
        {
            _activeTween?.Kill(complete: false);
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            switch (data.Type)
            {
                case PopupAnimationType.None:
                    _canvasGroup.alpha = 0f;
                    onComplete?.Invoke();
                    break;

                case PopupAnimationType.Fade:
                    _activeTween = _canvasGroup
                        .DOFade(0f, data.Duration)
                        .SetEase(data.Ease)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.Scale:
                    _activeTween = _rectTransform
                        .DOScale(Vector3.zero, data.Duration)
                        .SetEase(data.Ease)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.Punch:
                    _activeTween = _canvasGroup
                        .DOFade(0f, data.Duration > 0f ? data.Duration : 0.2f)
                        .SetUpdate(true)
                        .OnComplete(() => onComplete?.Invoke());
                    break;

                case PopupAnimationType.SlideFromBottom:
                    PlaySlideClose(Vector2.down, data, onComplete); break;
                case PopupAnimationType.SlideFromTop:
                    PlaySlideClose(Vector2.up, data, onComplete); break;
                case PopupAnimationType.SlideFromLeft:
                    PlaySlideClose(Vector2.left, data, onComplete); break;
                case PopupAnimationType.SlideFromRight:
                    PlaySlideClose(Vector2.right, data, onComplete); break;
            }
        }

        private void PlaySlideOpen(Vector2 direction, PopupAnimationData data, Action onComplete)
        {
            _canvasGroup.alpha = 1f;
            var offset = data.Magnitude > 0f ? data.Magnitude : 300f;
            _rectTransform.anchoredPosition = _originalPos + direction * offset;
            _activeTween = _rectTransform
                .DOAnchorPos(_originalPos, data.Duration)
                .SetEase(data.Ease)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void PlaySlideClose(Vector2 direction, PopupAnimationData data, Action onComplete)
        {
            var offset = data.Magnitude > 0f ? data.Magnitude : 300f;
            _activeTween = _rectTransform
                .DOAnchorPos(_originalPos + direction * offset, data.Duration)
                .SetEase(data.Ease)
                .SetUpdate(true)
                .OnComplete(() => onComplete?.Invoke());
        }
    }
}
