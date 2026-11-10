using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.UI.Tooltips;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>
    /// Beside the unit card while a villager of the player has nothing to do: shows how many, and a click (or the . key)
    /// selects the next one and moves the camera to it.
    /// </summary>
    public sealed class IdleVillagerButtonView : MonoBehaviour
    {
        // Villagers turn idle in many places, so the count is simply looked at a few times a second.
        private const float RefreshInterval = 0.25f;

        [Tooltip("Child that holds the button. Hidden while no villager is idle; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private HotkeyInputHandler _hotkeys;
        private IdleVillagerFinder _finder;
        private TooltipView _tooltip;
        private float _refreshDelayLeft;
        private int _shownCount = -1;

        [Inject]
        private void Construct(HotkeyInputHandler hotkeys, IdleVillagerFinder finder, TooltipView tooltip)
        {
            _hotkeys = hotkeys;
            _finder = finder;
            _tooltip = tooltip;
        }

        private void Start()
        {
            button.onClick.AddListener(OnClicked);
            HoverTooltip.Attach(button, _tooltip, "Idle Villager (.)", "Selects the next villager with nothing to do and moves the camera to it.");
            Refresh();
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        private void Update()
        {
            _refreshDelayLeft -= Time.unscaledDeltaTime;
            if (_refreshDelayLeft <= 0f)
                Refresh();
        }

        private void OnClicked()
        {
            _hotkeys.SelectNextIdleVillager();
        }

        private void Refresh()
        {
            _refreshDelayLeft = RefreshInterval;
            var count = _finder.CountIdle();
            if (count == _shownCount)
                return;

            _shownCount = count;
            content.SetActive(count > 0);
            label.text = $"Idle: {count}";
        }
    }
}
