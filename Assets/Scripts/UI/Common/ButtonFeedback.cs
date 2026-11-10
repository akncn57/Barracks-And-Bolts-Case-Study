using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Common
{
    /// <summary>
    /// Makes a button grow a little under the mouse and dip when pressed. Unscaled time, so it works while paused.
    /// Added at runtime by <see cref="ButtonFeedbackInstaller"/> and popups, so no prefab has to carry it.
    /// Buttons rest at scale 1; an entrance animation may still be running when this is added.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private const float HoverScale = 1.05f;
        private const float PressScale = 0.95f;
        private const float Duration = 0.12f;

        private Button _button;
        private bool _isHovered;
        private Tween _tween;

        /// <summary>Adds the feedback to every button under the root that doesn't have it yet, hidden ones too.</summary>
        public static void AddToChildren(Transform root)
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (!button.TryGetComponent<ButtonFeedback>(out _))
                    button.gameObject.AddComponent<ButtonFeedback>();
            }
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _isHovered = false;
            transform.localScale = Vector3.one;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            ScaleTo(HoverScale, Ease.OutQuad);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            ScaleTo(1f, Ease.OutQuad);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ScaleTo(PressScale, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ScaleTo(_isHovered ? HoverScale : 1f, Ease.OutBack);
        }

        private void ScaleTo(float factor, Ease ease)
        {
            if (!_button.IsInteractable() && factor > 1f)
                return;

            _tween?.Kill();
            _tween = transform.DOScale(factor, Duration).SetEase(ease).SetUpdate(true).SetLink(gameObject);
        }
    }
}
