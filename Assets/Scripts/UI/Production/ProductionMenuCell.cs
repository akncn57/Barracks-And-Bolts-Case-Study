using System;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.UI.Scroll;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Production
{
    public sealed class ProductionMenuCell : RecyclingScrollCell, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;

        private bool _isPointerOver;

        /// <summary>Raised with the building the cell shows at the moment of the click.</summary>
        public event Action<BuildingDefinition> Clicked;

        public event Action<ProductionMenuCell> HoverStarted;
        public event Action<ProductionMenuCell> HoverEnded;

        public BuildingDefinition Definition { get; private set; }

        private void Awake()
        {
            // Hooked once; the cell is reused for other buildings, so the click reads the current Definition.
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }

        public void Bind(BuildingDefinition definition, Sprite sprite)
        {
            Definition = definition;
            icon.sprite = sprite;
            nameLabel.text = definition.DisplayName;

            // Scrolling can bring another building under a resting mouse.
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
            if (Definition != null)
                Clicked?.Invoke(Definition);
        }
    }
}
