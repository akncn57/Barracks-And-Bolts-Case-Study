using System.Collections.Generic;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Core.Simulation;
using BarracksAndBolts.Gameplay.Grid;
using BarracksAndBolts.Utils.Disposables;
using UnityEngine;
using VContainer;

namespace BarracksAndBolts.Gameplay.ResourceNodes
{
    /// <summary>
    /// Creates a view for every resource, as a child of this object, and plays their animations.
    /// One loop here instead of an Update on each of the hundreds of nodes.
    /// </summary>
    public sealed class ResourceNodeViewSpawner : MonoBehaviour
    {
        [SerializeField] private ResourceNodeView nodePrefab;
        [SerializeField, Min(1f)] private float framesPerSecond = 10f;

        private readonly List<ResourceNodeView> _animatedViews = new();
        private readonly CompositeDisposable _subscriptions = new();
        private GridMap _gridMap;
        private GameClock _clock;

        [Inject]
        private void Construct(IEventBus eventBus, GridMap gridMap, GameClock clock)
        {
            _gridMap = gridMap;
            _clock = clock;
            eventBus.Subscribe<ResourceNodePlacedEvent>(OnNodePlaced).AddTo(_subscriptions);
        }

        private void OnDestroy()
        {
            _subscriptions.Dispose();
        }

        private void OnNodePlaced(ResourceNodePlacedEvent placedEvent)
        {
            var node = placedEvent.Node;
            var view = Instantiate(nodePrefab, transform);
            view.Bind(node, _gridMap.AreaToWorldBottomCenter(node.Area));

            if (view.IsAnimated)
                _animatedViews.Add(view);
        }

        private void LateUpdate()
        {
            // Follows the game clock, so the map freezes on pause like the units do.
            var frameTime = _clock.ElapsedTime * framesPerSecond;
            foreach (var view in _animatedViews)
                view.ShowFrameAt(frameTime);
        }
    }
}
