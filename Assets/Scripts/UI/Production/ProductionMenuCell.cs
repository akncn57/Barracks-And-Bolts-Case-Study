using System;
using BarracksAndBolts.UI.Scroll;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Production
{
    /// <summary>One entry of the production menu: a building or, in sandbox mode, a unit. The menu knows which by the index.</summary>
    public sealed class ProductionMenuCell : RecyclingScrollCell, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;

        private bool _isPointerOver;

        /// <summary>Raised on click; read <see cref="Index"/> for what the cell shows at that moment.</summary>
        public event Action<ProductionMenuCell> Clicked;

        public event Action<ProductionMenuCell> HoverStarted;
        public event Action<ProductionMenuCell> HoverEnded;

        /// <summary>Index of the shown item in the menu's current list. -1 before the first bind.</summary>
        public int Index { get; private set; } = -1;

        private void Awake()
        {
            // Hooked once; the cell is reused for other items, so the click reads the current index.
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }

        public void Bind(int index, string displayName, Sprite sprite)
        {
            Index = index;
            icon.sprite = sprite;
            nameLabel.text = displayName;

            // Scrolling can bring another item under a resting mouse.
            if (_isPointerOver)
                HoverStarted?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;
            HoverStarted?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EndHover();
        }

        private void OnDisable()
        {
            // A hidden cell gets no pointer exit, and the tooltip would stay on the screen.
            EndHover();
        }

        private void EndHover()
        {
            if (!_isPointerOver)
                return;

            _isPointerOver = false;
            HoverEnded?.Invoke(this);
        }

        private void OnButtonClicked()
        {
            if (Index >= 0)
                Clicked?.Invoke(this);
        }
    }
}
