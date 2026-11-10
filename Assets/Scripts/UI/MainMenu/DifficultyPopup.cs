using System;
using BarracksAndBolts.Core.Session;
using BarracksAndBolts.Popup;
using BarracksAndBolts.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BarracksAndBolts.UI.MainMenu
{
    /// <summary>Asked after Play on the main menu: how hard the game is. Picking one starts the game; Back cancels.</summary>
    public sealed class DifficultyPopup : PopupBase
    {
        [SerializeField] private Button easyButton;
        [SerializeField] private TMP_Text easyLabel;
        [SerializeField] private Button normalButton;
        [SerializeField] private TMP_Text normalLabel;
        [SerializeField] private Button hardButton;
        [SerializeField] private TMP_Text hardLabel;

        /// <summary>Raised once, when the player picks a difficulty.</summary>
        public event Action<Difficulty> Chosen;

        protected override void Awake()
        {
            base.Awake();
            ButtonFeedback.AddToChildren(transform);
        }

        public override void OnOpened()
        {
            easyLabel.text = DifficultyText.WithSummary(Difficulty.Easy);
            normalLabel.text = DifficultyText.WithSummary(Difficulty.Normal);
            hardLabel.text = DifficultyText.WithSummary(Difficulty.Hard);

            easyButton.onClick.AddListener(() => Choose(Difficulty.Easy));
            normalButton.onClick.AddListener(() => Choose(Difficulty.Normal));
            hardButton.onClick.AddListener(() => Choose(Difficulty.Hard));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            easyButton.onClick.RemoveAllListeners();
            normalButton.onClick.RemoveAllListeners();
            hardButton.onClick.RemoveAllListeners();
        }

        private void Choose(Difficulty difficulty)
        {
            // One pick only; the window is on its way out.
            easyButton.interactable = false;
            normalButton.interactable = false;
            hardButton.interactable = false;

            Chosen?.Invoke(difficulty);
            _popupSystem.Close(this);
        }
    }
}
