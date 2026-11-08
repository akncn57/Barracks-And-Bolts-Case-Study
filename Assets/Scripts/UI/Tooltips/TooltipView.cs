using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Tooltips
{
    /// <summary>
    /// The one tooltip of the screen. Any view can call Show with a title, a text and the element to sit next to.
    /// This object covers the canvas and stays active; only the panel inside it is shown and hidden.
    /// </summary>
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [Tooltip("Seconds the mouse must rest on an element, so tooltips don't flash while passing over.")]
        [SerializeField, Min(0f)] private float delay = 0.25f;
        [SerializeField, Min(0f)] private float gap = 12f;

        private static readonly Vector3[] Corners = new Vector3[4];

        private RectTransform _area;
        private RectTransform _anchor;
        private float _showAt;

        private void Awake()
        {
            _area = (RectTransform)transform;
            panel.gameObject.SetActive(false);
        }

        public void Show(string title, string body, RectTransform anchor)
        {
            titleLabel.text = title;
            bodyLabel.text = body;
            _anchor = anchor;

            // Moving from one element to the next feels instant; only the first one waits.
            if (panel.gameObject.activeSelf)
                Reveal();
            else
                _showAt = Time.unscaledTime + delay;
        }

        public void Hide()
        {
            _anchor = null;
            panel.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (_anchor != null && !panel.gameObject.activeSelf && Time.unscaledTime >= _showAt)
                Reveal();
        }

        private void Reveal()
        {
            panel.gameObject.SetActive(true);

            // The size is needed now to place the panel, not at the end of the frame.
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

            var bottomLeft = TooltipPlacement.GetBottomLeft(GetRectInArea(_anchor), panel.rect.size, _area.rect, gap);
            panel.localPosition = bottomLeft + panel.rect.size * panel.pivot;
        }

        private Rect GetRectInArea(RectTransform element)
        {
            element.GetWorldCorners(Corners);
            var min = (Vector2)_area.InverseTransformPoint(Corners[0]);
            var max = (Vector2)_area.InverseTransformPoint(Corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }
}
