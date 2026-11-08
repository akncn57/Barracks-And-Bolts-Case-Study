using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Utils.Disposables;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Input
{
    /// <summary>
    /// The one place that turns action maps on and off when the input mode changes.
    /// The camera map stays on in every mode, so the player can look around while placing.
    /// </summary>
    public sealed class InputModeController : IStartable, IDisposable
    {
        private readonly GameInputActions _inputActions;
        private readonly IEventBus _eventBus;
        private readonly CompositeDisposable _subscriptions = new();

        public InputModeController(GameInputActions inputActions, IEventBus eventBus)
        {
            _inputActions = inputActions;
            _eventBus = eventBus;
        }

        public void Start()
        {
            _eventBus.Subscribe<PlacementStartedEvent>(_ => _inputActions.Placement.Enable()).AddTo(_subscriptions);
            _eventBus.Subscribe<PlacementEndedEvent>(_ => _inputActions.Placement.Disable()).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();
        }
    }
}
