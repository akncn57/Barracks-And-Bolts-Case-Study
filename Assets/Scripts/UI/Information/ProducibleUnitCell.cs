using System;
using BarracksAndBolts.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>One unit the selected building can produce.</summary>
    public sealed class ProducibleUnitCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;

        private bool _isPointerOver;

        /// <summary>Raised with the unit the cell shows at the moment of the click.</summary>
        public event Action<UnitDefinition> Clicked;

        public event Action<ProducibleUnitCell> HoverStarted;
        public event Action<ProducibleUnitCell> HoverEnded;

        public UnitDefinition Definition { get; private set; }

        private void Awake()
        {
            // Hooked once; the cell is reused for other buildings, so the click reads the current Definition.
            button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }

        public void Bind(UnitDefinition definition)
        {
            Definition = definition;
            portrait.sprite = definition.Portrait;
            nameLabel.text = definition.DisplayName;
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
