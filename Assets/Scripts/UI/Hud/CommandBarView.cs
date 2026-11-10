using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Commands;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.UI.Tooltips;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>
    /// Command buttons over the unit card while the player's units are selected: Attack Move, Stop, Hold Position.
    /// Each shows its key and calls the same method as the key, so using the mouse teaches the keyboard.
    /// </summary>
    public sealed class CommandBarView : MonoBehaviour
    {
        [Tooltip("Child that holds the buttons. Hidden with no unit of the player selected; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private Button attackMoveButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private Button holdButton;
        [Tooltip("Tint of the Attack Move button while it waits for a point on the map.")]
        [SerializeField] private Color waitingColor = new(1f, 0.8f, 0.6f, 1f);

        private readonly CompositeDisposable _subscriptions = new();
        private HotkeyInputHandler _hotkeys;
        private AttackMoveTargeting _targeting;
        private TooltipView _tooltip;
        private IEventBus _eventBus;
        private Graphic _attackMoveGraphic;
        private Color _attackMoveColor;

        [Inject]
        private void Construct(HotkeyInputHandler hotkeys, AttackMoveTargeting targeting, TooltipView tooltip, IEventBus eventBus)
        {
            _hotkeys = hotkeys;
            _targeting = targeting;
            _tooltip = tooltip;
            _eventBus = eventBus;
        }

        private void Start()
        {
            attackMoveButton.onClick.AddListener(_hotkeys.BeginAttackMove);
            stopButton.onClick.AddListener(_hotkeys.Stop);
            holdButton.onClick.AddListener(_hotkeys.HoldPosition);

            HoverTooltip.Attach(attackMoveButton, _tooltip, "Attack Move (F)", "Then left click on the map. The units walk there and fight every enemy they meet on the way.");
            HoverTooltip.Attach(stopButton, _tooltip, "Stop (X)", "The units stop where they are and drop their work.");
            HoverTooltip.Attach(holdButton, _tooltip, "Hold Position (H)", "The units stay where they are. They hit what comes into range but never chase.");

            _attackMoveGraphic = attackMoveButton.targetGraphic;
            _attackMoveColor = _attackMoveGraphic != null ? _attackMoveGraphic.color : Color.white;

            _eventBus.Subscribe<SelectionChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
            attackMoveButton.onClick.RemoveAllListeners();
            stopButton.onClick.RemoveAllListeners();
            holdButton.onClick.RemoveAllListeners();
        }

        private void LateUpdate()
        {
            if (_attackMoveGraphic != null && content.activeSelf)
                _attackMoveGraphic.color = _targeting.IsActive ? waitingColor : _attackMoveColor;
        }

        private void Refresh()
        {
            content.SetActive(_hotkeys.HasOwnUnitSelected);
        }
    }
}
