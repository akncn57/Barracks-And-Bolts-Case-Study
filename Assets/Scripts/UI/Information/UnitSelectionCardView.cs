using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Combat;
using BarracksAndBolts.Gameplay.Selection;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace BarracksAndBolts.UI.Information
{
    /// <summary>
    /// Small card for selected units: the first one's picture and the health of the whole group.
    /// None of its graphics catch the mouse, so commands can be given through it.
    /// </summary>
    public sealed class UnitSelectionCardView : MonoBehaviour
    {
        [Tooltip("Child that holds the visible card. Hidden unless units are selected; this object stays active.")]
        [SerializeField] private GameObject content;
        [SerializeField] private Image picture;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text hpLabel;
        [Tooltip("Image with Image Type set to Filled.")]
        [SerializeField] private Image hpFill;

        private readonly CompositeDisposable _subscriptions = new();
        private SelectionService _selection;
        private UnitService _units;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(SelectionService selection, UnitService units, IEventBus eventBus)
        {
            _selection = selection;
            _units = units;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _eventBus.Subscribe<SelectionChangedEvent>(_ => Refresh()).AddTo(_subscriptions);
            _eventBus.Subscribe<EntityDamagedEvent>(OnEntityDamaged).AddTo(_subscriptions);
            Refresh();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnEntityDamaged(EntityDamagedEvent damagedEvent)
        {
            if (_selection.IsSelected(damagedEvent.Id))
                Refresh();
        }

        private void Refresh()
        {
            var hasUnits = _units.TryGet(_selection.Primary, out var primary);
            content.SetActive(hasUnits);
            if (!hasUnits)
                return;

            picture.sprite = primary.Definition.Portrait;

            var count = _selection.Selected.Count;
            nameLabel.text = count == 1 ? primary.Definition.DisplayName : $"{count} Units";

            var currentHp = 0;
            var maxHp = 0;
            foreach (var id in _selection.Selected)
            {
                if (!_units.TryGet(id, out var unit))
                    continue;

                currentHp += unit.CurrentHp;
                maxHp += unit.Definition.MaxHp;
            }

            hpLabel.text = $"{currentHp} / {maxHp}";
            hpFill.fillAmount = (float)currentHp / maxHp;
        }
    }
}
