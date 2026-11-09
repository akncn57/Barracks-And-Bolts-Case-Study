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

        private readonly CompositeDisposable _subscriptions = new();
        private ResourceService _resources;
        private PopulationService _population;
        private IEventBus _eventBus;

        [Inject]
        private void Construct(ResourceService resources, PopulationService population, IEventBus eventBus)
        {
            _resources = resources;
            _population = population;
            _eventBus = eventBus;
        }

        private void Start()
        {
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

        private void ShowPopulation()
        {
            populationLabel.text = $"{_population.Used}/{_population.Capacity}";
        }
    }
}
