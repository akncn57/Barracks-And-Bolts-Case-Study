using BarracksAndBolts.Gameplay.CameraControl;
using BarracksAndBolts.Gameplay.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace BarracksAndBolts.Gameplay.Selection
{
    /// <summary>
    /// Left click selects what is under the cursor. The Gameplay map is off while placing,
    /// so the click that places a building never selects anything.
    /// Post-tick for the same reason as placement: the camera has already moved this frame.
    /// </summary>
    public sealed class SelectionInputHandler : IPostTickable
    {
        private readonly SelectionService _selection;
        private readonly CameraController _cameraController;
        private readonly GameInputActions.GameplayActions _actions;

        public SelectionInputHandler(SelectionService selection, CameraController cameraController, GameInputActions inputActions)
        {
            _selection = selection;
            _cameraController = cameraController;
            _actions = inputActions.Gameplay;
        }

        public void PostTick()
        {
            if (!_actions.Select.WasPressedThisFrame())
                return;

            // A click on a panel belongs to the UI and must not change the selection behind it.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var pointer = _actions.PointerPosition.ReadValue<Vector2>();
            _selection.SelectAt(_cameraController.ScreenToWorld(pointer));
        }
    }
}
