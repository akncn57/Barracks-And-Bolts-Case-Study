using System;
using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.Placement;
using BarracksAndBolts.Utils.Disposables;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Input
{
    /// <summary>
    /// The one place that turns action maps on and off when the input mode changes.
    /// Gameplay (selection) and Placement are never on together, so one left click can't do both.
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
            _inputActions.Gameplay.Enable();
            _eventBus.Subscribe<PlacementStartedEvent>(_ => EnterPlacement()).AddTo(_subscriptions);
            _eventBus.Subscribe<PlacementEndedEvent>(_ => ExitPlacement()).AddTo(_subscriptions);
        }

        public void Dispose()
        {
            _subscriptions.Dispose();

            // Enabled maps that are never disabled leak; the generated class asserts on it.
            if (_inputActions.Gameplay.enabled)
                _inputActions.Gameplay.Disable();

            if (_inputActions.Placement.enabled)
                _inputActions.Placement.Disable();
        }

        private void EnterPlacement()
        {
            _inputActions.Gameplay.Disable();
            _inputActions.Placement.Enable();
        }

        private void ExitPlacement()
        {
            _inputActions.Placement.Disable();
            _inputActions.Gameplay.Enable();
        }
    }
}
