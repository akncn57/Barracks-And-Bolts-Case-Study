using DG.Tweening;
using UnityEngine;

namespace BarracksAndBolts.UI.MainMenu
{
    /// <summary>
    /// Keeps the main menu alive: the island picture drifts and zooms very slowly, and the title drops in, then floats.
    /// </summary>
    public sealed class MainMenuBackgroundView : MonoBehaviour
    {
        [SerializeField] private RectTransform picture;
        [SerializeField] private RectTransform title;

        [Header("Picture")]
        [Tooltip("The picture is drawn this much bigger than the screen, so its drift never shows an edge.")]
        [SerializeField, Min(1f)] private float baseScale = 1.08f;
        [SerializeField, Min(1f)] private float zoomScale = 1.14f;
        [SerializeField] private Vector2 drift = new(40f, 18f);
        [SerializeField, Min(1f)] private float cycleSeconds = 24f;
        [SerializeField, Min(0.1f)] private float pictureAspect = 16f / 9f;

        [Header("Title")]
        [SerializeField] private float titleDropHeight = 140f;
        [SerializeField] private float titleFloatHeight = 8f;

        private Vector2 _fittedTo;

        private void Start()
        {
            FitPicture();
            picture.localScale = Vector3.one * baseScale;
            picture.DOScale(zoomScale, cycleSeconds).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);
            picture.DOAnchorPos(drift, cycleSeconds * 0.75f).From(-drift).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject);

            var rest = title.anchoredPosition;
            title.anchoredPosition = rest + Vector2.up * titleDropHeight;
            // A sequence can't hold an endless tween, so the float starts when the drop ends.
            title.DOAnchorPos(rest, 0.7f).SetEase(Ease.OutBounce).SetLink(gameObject).OnComplete(() =>
                title.DOAnchorPosY(rest.y + titleFloatHeight, 1.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(gameObject));
        }

        // The window can change size at any time; the picture must still cover all of it.
        private void Update()
        {
            FitPicture();
        }

        /// <summary>Covers the whole screen at any aspect ratio, cutting off the sides or the top instead of leaving bars.</summary>
        private void FitPicture()
        {
            var screen = ((RectTransform)picture.parent).rect.size;
            if (screen == _fittedTo)
                return;

            _fittedTo = screen;
            var isWiderThanPicture = screen.x / screen.y > pictureAspect;
            picture.sizeDelta = isWiderThanPicture ? new Vector2(screen.x, screen.x / pictureAspect) : new Vector2(screen.y * pictureAspect, screen.y);
        }
    }
}
