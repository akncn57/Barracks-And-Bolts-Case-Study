using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Calendar;
using BarracksAndBolts.Gameplay.Data;
using BarracksAndBolts.Gameplay.ResourceNodes;
using BarracksAndBolts.Utils.Disposables;

namespace BarracksAndBolts.Gameplay.Economy
{
    /// <summary>New sheep appear every morning, up to a limit, so food can't run out for good.</summary>
    public sealed class ResourceRespawnService : IDisposable
    {
        private readonly ResourceDistributor _distributor;
        private readonly ResourceNodeService _nodes;
        private readonly NormalModeSettings _settings;
        private readonly CompositeDisposable _subscriptions = new();

        public ResourceRespawnService(ResourceDistributor distributor, ResourceNodeService nodes, NormalModeSettings settings, IEventBus eventBus)
        {
            _distributor = distributor;
            _nodes = nodes;
            _settings = settings;
            eventBus.Subscribe<DayStartedEvent>(OnDayStarted).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }

        private void OnDayStarted(DayStartedEvent startedEvent)
        {
            // The map is already full on the first morning.
            if (startedEvent.Day <= 1)
                return;

            var sheep = 0;
            foreach (var node in _nodes.Nodes)
            {
                if (node.Definition.Kind == ResourceKind.Food)
                    sheep++;
            }

            var count = Math.Min(_settings.SheepPerMorning, _settings.MaxSheep - sheep);
            if (count > 0)
                _distributor.AddNodes(ResourceKind.Food, count);
        }
    }
}
