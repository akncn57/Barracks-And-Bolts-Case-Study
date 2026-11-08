using System;
using BarracksAndBolts.Gameplay.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>One unit the selected building can produce.</summary>
    public sealed class ProducibleUnitCell : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameLabel;

        /// <summary>Raised with the unit the cell shows at the moment of the click.</summary>
        public event Action<UnitDefinition> Clicked;

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

        private void OnButtonClicked()
        {
            if (Definition != null)
                Clicked?.Invoke(Definition);
        }
    }
}
