using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Drawer on the left edge of the screen. Starts closed; its button slides it in and out.
    /// The panel must be anchored to the left. Keep the button outside its right edge so it stays on screen when closed.
    /// </summary>
    public sealed class SlideOutPanel : MonoBehaviour, ICloseablePanel
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private TMP_Text toggleLabel;
        [SerializeField] private string openText = "Open >";
        [SerializeField] private string closeText = "< Close";
        [SerializeField] private float duration = 0.25f;

        private RectTransform _rect;
        private Vector2 _openPosition;
        private Vector2 _closedPosition;
        private Tween _slide;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _openPosition = _rect.anchoredPosition;

            // Puts the panel's right edge on the screen's left edge.
            _closedPosition = _openPosition + Vector2.left * _rect.offsetMax.x;
            _rect.anchoredPosition = _closedPosition;

            toggleButton.onClick.AddListener(Toggle);
            RefreshLabel();
        }

        private void OnDestroy()
        {
            _slide?.Kill();
            toggleButton.onClick.RemoveListener(Toggle);
        }

        public void Open()
        {
            if (!IsOpen)
                Toggle();
        }

        public void Close()
        {
            if (IsOpen)
                Toggle();
        }

        private void Toggle()
        {
            IsOpen = !IsOpen;
            RefreshLabel();

            // A click during the slide turns it around from where it is.
            _slide?.Kill();
            _slide = _rect
                .DOAnchorPos(IsOpen ? _openPosition : _closedPosition, duration)
                .SetEase(IsOpen ? Ease.OutCubic : Ease.InCubic)
                .SetUpdate(true);
        }

        private void RefreshLabel()
        {
            toggleLabel.text = IsOpen ? closeText : openText;
        }
    }
}
