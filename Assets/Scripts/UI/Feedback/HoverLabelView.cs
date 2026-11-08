using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Hover;
using BarracksAndBolts.Gameplay.Teams;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Feedback
{
    /// <summary>Shows the name of what the mouse is over, at a fixed place on the screen.</summary>
    public sealed class HoverLabelView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible label. Hidden while nothing is under the mouse; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color playerColor = Color.white;
        [SerializeField] private Color enemyColor = new(1f, 0.55f, 0.5f);

        private readonly CompositeDisposable _subscriptions = new();
        private HoverService _hover;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(HoverService hover, IEventBus eventBus)
        {
            _hover = hover;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _eventBus.Subscribe<HoverChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void Refresh()
        {
            var hasTarget = _hover.TryGetInfo(out var info);
            content.SetActive(hasTarget);
            if (!hasTarget)
                return;

            var isPlayer = info.Faction == Faction.Player;
            label.text = isPlayer ? info.Name : $"Enemy {info.Name}";
            label.color = isPlayer ? playerColor : enemyColor;
        }
    }
}
