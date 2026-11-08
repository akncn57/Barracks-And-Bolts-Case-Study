using BarracksAndBolts.Gameplay.Sandbox;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>Top bar with every resource and the population. Sandbox has no limits, so all of them show the infinity sign.</summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        private const string InfiniteAmount = "∞";

        // Normal mode has no economy yet, so nothing is owned there.
        private const string EmptyAmount = "0";

        [SerializeField] private TMP_Text woodLabel;
        [SerializeField] private TMP_Text stoneLabel;
        [SerializeField] private TMP_Text foodLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text populationLabel;

        private SandboxService _sandbox;

        [Inject]
        private void Construct(SandboxService sandbox)
        {
            _sandbox = sandbox;
        }

        private void Start()
        {
            var amount = _sandbox.IsEnabled ? InfiniteAmount : EmptyAmount;
            woodLabel.text = amount;
            stoneLabel.text = amount;
            foodLabel.text = amount;
            goldLabel.text = amount;
            populationLabel.text = amount;
        }
    }
}
