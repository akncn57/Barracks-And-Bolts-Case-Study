using System;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.UI.Scroll;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.Production
{
    public sealed class ProductionMenuCell : RecyclingScrollCell
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;

        /// <summary>Raised with the building the cell shows at the moment of the click.</summary>
        public event Action<BuildingDefinition> Clicked;

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
        }

        private void OnButtonClicked()
        {
            if (Definition != null)
                Clicked?.Invoke(Definition);
        }
    }
}
