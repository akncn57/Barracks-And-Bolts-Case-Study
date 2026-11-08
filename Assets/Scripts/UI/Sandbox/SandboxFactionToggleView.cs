using BarracksAndBolts.Gameplay.Sandbox;
using BarracksAndBolts.Gameplay.Teams;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Sandbox
{
    /// <summary>Button that switches the side new buildings and units belong to. Hidden outside sandbox mode.</summary>
    public sealed class SandboxFactionToggleView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private SandboxService _sandbox;

        [Inject]
        private void Construct(SandboxService sandbox)
        {
            _sandbox = sandbox;
        }

        private void Start()
        {
            if (!_sandbox.IsEnabled)
            {
                gameObject.SetActive(false);
                return;
            }

            button.onClick.AddListener(OnClicked);
            RefreshLabel();
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _sandbox.ToggleFaction();
            RefreshLabel();
        }

        private void RefreshLabel()
        {
            label.text = _sandbox.ActiveFaction == Faction.Player ? "Side: Player" : "Side: Enemy";
        }
    }
}
