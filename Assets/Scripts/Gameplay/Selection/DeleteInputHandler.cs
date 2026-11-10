using BarracksAndBolts.Core.Events;
using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Feedback;
using BarracksAndBolts.Gameplay.Input;
using UnityEngine;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>The Delete key removes the selection, or in sandbox mode the resource under the mouse.</summary>
    public sealed class DeleteInputHandler : IPostTickable
    {
        private readonly DeletionService _deletion;
        private readonly SelectionService _selection;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.GameplayActions _actions;
        private readonly IEventBus _eventBus;

        public DeleteInputHandler(DeletionService deletion, SelectionService selection, CameraController cameraController,
            GameInputActions inputActions, IEventBus eventBus)
        {
            _deletion = deletion;
            _selection = selection;
            _cameraController = cameraController;
            _actions = inputActions.Gameplay;
            _eventBus = eventBus;
        }

        public void PostTick()
        {
            if (!_actions.enabled || !_actions.Delete.WasPressedThisFrame())
                return;

            var hadSelection = _selection.Selected.Count > 0;
            if (_deletion.DeleteSelectedOrHovered() > 0 || !hadSelection)
                return;

            // Something was selected but none of it may go: the enemy's or the Castle in a normal game.
            var pointer = _cameraController.ScreenToWorld(_actions.PointerPosition.ReadValue<Vector2>());
            _eventBus.Publish(new FloatingTextRequestedEvent("Can't delete this", pointer, FloatingTextKind.Warning));
        }
    }
}
