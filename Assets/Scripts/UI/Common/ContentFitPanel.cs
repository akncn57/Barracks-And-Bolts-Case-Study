using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Makes a panel as tall as its content, between a minimum and a maximum.
    /// Content taller than that scrolls inside the panel's ScrollRect.
    /// The panel must be anchored to one edge of its parent, so it can set its own height.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ContentFitPanel : MonoBehaviour
    {
        [Tooltip("The ScrollRect's viewport. Whatever the panel adds around it counts as frame.")]
        [SerializeField] private RectTransform viewport;
        [Tooltip("The object with the Content Size Fitter inside the viewport.")]
        [SerializeField] private RectTransform content;
        [SerializeField, Min(0f)] private float minHeight = 380f;
        [SerializeField, Min(0f)] private float maxHeight = 950f;
        [Tooltip("Space to leave free on the parent, so the panel keeps its margins on small screens.")]
        [SerializeField, Min(0f)] private float parentMargin = 50f;

        private RectTransform _rectTransform;
        private RectTransform _parent;
        private float _fittedForParentHeight;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _parent = (RectTransform)_rectTransform.parent;
        }

        /// <summary>Call after changing the content, so the panel doesn't show the old size for a frame.</summary>
        public void Refit()
        {
            if (!content.gameObject.activeInHierarchy)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            var chromeHeight = _rectTransform.rect.height - viewport.rect.height;
            var availableHeight = _parent.rect.height - parentMargin;
            var height = PanelSizeMath.FitHeight(content.rect.height, chromeHeight, minHeight, maxHeight, availableHeight);

            _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            _fittedForParentHeight = _parent.rect.height;
        }

        private void LateUpdate()
        {
            // The canvas height changes when the window is resized.
            if (!Mathf.Approximately(_parent.rect.height, _fittedForParentHeight))
                Refit();
        }
    }
}
