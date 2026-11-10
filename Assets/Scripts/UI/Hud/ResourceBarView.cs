using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Buildings;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.Economy;
using BarracksAndBolts.Gameplay.Units;
using BarracksAndBolts.Utils.Disposables;
using TMPro;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.UI.Hud
{
    /// <summary>
    /// Top bar with every resource and the population. Sandbox has no limits, so all of them show the infinity sign;
    /// otherwise it follows what the player owns.
    /// </summary>
    public sealed class ResourceBarView : MonoBehaviour
    {
        private const string InfiniteAmount = "∞";

        [SerializeField] private TMP_Text woodLabel;
        [SerializeField] private TMP_Text stoneLabel;
        [SerializeField] private TMP_Text foodLabel;
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text populationLabel;
        [Tooltip("Food amount while the people are starving.")]
        [SerializeField] private Color starvingColor = new(1f, 0.35f, 0.3f, 1f);

        private readonly CompositeDisposable _subscriptions = new();
        private ResourceService _resources;
        private PopulationService _population;
        private HungerSystem _hunger;
        private IEventBus _eventBus;
        private Color _foodColor;

        [Inject]
        private void Construct(ResourceService resources, PopulationService population, HungerSystem hunger, IEventBus eventBus)
        {
            _resources = resources;
            _population = population;
            _hunger = hunger;
            _eventBus = eventBus;
        }

        private void Start()
        {
            _foodColor = foodLabel.color;
            if (_resources.IsUnlimited)
            {
                woodLabel.text = InfiniteAmount;
                stoneLabel.text = InfiniteAmount;
                foodLabel.text = InfiniteAmount;
                goldLabel.text = InfiniteAmount;
                populationLabel.text = InfiniteAmount;
                return;
            }

            _eventBus.Subscribe<ResourcesChangedEvent>(_ => ShowResources()).AddTo(_subscriptions);
            _eventBus.Subscribe<UnitSpawnedEvent>(_ => ShowPopulation()).AddTo(_subscriptions);
            _eventBus.Subscribe<UnitDiedEvent>(_ => ShowPopulation()).AddTo(_subscriptions);
            _eventBus.Subscribe<BuildingPlacedEvent>(_ => ShowPopulation()).AddTo(_subscriptions);
            _eventBus.Subscribe<BuildingDestroyedEvent>(_ => ShowPopulation()).AddTo(_subscriptions);

            // A house adds room only once it is finished.
            _eventBus.Subscribe<BuildingCompletedEvent>(_ => ShowPopulation()).AddTo(_subscriptions);
            _eventBus.Subscribe<HungerChangedEvent>(_ => ShowHunger()).AddTo(_subscriptions);
            ShowResources();
            ShowPopulation();
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void ShowResources()
        {
            woodLabel.text = _resources.Get(ResourceKind.Wood).ToString();
            stoneLabel.text = _resources.Get(ResourceKind.Stone).ToString();
            foodLabel.text = _resources.Get(ResourceKind.Food).ToString();
            goldLabel.text = _resources.Get(ResourceKind.Gold).ToString();
        }

        private void ShowHunger()
        {
            foodLabel.color = _hunger.IsStarving ? starvingColor : _foodColor;
        }

        private void ShowPopulation()
        {
            populationLabel.text = $"{_population.Used}/{_population.Capacity}";
        }
    }
}
