using UnityEngine;
using UnityEngine.EventSystems;

namespace BarracksAndBolts.UI.Tooltips
{
    /// <summary>
    /// Shows a tooltip while the mouse is over this element. Added from code by the view that owns the element,
    /// which already has the tooltip injected, so buttons in prefabs need no setup.
    /// </summary>
    public sealed class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private TooltipView _view;
        private string _title;
        private string _body;
        private bool _isShowing;

        public static void Attach(Component target, TooltipView view, string title, string body)
        {
            var tooltip = target.GetComponent<HoverTooltip>();
            if (tooltip == null)
                tooltip = target.gameObject.AddComponent<HoverTooltip>();

            tooltip._view = view;
            tooltip._title = title;
            tooltip._body = body;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isShowing = true;
            _view.Show(_title, _body, (RectTransform)transform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hide();
        }

        // A button that hides under the mouse never gets the exit call.
        private void OnDisable()
        {
            Hide();
        }

        private void Hide()
        {
            if (!_isShowing)
                return;

            _isShowing = false;
            _view.Hide();
        }
    }
}
